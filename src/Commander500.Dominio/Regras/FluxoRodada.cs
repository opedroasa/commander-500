using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <summary>Jogador presente na rodada e o deck que ele vai usar.</summary>
public record ParticipanteInformado(int IdJogador, int IdDeck);

/// <summary>Timer da rodada calculado a partir do início + duração (spec 4.4).</summary>
public record Cronometro(DateTime IniciadaEm, int DuracaoMinutos, DateTime TerminaEm, int SegundosRestantes, bool Esgotado);

/// <summary>Quem já dividiu mesa numa etapa, a cadeira de cada jogador na rodada mais recente e quem já ficou de bye.</summary>
public record HistoricoEtapa(IReadOnlyDictionary<(int, int), int> Pares, IReadOnlyDictionary<int, int> UltimaCadeira,
    IReadOnlyDictionary<int, int> Byes)
{
    public static HistoricoEtapa Montar(IEnumerable<Rodada> rodadas)
    {
        var pares = new Dictionary<(int, int), int>();
        var ultimaCadeira = new Dictionary<int, int>();
        var byes = new Dictionary<int, int>();
        foreach (var rodada in rodadas.Where(r => r.Situacao != SituacaoRodada.Rascunho).OrderBy(r => r.Numero))
        {
            foreach (var bye in rodada.Participacoes.Where(p => p.Bye)) byes[bye.IdJogador] = byes.GetValueOrDefault(bye.IdJogador) + 1;
            var participacoesPorId = rodada.Participacoes.ToDictionary(p => p.Id);
            foreach (var mesa in rodada.Mesas)
            {
                var sentados = mesa.Cadeiras
                    .Select(c => (IdJogador: (c.Participacao ?? participacoesPorId[c.IdParticipacao]).IdJogador, c.Numero))
                    .ToList();
                foreach (var (idJogador, cadeira) in sentados) ultimaCadeira[idJogador] = cadeira; // rodada mais nova sobrescreve
                for (var i = 0; i < sentados.Count; i++)
                    for (var j = i + 1; j < sentados.Count; j++)
                    {
                        var par = SorteadorMesas.ChavePar(sentados[i].IdJogador, sentados[j].IdJogador);
                        pares[par] = pares.GetValueOrDefault(par) + 1;
                    }
            }
        }
        return new HistoricoEtapa(pares, ultimaCadeira, byes);
    }
}

/// <summary>
/// Máquina de estados da rodada (spec 4.6): Rascunho → Sorteada → EmAndamento → Encerrada.
/// Trabalha só com os objetos em memória; quem chama é responsável por salvar no banco.
/// </summary>
public static class FluxoRodada
{
    public const int DuracaoMaximaMinutos = 600;

    /// <summary>Define quem está presente e com qual deck. Permitido em Rascunho/Sorteada; uma rodada sorteada volta para Rascunho.</summary>
    public static void DefinirParticipantes(Rodada rodada, IReadOnlyList<ParticipanteInformado> participantes)
    {
        GarantirSituacao(rodada, "alterar os presentes", SituacaoRodada.Rascunho, SituacaoRodada.Sorteada);
        if (participantes.Select(p => p.IdJogador).Distinct().Count() != participantes.Count)
            throw new RegraNegocioException("Um jogador não pode aparecer duas vezes na mesma rodada.");

        DesfazerSorteio(rodada);

        var informados = participantes.ToDictionary(p => p.IdJogador);
        rodada.Participacoes.RemoveAll(p => !informados.ContainsKey(p.IdJogador));
        foreach (var informado in participantes)
        {
            var existente = rodada.Participacoes.FirstOrDefault(p => p.IdJogador == informado.IdJogador);
            if (existente is null)
                rodada.Participacoes.Add(new Participacao { IdJogador = informado.IdJogador, IdDeck = informado.IdDeck, Rodada = rodada });
            else
                existente.IdDeck = informado.IdDeck; // só a FK; o EF ajusta a navegação
        }
    }

