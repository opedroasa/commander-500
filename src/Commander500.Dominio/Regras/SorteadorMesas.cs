using Commander500.Dominio.Comum;

namespace Commander500.Dominio.Regras;

/// <param name="Chave">Identificador escolhido por quem chama (ex.: posição da participação na lista).</param>
/// <param name="ByesAnteriores">Quantos byes o jogador já teve na temporada.</param>
/// <param name="UltimaCadeira">Cadeira (1–4) na rodada anterior da mesma etapa, para não repetir.</param>
/// <param name="ByesNaEtapa">Byes que o jogador já teve nas outras rodadas da mesma etapa (dia). Pesa mais que os da temporada.</param>
public record CandidatoSorteio(int Chave, int ByesAnteriores, int? UltimaCadeira = null, int ByesNaEtapa = 0);

/// <param name="ChavesBye">Candidatos que ficaram fora das mesas.</param>
/// <param name="Mesas">Cada mesa lista as chaves na ordem das cadeiras (posição 0 = cadeira 1, começa jogando).</param>
/// <param name="ParesRepetidos">Pares de jogadores que já tinham dividido mesa na etapa (0 = todos os confrontos novos).</param>
/// <param name="CadeirasRepetidas">Jogadores na mesma cadeira da rodada anterior.</param>
public record ResultadoSorteio(
    IReadOnlyList<int> ChavesBye, IReadOnlyList<IReadOnlyList<int>> Mesas,
    int ParesRepetidos = 0, int CadeirasRepetidas = 0);

/// <summary>Sorteio das mesas (spec 4.2 e 4.3).</summary>
public static class SorteadorMesas
{
    public const int TamanhoMesa = 4;

    /// <summary>Arranjos aleatórios testados quando há histórico na etapa; fica o melhor.</summary>
    public const int Tentativas = 300;

    private const int PesoParRepetido = 100;   // repetir adversário é muito pior que…
    private const int PesoCadeiraRepetida = 1; // …repetir a cadeira

    /// <param name="paresAnteriores">
    /// Quantas vezes cada par de chaves já dividiu mesa nesta etapa (a ordem dentro do par não importa).
    /// Vazio/nulo = sorteio aleatório simples.
    /// </param>
    public static ResultadoSorteio Sortear(
        IReadOnlyList<CandidatoSorteio> candidatos, IGeradorAleatorio gerador,
        IReadOnlyDictionary<(int, int), int>? paresAnteriores = null)
    {
        if (candidatos.Count < TamanhoMesa)
            throw new RegraNegocioException($"São necessários pelo menos {TamanhoMesa} jogadores presentes para sortear as mesas.");
        if (candidatos.Select(c => c.Chave).Distinct().Count() != candidatos.Count)
            throw new RegraNegocioException("Há jogadores duplicados no sorteio.");

        var quantidadeByes = candidatos.Count % TamanhoMesa;

        // Embaralha e depois ordena (de forma estável): primeiro quem ainda não ficou de bye neste dia, depois quem tem
        // menos byes na temporada. Entre empatados a ordem é aleatória — ou seja, sorteio entre os que têm menos byes (spec 4.2).
        // Só repete bye no mesmo dia quando todos os presentes já ficaram de fora.
        var embaralhados = candidatos.ToList();
        Embaralhar(embaralhados, gerador);
        var byes = embaralhados.OrderBy(c => c.ByesNaEtapa).ThenBy(c => c.ByesAnteriores)
            .Take(quantidadeByes).Select(c => c.Chave).ToList();
        var jogando = embaralhados.Where(c => !byes.Contains(c.Chave)).ToList();

        var temHistorico = (paresAnteriores?.Count ?? 0) > 0 || jogando.Any(c => c.UltimaCadeira is not null);
        var tentativas = temHistorico ? Tentativas : 1;

        List<List<CandidatoSorteio>>? melhor = null;
        (int Pares, int Cadeiras) melhorNota = (int.MaxValue, int.MaxValue);
        for (var i = 0; i < tentativas; i++)
        {
            Embaralhar(jogando, gerador);
            var mesas = jogando.Chunk(TamanhoMesa).Select(m => m.ToList()).ToList();
            var nota = Avaliar(mesas, paresAnteriores);
            if (Peso(nota) < Peso(melhorNota))
            {
                melhor = mesas;
                melhorNota = nota;
                if (Peso(nota) == 0) break; // não dá para melhorar
            }
        }

        return new ResultadoSorteio(
            byes,
            melhor!.Select(m => (IReadOnlyList<int>)m.Select(c => c.Chave).ToList()).ToList(),
            melhorNota.Pares, melhorNota.Cadeiras);
    }

    private static int Peso((int Pares, int Cadeiras) nota) =>
        nota.Pares == int.MaxValue ? int.MaxValue : nota.Pares * PesoParRepetido + nota.Cadeiras * PesoCadeiraRepetida;

    private static (int Pares, int Cadeiras) Avaliar(List<List<CandidatoSorteio>> mesas, IReadOnlyDictionary<(int, int), int>? paresAnteriores)
    {
        int pares = 0, cadeiras = 0;
        foreach (var mesa in mesas)
        {
            for (var posicao = 0; posicao < mesa.Count; posicao++)
            {
                if (mesa[posicao].UltimaCadeira == posicao + 1) cadeiras++;
                if (paresAnteriores is null) continue;
                for (var outro = posicao + 1; outro < mesa.Count; outro++)
                    pares += paresAnteriores.GetValueOrDefault(ChavePar(mesa[posicao].Chave, mesa[outro].Chave));
            }
        }
        return (pares, cadeiras);
    }

    /// <summary>Chave de um par que não depende da ordem: (3, 1) e (1, 3) viram (1, 3).</summary>
    public static (int, int) ChavePar(int a, int b) => a < b ? (a, b) : (b, a);

    /// <summary>Embaralhamento Fisher–Yates na própria lista.</summary>
    public static void Embaralhar<T>(IList<T> lista, IGeradorAleatorio gerador)
    {
        for (var i = lista.Count - 1; i > 0; i--)
        {
            var j = gerador.Proximo(i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
