using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <summary>Etapas são criadas automaticamente: uma por data dentro da temporada, numeradas pela data.</summary>
public static class RegrasEtapa
{
    /// <summary>Devolve a etapa da temporada naquela <paramref name="data"/>, criando uma nova na lista se precisar.</summary>
    public static Etapa Resolver(Temporada temporada, List<Etapa> etapas, DateOnly data)
    {
        if (data < temporada.DataInicio || (temporada.DataFim is { } fim && data > fim))
            throw new DadosInvalidosException($"A data deve estar dentro da temporada ({temporada.DataInicio:dd/MM/yyyy}" +
                                              (temporada.DataFim is { } f ? $" a {f:dd/MM/yyyy})." : " em diante)."));

        var etapa = etapas.FirstOrDefault(e => e.Data == data && !e.Final); // a mesa final só recebe rodadas pelo id
        if (etapa is null)
        {
            etapa = new Etapa { Temporada = temporada, IdTemporada = temporada.Id, Data = data };
            etapas.Add(etapa);
            Renumerar(etapas);
        }
        return etapa;
    }

    /// <summary>Numera as etapas 1..N pela data. A mesa final fica sempre por último.</summary>
    public static void Renumerar(IEnumerable<Etapa> etapas)
    {
        var numero = 1;
        foreach (var etapa in etapas.OrderBy(e => e.Final).ThenBy(e => e.Data)) etapa.Numero = numero++;
    }

    /// <summary>Etapa concluída: tem rodadas e todas estão encerradas. Só ela entra na pontuação por colocação.</summary>
    public static bool EstaConcluida(IEnumerable<Rodada> rodadasDaEtapa)
    {
        var rodadas = rodadasDaEtapa.ToList();
        return rodadas.Count > 0 && rodadas.All(r => r.Situacao == SituacaoRodada.Encerrada);
    }

    /// <summary>
    /// Cria a mesa final do top 4 (modo por colocação): uma etapa marcada como final, com a quantidade de rodadas que
    /// os finalistas vão jogar. A primeira rodada (com os finalistas presentes) é criada por quem chama.
    /// </summary>
    public static Etapa CriarFinal(Temporada temporada, List<Etapa> etapas, IReadOnlyCollection<Rodada> rodadasDaTemporada,
        DateOnly data, int quantidadeRodadas, int quantidadeFinalistas)
    {
        if (temporada.ModoPontuacao != ModoPontuacao.ColocacaoNaEtapa)
            throw new RegraNegocioException("A mesa final só existe no modo de pontuação por colocação na etapa.");
        if (!temporada.Ativa) throw new RegraNegocioException("A temporada não está ativa.");
        if (etapas.Any(e => e.Final)) throw new RegraNegocioException("Esta temporada já tem uma mesa final.");
        if (rodadasDaTemporada.Any(r => r.Situacao != SituacaoRodada.Encerrada))
            throw new RegraNegocioException("Encerre (ou exclua) as rodadas em aberto antes de formar a mesa final.");
        if (quantidadeRodadas is < 1 or > 10) throw new DadosInvalidosException("A final deve ter de 1 a 10 rodadas.");
        if (quantidadeFinalistas < SorteadorMesas.TamanhoMesa)
            throw new DadosInvalidosException($"Escolha pelo menos {SorteadorMesas.TamanhoMesa} finalistas.");
        if (data < temporada.DataInicio || (temporada.DataFim is { } fim && data > fim))
            throw new DadosInvalidosException("A data da final deve estar dentro da temporada.");

        var final = new Etapa { Temporada = temporada, IdTemporada = temporada.Id, Data = data, Final = true, RodadasPrevistas = quantidadeRodadas };
        etapas.Add(final);
        Renumerar(etapas);
        return final;
    }
}