    /// <summary>Sorteia (ou re-sorteia) mesas e byes. Permitido em Rascunho/Sorteada, ou seja, antes do timer começar.</summary>
    /// <param name="byesAnterioresPorJogador">Byes que cada jogador já teve na temporada (outras rodadas).</param>
    /// <param name="rodadasAnterioresDaEtapa">
    /// Outras rodadas já sorteadas da mesma etapa. O sorteio evita juntar de novo quem já dividiu mesa
    /// e evita repetir a cadeira de cada um na rodada mais recente.
    /// </param>
    public static void Sortear(Rodada rodada, IReadOnlyDictionary<int, int> byesAnterioresPorJogador, IGeradorAleatorio gerador,
        IEnumerable<Rodada>? rodadasAnterioresDaEtapa = null)
    {
        GarantirSituacao(rodada, "sortear", SituacaoRodada.Rascunho, SituacaoRodada.Sorteada);

        var participacoes = rodada.Participacoes;
        var historico = HistoricoEtapa.Montar(rodadasAnterioresDaEtapa ?? []);
        var candidatos = participacoes
            .Select((p, indice) => new CandidatoSorteio(indice, byesAnterioresPorJogador.GetValueOrDefault(p.IdJogador),
                historico.UltimaCadeira.TryGetValue(p.IdJogador, out var cadeira) ? cadeira : null,
                historico.Byes.GetValueOrDefault(p.IdJogador)))
            .ToList();

        // Converte os pares de jogadores (Id) em pares de chaves do sorteio (posição na lista).
        var chaveDoJogador = participacoes.Select((p, indice) => (p.IdJogador, indice)).ToDictionary(x => x.IdJogador, x => x.indice);
        var pares = new Dictionary<(int, int), int>();
        foreach (var ((jogadorA, jogadorB), vezes) in historico.Pares)
            if (chaveDoJogador.TryGetValue(jogadorA, out var chaveA) && chaveDoJogador.TryGetValue(jogadorB, out var chaveB))
                pares[SorteadorMesas.ChavePar(chaveA, chaveB)] = vezes;

        var resultado = SorteadorMesas.Sortear(candidatos, gerador, pares);

        DesfazerSorteio(rodada);
        foreach (var chave in resultado.ChavesBye) participacoes[chave].Bye = true;

        for (var numeroMesa = 0; numeroMesa < resultado.Mesas.Count; numeroMesa++)
        {
            var mesa = new Mesa { Numero = numeroMesa + 1, Rodada = rodada };
            var chavesNaMesa = resultado.Mesas[numeroMesa];
            for (var posicao = 0; posicao < chavesNaMesa.Count; posicao++)
            {
                var participacao = participacoes[chavesNaMesa[posicao]];
                mesa.Cadeiras.Add(new Cadeira { Mesa = mesa, Participacao = participacao, IdParticipacao = participacao.Id, Numero = posicao + 1 });
            }
            rodada.Mesas.Add(mesa);
        }
        rodada.Situacao = SituacaoRodada.Sorteada;
    }

    public const int MinimoJogadoresMesaManual = 2;
    public const int MaximoJogadoresMesaManual = 6;

