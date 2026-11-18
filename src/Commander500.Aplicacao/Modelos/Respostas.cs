using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Aplicacao.Modelos;

// Dados que a API devolve para o site (React). Espelhados em web/src/api/tipos.ts.

public record LoginResposta(string Token, DateTime ExpiraEm, string Email);

// ----- Temporadas
/// <param name="ProximaEtapaEm">Próxima etapa marcada (UTC), para o aviso da página inicial.</param>
/// <param name="IdEtapaFinal">Etapa da mesa final, quando já foi formada.</param>
public record TemporadaResposta(
    int Id, string Nome, DateOnly DataInicio, DateOnly? DataFim, bool Ativa, bool ConfiguracaoTravada,
    ConfiguracaoTemporada Configuracao, int QuantidadeRodadas, int QuantidadeEtapas,
    DateTime? ProximaEtapaEm, DateOnly? PrazoListas, int? IdEtapaFinal);

// ----- Jogadores, decks e comandantes
public record JogadorResposta(int Id, string Nome, string? Apelido, bool Ativo, DateTime DataCadastro, int QuantidadeDecks);

/// <param name="UrlArte">Arte recortada do Scryfall (nula se ainda não conhecida).</param>
/// <param name="IdentidadeCor">Letras WUBRG; "" = incolor; nulo = desconhecida.</param>
public record ComandanteResposta(int Id, string Nome, string? UrlArte, string? UrlImagem, string? IdentidadeCor);

/// <summary>Resultado da busca no Scryfall (campo de comandante do cadastro de deck).</summary>
public record CartaScryfallResposta(Guid Id, string Nome, string? UrlArte, string? UrlImagem, string IdentidadeCor, string Tipo);

public record DeckResposta(
    int Id, int IdJogador, string NomeJogador, string Nome, string? UrlLista,
    SituacaoDeck Situacao, string? ObservacaoRevisao, DateTime DataCadastro, IReadOnlyList<ComandanteResposta> Comandantes);

// ----- Estatísticas
public record EstatisticaComandanteResposta(
    string Nome, IReadOnlyList<ComandanteResposta> Comandantes,
    int Partidas, int Vitorias, int Empates, double? PercentualVitoria, int Decks, int Jogadores);

/// <param name="Participacoes">Rodadas em que esteve presente (partidas + byes).</param>
public record EstatisticasJogadorResposta(int Participacoes, int Partidas, int Vitorias, int Empates, int Derrotas, int Byes, double? PercentualVitoria);
public record TemporadaJogadorResposta(int IdTemporada, string NomeTemporada, int? Posicao, int Pontos, EstatisticasJogadorResposta Estatisticas);
public record ReferenciaTemporadaResposta(int Id, string Nome);
public record ReferenciaEtapaResposta(int Id, int IdTemporada, string NomeTemporada, int Numero, DateOnly Data);

/// <summary>Mini perfil. <see cref="DeckResposta.ObservacaoRevisao"/> só vem preenchida para o admin.</summary>
public record PerfilJogadorResposta(
    int Id, string Nome, string? Apelido, bool Ativo,
    EstatisticasJogadorResposta Geral, int TemporadasJogadas, TemporadaJogadorResposta? Temporada,
    IReadOnlyList<ReferenciaTemporadaResposta> TemporadasVencidas, IReadOnlyList<ReferenciaEtapaResposta> EtapasVencidas,
    IReadOnlyList<DeckResposta> Decks);

// ----- Etapas
public record ReferenciaJogadorResposta(int Id, string Nome);

/// <param name="Concluida">Todas as rodadas encerradas: ranking e vencedores são definitivos.</param>
/// <param name="Lideres">Posição 1 (vários quando empatados).</param>
/// <param name="Final">Mesa final do top 4.</param>
/// <param name="RodadasPrevistas">Só na final: quantas rodadas os finalistas jogam.</param>
public record EtapaResposta(
    int Id, int IdTemporada, string NomeTemporada, int Numero, DateOnly Data,
    int QuantidadeRodadas, int RodadasEncerradas, bool Concluida, IReadOnlyList<ReferenciaJogadorResposta> Lideres,
    bool Final, int? RodadasPrevistas);

/// <summary>Linha do ranking da etapa + pontos que a colocação vale na temporada (só no modo por colocação).</summary>
public record LinhaEtapaResposta(LinhaRankingEtapa Linha, int? PontosTemporada);

public record DetalheEtapaResposta(EtapaResposta Etapa, ModoPontuacao ModoPontuacao, IReadOnlyList<LinhaEtapaResposta> Ranking,
    IReadOnlyList<RodadaResposta> Rodadas);

/// <param name="IdDeck">Deck da última rodada do jogador na temporada (0 = nenhum).</param>
public record FinalistaSugeridoResposta(int Posicao, int IdJogador, string NomeJogador, int Pontos, bool Empatado, int IdDeck);

// ----- Regulamento
public record RegulamentoResposta(string Conteudo, DateTime? AlteradoEm, string? AlteradoPor);

// ----- Rodadas
public record ParticipacaoResposta(
    int Id, int IdJogador, string NomeJogador, string? Apelido,
    int IdDeck, string NomeDeck, SituacaoDeck SituacaoDeck, string Comandantes, string? UrlArteComandante, bool Bye);

public record CadeiraResposta(int Numero, ParticipacaoResposta Participacao);

public record MesaResposta(
    int Id, int Numero, ResultadoMesa Resultado, int? IdParticipacaoVencedora,
    DateTime? AlteradaEm, string? AlteradaPor, IReadOnlyList<CadeiraResposta> Cadeiras);

public record CronometroResposta(DateTime IniciadaEm, int DuracaoMinutos, DateTime TerminaEm, int SegundosRestantes, bool Esgotado);

/// <param name="Numero">Sequencial na temporada.</param>
/// <param name="NumeroNaEtapa">1, 2, 3… dentro da etapa.</param>
/// <param name="AgoraServidor">Hora do servidor, para o timer do navegador acertar o relógio.</param>
/// <param name="EtapaFinal">A rodada é da mesa final.</param>
/// <param name="RodadasPrevistasEtapa">Só na final: quantas rodadas estão previstas.</param>
/// <param name="Retroativa">Data passada: aceita resultados e encerramento sem iniciar o timer.</param>
public record RodadaResposta(
    int Id, int IdTemporada, string NomeTemporada, int IdEtapa, int NumeroEtapa, int NumeroNaEtapa, int Numero, DateOnly Data,
    SituacaoRodada Situacao, string DescricaoSituacao, int DuracaoMinutos, int TurnosExtrasAposTempo,
    DateTime AgoraServidor, CronometroResposta? Cronometro, DateTime? EncerradaEm,
    IReadOnlyList<ParticipacaoResposta> Participacoes, IReadOnlyList<ParticipacaoResposta> Byes, IReadOnlyList<MesaResposta> Mesas,
    bool EtapaFinal, int? RodadasPrevistasEtapa, bool Retroativa);
