using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Aplicacao.Comum;

/// <summary>Converte entidades do domínio nos objetos de resposta da API (mapeamento manual, sem AutoMapper).</summary>
public static class Mapeamento
{
    public static TemporadaResposta ParaResposta(this Temporada t, int quantidadeRodadas, int quantidadeEtapas) =>
        new(t.Id, t.Nome, t.DataInicio, t.DataFim, t.Ativa, t.ConfiguracaoTravada, ConfiguracaoTemporada.De(t), quantidadeRodadas, quantidadeEtapas);

    public static ComandanteResposta ParaResposta(this Comandante c) => new(c.Id, c.Nome, c.UrlArte, c.UrlImagem, c.IdentidadeCor);

    public static CartaScryfallResposta ParaResposta(this CartaScryfall c) => new(c.Id, c.Nome, c.UrlArte, c.UrlImagem, c.IdentidadeCor, c.Tipo);

    public static DeckResposta ParaResposta(this Deck d, bool incluirObservacaoRevisao = true) => new(
        d.Id, d.IdJogador, d.Jogador?.Nome ?? "", d.Nome, d.UrlLista, d.Situacao,
        incluirObservacaoRevisao ? d.ObservacaoRevisao : null, d.DataCadastro,
        d.Comandantes.Select(dc => dc.Comandante!).OrderBy(c => c.Nome).Select(c => c.ParaResposta()).ToList());

    /// <summary>"Thrasios, Triton Hero + Tymna the Weaver"</summary>
    public static string NomesComandantes(Deck? deck) =>
        deck is null ? "" : string.Join(" + ", deck.Comandantes.Select(dc => dc.Comandante?.Nome).Where(n => n is not null).Order());

    public static ParticipacaoResposta ParaResposta(this Participacao p) => new(
        p.Id, p.IdJogador, p.Jogador?.Nome ?? "", p.Jogador?.Apelido,
        p.IdDeck, p.Deck?.Nome ?? "", p.Deck?.Situacao ?? SituacaoDeck.Pendente, NomesComandantes(p.Deck),
        p.Deck?.Comandantes.Select(dc => dc.Comandante).OrderBy(c => c?.Nome).FirstOrDefault(c => c?.UrlArte is not null)?.UrlArte,
        p.Bye);

    public static RodadaResposta ParaResposta(this Rodada r, DateTime agoraUtc)
    {
        var participacoes = r.Participacoes.OrderBy(p => p.Jogador?.Nome).ToList();
        var participacoesPorId = participacoes.ToDictionary(p => p.Id);
        var cronometro = FluxoRodada.ObterCronometro(r, agoraUtc);

        var mesas = r.Mesas.OrderBy(m => m.Numero).Select(m => new MesaResposta(
            m.Id, m.Numero, m.Resultado, m.IdParticipacaoVencedora, m.AlteradaEm, m.AlteradaPor,
            m.Cadeiras.OrderBy(c => c.Numero)
                .Select(c => new CadeiraResposta(c.Numero, (c.Participacao ?? participacoesPorId[c.IdParticipacao]).ParaResposta()))
                .ToList())).ToList();

        // Posição dentro da etapa: 1 + rodadas da mesma etapa com número menor na temporada.
        var numeroNaEtapa = r.Etapa is null ? 1 : 1 + r.Etapa.Rodadas.Count(x => x.Numero < r.Numero);

        return new RodadaResposta(
            r.Id, r.IdTemporada, r.Temporada?.Nome ?? "", r.IdEtapa, r.Etapa?.Numero ?? 0, numeroNaEtapa, r.Numero, r.Data,
            r.Situacao, FluxoRodada.DescricaoSituacao(r.Situacao), r.DuracaoMinutos, r.Temporada?.TurnosExtrasAposTempo ?? 0,
            agoraUtc,
            cronometro is null ? null : new CronometroResposta(cronometro.IniciadaEm, cronometro.DuracaoMinutos, cronometro.TerminaEm,
                cronometro.SegundosRestantes, cronometro.Esgotado),
            r.EncerradaEm,
            participacoes.Select(p => p.ParaResposta()).ToList(),
            participacoes.Where(p => p.Bye).Select(p => p.ParaResposta()).ToList(),
            mesas);
    }

    public static EstatisticaComandanteResposta ParaResposta(this LinhaEstatisticaComandante linha, IReadOnlyDictionary<int, Comandante> comandantes) => new(
        linha.Nome,
        linha.IdsComandantes
            .Select(id => comandantes.TryGetValue(id, out var c) ? c.ParaResposta() : new ComandanteResposta(id, "?", null, null, null))
            .OrderBy(c => c.Nome).ToList(),
        linha.Partidas, linha.Vitorias, linha.Empates, linha.PercentualVitoria, linha.Decks, linha.Jogadores);
}

/// <summary>Copia os dados do Scryfall para o comandante. O nome passa a ser o nome oficial da carta.</summary>
public static class DadosScryfall
{
    public static void Aplicar(Comandante comandante, CartaScryfall carta, DateTime agoraUtc)
    {
        comandante.IdScryfall = carta.Id;
        comandante.Nome = carta.Nome;
        comandante.NomeNormalizado = NomeComandante.Normalizar(carta.Nome);
        comandante.UrlArte = carta.UrlArte;
        comandante.UrlImagem = carta.UrlImagem;
        comandante.IdentidadeCor = carta.IdentidadeCor;
        comandante.DataConsultaScryfall = agoraUtc;
    }
}