    /// <summary>
    /// Monta as mesas à mão (rodadas que já aconteceram ou quando o lojista não quer sortear). Cada mesa é a lista de
    /// participações na ordem das cadeiras (a primeira começa jogando). Quem não estiver em nenhuma mesa fica de bye.
    /// Permitido em Rascunho/Sorteada → Sorteada.
    /// </summary>
    public static void MontarMesas(Rodada rodada, IReadOnlyList<IReadOnlyList<int>> mesasComParticipacoes)
    {
        GarantirSituacao(rodada, "montar as mesas", SituacaoRodada.Rascunho, SituacaoRodada.Sorteada);
        var mesas = mesasComParticipacoes.Where(m => m.Count > 0).ToList();
        if (mesas.Count == 0) throw new DadosInvalidosException("Monte pelo menos uma mesa.");

        var participacoesPorId = rodada.Participacoes.ToDictionary(p => p.Id);
        var sentados = new HashSet<int>();
        foreach (var mesa in mesas)
        {
            if (mesa.Count is < MinimoJogadoresMesaManual or > MaximoJogadoresMesaManual)
                throw new DadosInvalidosException($"Cada mesa deve ter de {MinimoJogadoresMesaManual} a {MaximoJogadoresMesaManual} jogadores.");
            foreach (var idParticipacao in mesa)
            {
                if (!participacoesPorId.ContainsKey(idParticipacao))
                    throw new DadosInvalidosException("Só jogadores presentes na rodada podem sentar nas mesas.");
                if (!sentados.Add(idParticipacao))
                    throw new DadosInvalidosException("Um jogador não pode sentar em duas mesas.");
            }
        }

        DesfazerSorteio(rodada);
        foreach (var participacao in rodada.Participacoes) participacao.Bye = !sentados.Contains(participacao.Id);
        for (var numeroMesa = 0; numeroMesa < mesas.Count; numeroMesa++)
        {
            var mesa = new Mesa { Numero = numeroMesa + 1, Rodada = rodada };
            for (var posicao = 0; posicao < mesas[numeroMesa].Count; posicao++)
            {
                var participacao = participacoesPorId[mesas[numeroMesa][posicao]];
                mesa.Cadeiras.Add(new Cadeira { Mesa = mesa, Participacao = participacao, IdParticipacao = participacao.Id, Numero = posicao + 1 });
            }
            rodada.Mesas.Add(mesa);
        }
        rodada.Situacao = SituacaoRodada.Sorteada;
    }

    /// <summary>
    /// Troca quem começa jogando na mesa: o jogador escolhido vai para a cadeira 1 e os outros giram junto,
    /// mantendo a ordem de jogo (quem estava depois dele continua depois). Permitido até a rodada ser encerrada.
    /// </summary>
    public static void DefinirQuemComeca(Rodada rodada, Mesa mesa, int idParticipacao)
    {
        GarantirSituacao(rodada, "trocar quem começa", SituacaoRodada.Sorteada, SituacaoRodada.EmAndamento);
        if (!rodada.Mesas.Contains(mesa)) throw new RegraNegocioException("A mesa não pertence a esta rodada.");
        var cadeiras = mesa.Cadeiras.OrderBy(c => c.Numero).ToList();
        var indice = cadeiras.FindIndex(c => IdParticipacaoDaCadeira(c) == idParticipacao);
        if (indice < 0) throw new RegraNegocioException("O jogador escolhido não está sentado nesta mesa.");
        for (var i = 0; i < cadeiras.Count; i++)
            cadeiras[(indice + i) % cadeiras.Count].Numero = i + 1;
    }

    /// <summary>
    /// Rodada com data anterior a hoje: já aconteceu e está sendo lançada depois. Pode receber resultados e ser encerrada
    /// sem passar pelo timer (Sorteada → Encerrada).
    /// </summary>
    public static bool EhRetroativa(Rodada rodada, DateOnly? hoje) => hoje is { } dia && rodada.Data < dia;

    /// <summary>Inicia o timer. Sorteada → EmAndamento.</summary>
    public static void Iniciar(Rodada rodada, DateTime agoraUtc, int? duracaoMinutos = null)
    {
        GarantirSituacao(rodada, "iniciar", SituacaoRodada.Sorteada);
        var duracao = duracaoMinutos ?? rodada.DuracaoMinutos;
        if (duracao is <= 0 or > DuracaoMaximaMinutos)
            throw new RegraNegocioException($"Duração deve estar entre 1 e {DuracaoMaximaMinutos} minutos.");

        rodada.DuracaoMinutos = duracao;
        rodada.IniciadaEm = agoraUtc;
        rodada.Situacao = SituacaoRodada.EmAndamento;
    }

