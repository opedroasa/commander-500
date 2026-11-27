// Espelho dos objetos da API (src/Commander500.Aplicacao/Modelos). Os enumeradores chegam como texto.

export type SituacaoDeck = 'Pendente' | 'Aprovado' | 'Reprovado'
export type SituacaoRodada = 'Rascunho' | 'Sorteada' | 'EmAndamento' | 'Encerrada'
export type ResultadoMesa = 'Pendente' | 'Vitoria' | 'Empate'
export type CriterioDesempate = 'Vitorias' | 'MenosPartidas' | 'MaisPartidas' | 'Empates' | 'PercentualVitoria'
/** ColocacaoNaEtapa (padrão, tratado): a colocação em cada etapa vale pontos. SomaDasRodadas: os pontos das rodadas somam direto. */
export type ModoPontuacao = 'ColocacaoNaEtapa' | 'SomaDasRodadas'

// ---------- Temporadas
export interface ConfiguracaoTemporada {
  pontosVitoria: number
  pontosEmpate: number
  pontosBye1: number
  pontosBye2: number
  pontosBye3: number
  minutosPadraoRodada: number
  /** regra "acabou o tempo, mais N turnos" (só aparece no timer) */
  turnosExtrasAposTempo: number
  minimoPartidasPercentualComandante: number
  /** só no modo soma das rodadas */
  criteriosDesempate: CriterioDesempate[]
  modoPontuacao: ModoPontuacao
  /** pontos de quem jogou e perdeu (presença) */
  pontosDerrota: number
  /** modo por colocação: pontos do 1º, 2º, 3º… da etapa */
  pontosPorColocacao: number[]
  /** modo por colocação: pontos de quem ficou abaixo da tabela */
  pontosDemaisColocados: number
}

export interface Temporada {
  id: number
  nome: string
  dataInicio: string
  dataFim: string | null
  ativa: boolean
  configuracaoTravada: boolean
  configuracao: ConfiguracaoTemporada
  quantidadeRodadas: number
  /** etapas normais (sem contar a mesa final) */
  quantidadeEtapas: number
  /** próxima etapa marcada (UTC) */
  proximaEtapaEm: string | null
  /** último dia para enviar as listas */
  prazoListas: string | null
  /** etapa da mesa final, se já foi formada */
  idEtapaFinal: number | null
  /** foto de destaque (top da temporada) */
  urlFoto: string | null
  legendaFoto: string | null
}

export interface TemporadaEnvio {
  nome: string
  dataInicio: string
  dataFim: string | null
  ativa: boolean
  configuracao?: ConfiguracaoTemporada | null
}

export interface LinhaRanking {
  posicao: number
  idJogador: number
  nomeJogador: string
  pontos: number
  vitorias: number
  empates: number
  derrotas: number
  byes: number
  /** mesas jogadas (bye não conta) */
  partidas: number
  percentualVitoria: number | null
  /** modo por colocação: etapas concluídas disputadas (sem a final) */
  etapas: number
  /** modo por colocação: etapas terminadas em 1º */
  primeirosLugares: number
  /** modo por colocação: pontos da mesa final (null = não jogou) */
  pontosFinal: number | null
  /** modo por colocação: mesma posição de outro jogador */
  empatado: boolean
}

export interface FinalistaSugerido {
  posicao: number
  idJogador: number
  nomeJogador: string
  pontos: number
  empatado: boolean
  /** deck da última rodada (0 = nenhum) */
  idDeck: number
}

// ---------- Jogadores, decks e comandantes
export interface Jogador {
  id: number
  nome: string
  apelido: string | null
  ativo: boolean
  dataCadastro: string
  quantidadeDecks: number
}

export interface JogadorEnvio {
  nome: string
  apelido: string | null
  ativo: boolean
}

export interface Comandante {
  id: number
  nome: string
  /** arte recortada do Scryfall */
  urlArte: string | null
  urlImagem: string | null
  /** letras WUBRG; '' = incolor; nulo = desconhecida */
  identidadeCor: string | null
}

/** Resultado da busca no Scryfall (campo de comandante). */
export interface CartaScryfall {
  id: string
  nome: string
  urlArte: string | null
  urlImagem: string | null
  identidadeCor: string
  tipo: string
}

/** Comandante escolhido no cadastro do deck: do Scryfall (com id) ou digitado. */
export interface ComandanteEnvio {
  idScryfall: string | null
  nome: string
}

export interface Deck {
  id: number
  idJogador: number
  nomeJogador: string
  nome: string
  urlLista: string | null
  situacao: SituacaoDeck
  /** só vem para o admin */
  observacaoRevisao: string | null
  dataCadastro: string
  comandantes: Comandante[]
}

