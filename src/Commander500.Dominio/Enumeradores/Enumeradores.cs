namespace Commander500.Dominio.Enumeradores;

// Os enumeradores são gravados no banco e enviados no JSON pelo NOME (ex.: "EmAndamento").

/// <summary>Revisão do deck pelo lojista (spec 4.5). Não bloqueia o uso do deck na rodada.</summary>
public enum SituacaoDeck { Pendente, Aprovado, Reprovado }

/// <summary>Fluxo da rodada (spec 4.6): Rascunho → Sorteada → EmAndamento → Encerrada.</summary>
public enum SituacaoRodada { Rascunho, Sorteada, EmAndamento, Encerrada }

/// <summary>Resultado de uma mesa: um vencedor OU empate (todos recebem pontos de empate).</summary>
public enum ResultadoMesa { Pendente, Vitoria, Empate }

/// <summary>
/// Critérios de desempate do ranking da temporada, aplicados depois dos pontos e na ordem configurada.
/// O nome do jogador é sempre o último critério.
/// </summary>
public enum CriterioDesempate { Vitorias, MenosPartidas, MaisPartidas, Empates, PercentualVitoria }