    /// <summary>Registra (ou corrige, depois de encerrada) o resultado de uma mesa.</summary>
    /// <param name="hoje">Data de hoje (fuso da loja). Rodadas de datas passadas aceitam resultado ainda sorteadas.</param>
    /// <param name="idsEmpatados">
    /// Empate parcial: participações que ficaram no empate (2 ou mais; ex.: os 2 que sobraram vivos). Vazio = empate entre todos.
    /// Os outros da mesa contam como derrota.
    /// </param>
    public static void RegistrarResultado(Rodada rodada, Mesa mesa, ResultadoMesa resultado, int? idParticipacaoVencedora,
        DateTime agoraUtc, string? alteradaPor, DateOnly? hoje = null, IReadOnlyCollection<int>? idsEmpatados = null)
    {
        if (rodada.Situacao == SituacaoRodada.Encerrada) GarantirTemporadaAberta(rodada, "corrigir o resultado");
        if (EhRetroativa(rodada, hoje))
            GarantirSituacao(rodada, "registrar resultado", SituacaoRodada.Sorteada, SituacaoRodada.EmAndamento, SituacaoRodada.Encerrada);
        else
            GarantirSituacao(rodada, "registrar resultado", SituacaoRodada.EmAndamento, SituacaoRodada.Encerrada);
        if (!rodada.Mesas.Contains(mesa))
            throw new RegraNegocioException("A mesa não pertence a esta rodada.");

        switch (resultado)
        {
            case ResultadoMesa.Vitoria:
                if (idParticipacaoVencedora is null || mesa.Cadeiras.All(c => IdParticipacaoDaCadeira(c) != idParticipacaoVencedora))
                    throw new RegraNegocioException("O vencedor precisa ser um jogador sentado nesta mesa.");
                mesa.IdParticipacaoVencedora = idParticipacaoVencedora;
                foreach (var cadeira in mesa.Cadeiras) cadeira.Empatou = false;
                break;
            case ResultadoMesa.Empate:
                var empatados = (idsEmpatados ?? []).Distinct().ToHashSet();
                if (empatados.Any(id => mesa.Cadeiras.All(c => IdParticipacaoDaCadeira(c) != id)))
                    throw new RegraNegocioException("Só jogadores sentados nesta mesa podem empatar.");
                if (empatados.Count == 1)
                    throw new DadosInvalidosException("Empate precisa de pelo menos 2 jogadores. Com 1 só, marque como vitória.");
                // Todos marcados = empate geral (grava sem marcação, igual ao empate de antes).
                var parcial = empatados.Count > 0 && empatados.Count < mesa.Cadeiras.Count;
                foreach (var cadeira in mesa.Cadeiras) cadeira.Empatou = parcial && empatados.Contains(IdParticipacaoDaCadeira(cadeira));
                mesa.IdParticipacaoVencedora = null;
                mesa.ParticipacaoVencedora = null;
                break;
            default:
                throw new RegraNegocioException("Resultado deve ser vitória (com vencedor) ou empate.");
        }

        mesa.Resultado = resultado;
        mesa.AlteradaEm = agoraUtc;
        mesa.AlteradaPor = alteradaPor;
    }

    /// <summary>
    /// EmAndamento → Encerrada (ou Sorteada → Encerrada numa rodada retroativa). Toda mesa precisa de resultado.
    /// Trava a configuração da temporada.
    /// </summary>
    public static void Encerrar(Rodada rodada, DateTime agoraUtc, DateOnly? hoje = null)
    {
        if (EhRetroativa(rodada, hoje)) GarantirSituacao(rodada, "encerrar", SituacaoRodada.Sorteada, SituacaoRodada.EmAndamento);
        else GarantirSituacao(rodada, "encerrar", SituacaoRodada.EmAndamento);
        var pendentes = rodada.Mesas.Where(m => m.Resultado == ResultadoMesa.Pendente).Select(m => m.Numero).OrderBy(n => n).ToList();
        if (pendentes.Count > 0)
            throw new RegraNegocioException($"Mesas sem resultado: {string.Join(", ", pendentes)}.");

        rodada.Situacao = SituacaoRodada.Encerrada;
        rodada.EncerradaEm = agoraUtc;
        if (rodada.Temporada is not null) rodada.Temporada.ConfiguracaoTravada = true;
    }