export interface DeckEnvio {
  idJogador: number
  /** opcional: o padrão é o nome do(s) comandante(s) */
  nome: string | null
  urlLista: string | null
  comandantes: ComandanteEnvio[]
}

/** "A" ou "A + B" (dupla de parceiros/background = uma linha). */
export interface EstatisticaComandante {
  nome: string
  comandantes: Comandante[]
  partidas: number
  vitorias: number
  empates: number
  percentualVitoria: number | null
  /** decks cadastrados com esses comandantes */
  decks: number
  /** jogadores diferentes que jogaram com ele */
  jogadores: number
}

// ---------- Rodadas
export interface Participacao {
  id: number
  idJogador: number
  nomeJogador: string
  apelido: string | null
  idDeck: number
  nomeDeck: string
  situacaoDeck: SituacaoDeck
  comandantes: string
  urlArteComandante: string | null
  bye: boolean
}

export interface Cadeira {
  /** 1..4 (até 6 em mesa montada à mão); a cadeira 1 começa jogando */
  numero: number
  participacao: Participacao
  /** ficou no empate da mesa (todos, num empate geral; só os marcados, num empate parcial) */
  empatou: boolean
}

export interface Mesa {
  id: number
  numero: number
  resultado: ResultadoMesa
  idParticipacaoVencedora: number | null
  alteradaEm: string | null
  alteradaPor: string | null
  cadeiras: Cadeira[]
}

export interface Cronometro {
  iniciadaEm: string
  duracaoMinutos: number
  terminaEm: string
  segundosRestantes: number
  esgotado: boolean
}

export interface Rodada {
  id: number
  idTemporada: number
  nomeTemporada: string
  idEtapa: number
  numeroEtapa: number
  /** 1, 2, 3… dentro da etapa */
  numeroNaEtapa: number
  /** sequencial na temporada */
  numero: number
  data: string
  situacao: SituacaoRodada
  descricaoSituacao: string
  duracaoMinutos: number
  turnosExtrasAposTempo: number
  /** hora do servidor, para o timer acertar o relógio */
  agoraServidor: string
  cronometro: Cronometro | null
  encerradaEm: string | null
  participacoes: Participacao[]
  byes: Participacao[]
  mesas: Mesa[]
  /** rodada da mesa final */
  etapaFinal: boolean
  /** só na final: quantas rodadas estão previstas */
  rodadasPrevistasEtapa: number | null
  /** data passada: lança resultados e encerra sem timer */
  retroativa: boolean
  /** temporada aberta: rodada encerrada ainda aceita correção (resultado e deck) */
  temporadaAtiva: boolean
}

export interface LoginResposta {
  token: string
  expiraEm: string
  email: string
}

// ---------- Etapas
export interface ReferenciaJogador {
  id: number
  nome: string
}

export interface Etapa {
  id: number
  idTemporada: number
  nomeTemporada: string
  numero: number
  data: string
  quantidadeRodadas: number
  rodadasEncerradas: number
  /** todas as rodadas encerradas: ranking e vencedores definitivos */
  concluida: boolean
  /** posição 1 (vários quando empatados) */
  lideres: ReferenciaJogador[]
  /** mesa final do top 4 */
  final: boolean
  rodadasPrevistas: number | null
  /** foto do top da etapa */
  urlFoto: string | null
  legendaFoto: string | null
}

export interface LinhaRankingEtapa {
  /** igual para empatados (1, 1, 3…) */
  posicao: number
  idJogador: number
  nomeJogador: string
  /** pontos das rodadas da etapa */
  pontos: number
  /** vitórias em mesa + byes (na etapa o bye conta como vitória) */
  vitorias: number
  vitoriasMesa: number
  byes: number
  empates: number
  derrotas: number
  partidas: number
  vitoriasConfrontoDireto: number
  empatado: boolean
}

export interface DetalheEtapa {
  etapa: Etapa
  modoPontuacao: ModoPontuacao
  /** pontosTemporada: quanto a colocação vale no ranking da temporada (só no modo por colocação) */
  ranking: { linha: LinhaRankingEtapa; pontosTemporada: number | null }[]
  rodadas: Rodada[]
}

// ---------- Administradores (usuários da área do lojista)
export interface Administrador {
  id: number
  email: string
  /** true quando é quem está logado */
  usuarioAtual: boolean
}

// ---------- Fotos, configurações da loja e Hall da Fama
export interface Foto {
  urlFoto: string | null
  legendaFoto: string | null
}

