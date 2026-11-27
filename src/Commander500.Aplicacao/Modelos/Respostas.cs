using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Aplicacao.Modelos;

// Dados que a API devolve para o site (React). Espelhados em web/src/api/tipos.ts.

public record LoginResposta(string Token, DateTime ExpiraEm, string Email);

// ----- Administradores
/// <param name="UsuarioAtual">true quando é quem está logado (a tela mostra "você" e não deixa excluir).</param>
public record AdministradorResposta(int Id, string Email, bool UsuarioAtual);

// ----- Temporadas
/// <param name="ProximaEtapaEm">Próxima etapa marcada (UTC), para o aviso da página inicial.</param>
/// <param name="IdEtapaFinal">Etapa da mesa final, quando já foi formada.</param>
public record TemporadaResposta(
    int Id, string Nome, DateOnly DataInicio, DateOnly? DataFim, bool Ativa, bool ConfiguracaoTravada,
    ConfiguracaoTemporada Configuracao, int QuantidadeRodadas, int QuantidadeEtapas,
    DateTime? ProximaEtapaEm, DateOnly? PrazoListas, int? IdEtapaFinal, string? UrlFoto, string? LegendaFoto);

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
    bool Final, int? RodadasPrevistas, string? UrlFoto, string? LegendaFoto);

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

/// <param name="Empatou">Ficou no empate da mesa (todos, num empate geral; só os marcados, num empate parcial).</param>
public record CadeiraResposta(int Numero, ParticipacaoResposta Participacao, bool Empatou);

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
    bool EtapaFinal, int? RodadasPrevistasEtapa, bool Retroativa, bool TemporadaAtiva);

/// <summary>Foto do top (etapa ou temporada) depois de enviada.</summary>
public record FotoResposta(string? UrlFoto, string? LegendaFoto);

// ----- Configurações da loja
/// <param name="LinkGrupoWhatsapp">Convite do grupo do Commander 500 (nulo = botão escondido).</param>
public record ConfiguracaoLojaResposta(string? LinkGrupoWhatsapp);

// ----- Hall da Fama
public record DeckHallDaFamaResposta(int Id, string Nome, IReadOnlyList<ComandanteResposta> Comandantes);

/// <param name="Deck">Deck que o jogador mais usou na temporada.</param>
public record LinhaHallDaFamaResposta(int Posicao, int IdJogador, string NomeJogador, int Pontos, bool Empatado, DeckHallDaFamaResposta? Deck);

/// <param name="Ranking">Ranking final completo (o site destaca o top 3).</param>
public record TemporadaHallDaFamaResposta(
    int Id, string Nome, DateOnly DataInicio, DateOnly? DataFim, ModoPontuacao ModoPontuacao,
    string? UrlFoto, string? LegendaFoto, IReadOnlyList<LinhaHallDaFamaResposta> Ranking);

// ----- Páginas do jogador e do comandante
/// <param name="PercentualMes">Vitórias ÷ partidas do mês (0 a 1).</param>
/// <param name="PercentualAcumulado">Vitórias ÷ partidas até o mês (0 a 1).</param>
public record PontoMensalResposta(string Mes, int Partidas, int Vitorias, int Empates, int Derrotas, double PercentualMes, double PercentualAcumulado);

/// <summary>Linha da lista pública de jogadores.</summary>
/// <param name="Titulos">Temporadas encerradas em que terminou em 1º.</param>
/// <param name="ComandantesPrincipais">Comandantes do deck que ele mais jogou (para a arte).</param>
public record JogadorListaResposta(
    int Id, string Nome, string? Apelido, bool Ativo, int Temporadas, int Partidas, int Vitorias, int Empates,
    double? PercentualVitoria, int Titulos, int EtapasVencidas, IReadOnlyList<ComandanteResposta> ComandantesPrincipais);

public record TemporadaDoJogadorResposta(int IdTemporada, string Nome, bool Ativa, int? Posicao, int Pontos, int Partidas, int Vitorias, bool Campeao);

/// <param name="Situacao">Revisão da lista pelo lojista (aprovada, pendente ou reprovada). A observação da revisão não vai para a página pública.</param>
public record DeckDoJogadorResposta(
    int Id, string Nome, string? UrlLista, SituacaoDeck Situacao, string Chave, IReadOnlyList<ComandanteResposta> Comandantes,
    int Partidas, int Vitorias, int Empates, double? PercentualVitoria);

public record ConfrontoResposta(int IdJogador, string Nome, int Mesas, int VitoriasDoJogador, int VitoriasDoAdversario);

/// <summary>Página completa do jogador.</summary>
/// <param name="IdMelhorDeck">Maior % de vitória com 3+ partidas (ou o de mais vitórias).</param>
/// <param name="Nemesis">Quem mais venceu mesas com ele junto.</param>
/// <param name="VitimaFavorita">De quem ele mais venceu.</param>
public record PaginaJogadorResposta(
    int Id, string Nome, string? Apelido, bool Ativo, DateTime DataCadastro,
    EstatisticasJogadorResposta Geral, int Titulos, IReadOnlyList<TemporadaDoJogadorResposta> Temporadas,
    IReadOnlyList<ReferenciaEtapaResposta> EtapasVencidas, IReadOnlyList<DeckDoJogadorResposta> Decks, int? IdMelhorDeck,
    IReadOnlyList<PontoMensalResposta> Evolucao, ConfrontoResposta? Nemesis, ConfrontoResposta? VitimaFavorita,
    IReadOnlyList<ConfrontoResposta> Confrontos);

/// <summary>Título de temporada conquistado com o comandante (o campeão usou mais esse deck na temporada).</summary>
public record TituloComandanteResposta(int IdTemporada, string NomeTemporada, int IdJogador, string NomeJogador);

/// <summary>Lista (deck) cadastrada com o comandante, de qualquer jogador.</summary>
public record ListaComandanteResposta(
    int IdDeck, string NomeDeck, string? UrlLista, int IdJogador, string NomeJogador,
    int Partidas, int Vitorias, int Empates, double? PercentualVitoria);

public record TemporadaDoComandanteResposta(int IdTemporada, string Nome, int Partidas, int Vitorias, double? PercentualVitoria);

/// <summary>Página do comandante (ou da dupla de parceiros/background).</summary>
/// <param name="Chave">Ids dos comandantes em ordem, ex.: "12-34" (endereço da página).</param>
/// <param name="EtapasVencidas">Etapas concluídas em que um jogador com esse deck terminou em 1º.</param>
public record PaginaComandanteResposta(
    string Chave, string Nome, IReadOnlyList<ComandanteResposta> Comandantes, string? IdentidadeCor,
    int Partidas, int Vitorias, int Empates, int Derrotas, double? PercentualVitoria, int Jogadores, int JogadoresComVitoria,
    IReadOnlyList<TituloComandanteResposta> Titulos, int EtapasVencidas,
    IReadOnlyList<PontoMensalResposta> Evolucao, IReadOnlyList<ListaComandanteResposta> Listas,
    IReadOnlyList<TemporadaDoComandanteResposta> Temporadas);