    /// <summary>
    /// Troca o deck que o jogador usou na rodada (ex.: o lojista lançou o deck errado). Vale com a rodada sorteada,
    /// em andamento ou já encerrada — neste caso só enquanto a temporada estiver aberta. O deck precisa ser do jogador.
    /// </summary>
    public static void TrocarDeck(Rodada rodada, Participacao participacao, Deck deck)
    {
        GarantirSituacao(rodada, "trocar o deck", SituacaoRodada.Sorteada, SituacaoRodada.EmAndamento, SituacaoRodada.Encerrada);
        if (rodada.Situacao == SituacaoRodada.Encerrada) GarantirTemporadaAberta(rodada, "trocar o deck");
        if (!rodada.Participacoes.Contains(participacao))
            throw new RegraNegocioException("O jogador não está nesta rodada.");
        if (deck.IdJogador != participacao.IdJogador)
            throw new DadosInvalidosException($"O deck '{deck.Nome}' não é deste jogador.");
        participacao.IdDeck = deck.Id;
        participacao.Deck = deck;
    }

    /// <summary>Rodadas de temporada encerrada ficam congeladas (o ranking final e o Hall da Fama não mudam mais).</summary>
    private static void GarantirTemporadaAberta(Rodada rodada, string acao)
    {
        if (rodada.Temporada is { Ativa: false })
            throw new RegraNegocioException($"Não é possível {acao}: a temporada já foi encerrada.");
    }

    /// <summary>Só dá para excluir antes de iniciar o timer.</summary>
    public static void GarantirQuePodeExcluir(Rodada rodada) =>
        GarantirSituacao(rodada, "excluir", SituacaoRodada.Rascunho, SituacaoRodada.Sorteada);

    public static Cronometro? ObterCronometro(Rodada rodada, DateTime agoraUtc)
    {
        if (rodada.IniciadaEm is not { } iniciadaEm) return null;
        var terminaEm = iniciadaEm.AddMinutes(rodada.DuracaoMinutos);
        var segundosRestantes = (int)Math.Max(0, Math.Ceiling((terminaEm - agoraUtc).TotalSeconds));
        return new Cronometro(iniciadaEm, rodada.DuracaoMinutos, terminaEm, segundosRestantes, segundosRestantes == 0);
    }

    /// <summary>Byes de cada jogador na temporada, ignorando a rodada <paramref name="idRodadaIgnorada"/> e as ainda não sorteadas.</summary>
    public static Dictionary<int, int> ContarByesAnteriores(IEnumerable<Rodada> rodadasDaTemporada, int idRodadaIgnorada) =>
        rodadasDaTemporada
            .Where(r => r.Id != idRodadaIgnorada && r.Situacao != SituacaoRodada.Rascunho)
            .SelectMany(r => r.Participacoes)
            .Where(p => p.Bye)
            .GroupBy(p => p.IdJogador)
            .ToDictionary(g => g.Key, g => g.Count());

    public static string DescricaoSituacao(SituacaoRodada situacao) => situacao switch
    {
        SituacaoRodada.Rascunho => "Rascunho",
        SituacaoRodada.Sorteada => "Sorteada",
        SituacaoRodada.EmAndamento => "Em andamento",
        SituacaoRodada.Encerrada => "Encerrada",
        _ => situacao.ToString(),
    };

    private static int IdParticipacaoDaCadeira(Cadeira cadeira) => cadeira.Participacao?.Id ?? cadeira.IdParticipacao;

    private static void DesfazerSorteio(Rodada rodada)
    {
        rodada.Mesas.Clear();
        foreach (var participacao in rodada.Participacoes) participacao.Bye = false;
        rodada.Situacao = SituacaoRodada.Rascunho;
    }

    private static void GarantirSituacao(Rodada rodada, string acao, params SituacaoRodada[] permitidas)
    {
        if (!permitidas.Contains(rodada.Situacao))
            throw new RegraNegocioException($"Não é possível {acao}: rodada está em '{DescricaoSituacao(rodada.Situacao)}'.");
    }
}