export interface ConfiguracaoLoja {
  /** convite do grupo do Commander 500; nulo = botão escondido */
  linkGrupoWhatsapp: string | null
}

export interface LinhaHallDaFama {
  posicao: number
  idJogador: number
  nomeJogador: string
  pontos: number
  empatado: boolean
  /** deck que o jogador mais usou na temporada */
  deck: { id: number; nome: string; comandantes: Comandante[] } | null
}

export interface TemporadaHallDaFama {
  id: number
  nome: string
  dataInicio: string
  dataFim: string | null
  modoPontuacao: ModoPontuacao
  urlFoto: string | null
  legendaFoto: string | null
  /** ranking final completo (o site destaca o top 3) */
  ranking: LinhaHallDaFama[]
}

// ---------- Páginas do jogador e do comandante
export interface PontoMensal {
  /** "out/26" */
  mes: string
  partidas: number
  vitorias: number
  empates: number
  derrotas: number
  /** 0 a 1 */
  percentualMes: number
  percentualAcumulado: number
}

export interface JogadorLista {
  id: number
  nome: string
  apelido: string | null
  ativo: boolean
  temporadas: number
  partidas: number
  vitorias: number
  empates: number
  percentualVitoria: number | null
  /** temporadas encerradas em 1º */
  titulos: number
  etapasVencidas: number
  /** comandantes do deck que ele mais jogou */
  comandantesPrincipais: Comandante[]
}

export interface Confronto {
  idJogador: number
  nome: string
  mesas: number
  vitoriasDoJogador: number
  vitoriasDoAdversario: number
}

export interface DeckDoJogador {
  id: number
  nome: string
  urlLista: string | null
  /** revisão da lista pelo lojista */
  situacao: SituacaoDeck
  /** chave da página do comandante */
  chave: string
  comandantes: Comandante[]
  partidas: number
  vitorias: number
  empates: number
  percentualVitoria: number | null
}

export interface PaginaJogador {
  id: number
  nome: string
  apelido: string | null
  ativo: boolean
  dataCadastro: string
  geral: EstatisticasJogador
  titulos: number
  temporadas: { idTemporada: number; nome: string; ativa: boolean; posicao: number | null; pontos: number; partidas: number; vitorias: number; campeao: boolean }[]
  etapasVencidas: { id: number; idTemporada: number; nomeTemporada: string; numero: number; data: string }[]
  decks: DeckDoJogador[]
  idMelhorDeck: number | null
  evolucao: PontoMensal[]
  /** quem mais venceu mesas com ele junto */
  nemesis: Confronto | null
  /** de quem ele mais venceu */
  vitimaFavorita: Confronto | null
  confrontos: Confronto[]
}

export interface PaginaComandante {
  chave: string
  nome: string
  comandantes: Comandante[]
  identidadeCor: string | null
  partidas: number
  vitorias: number
  empates: number
  derrotas: number
  percentualVitoria: number | null
  /** jogadores únicos que jogaram com ele (partidas) */
  jogadores: number
  /** jogadores únicos que venceram mesas com ele */
  jogadoresComVitoria: number
  titulos: { idTemporada: number; nomeTemporada: string; idJogador: number; nomeJogador: string }[]
  etapasVencidas: number
  evolucao: PontoMensal[]
  listas: { idDeck: number; nomeDeck: string; urlLista: string | null; idJogador: number; nomeJogador: string; partidas: number; vitorias: number; empates: number; percentualVitoria: number | null }[]
  temporadas: { idTemporada: number; nome: string; partidas: number; vitorias: number; percentualVitoria: number | null }[]
}

// ---------- Regulamento
export interface Regulamento {
  /** HTML do editor */
  conteudo: string
  alteradoEm: string | null
  alteradoPor: string | null
}

// ---------- Perfil do jogador
export interface EstatisticasJogador {
  /** rodadas em que esteve presente (partidas + byes) */
  participacoes: number
  partidas: number
  vitorias: number
  empates: number
  derrotas: number
  byes: number
  percentualVitoria: number | null
}

export interface PerfilJogador {
  id: number
  nome: string
  apelido: string | null
  ativo: boolean
  geral: EstatisticasJogador
  temporadasJogadas: number
  temporada: { idTemporada: number; nomeTemporada: string; posicao: number | null; pontos: number; estatisticas: EstatisticasJogador } | null
  temporadasVencidas: { id: number; nome: string }[]
  etapasVencidas: { id: number; idTemporada: number; nomeTemporada: string; numero: number; data: string }[]
  decks: Deck[]
}
