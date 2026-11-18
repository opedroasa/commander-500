// Consultas e alterações na API usando o TanStack Query (cache, recarregar, carregando/erro).
// Os nomes começam com "use" porque é a regra do React para hooks.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { requisitar } from './cliente'
import type {
  CartaScryfall, ConfiguracaoTemporada, Deck, DeckEnvio, DetalheEtapa, EstatisticaComandante, Etapa, FinalistaSugerido, Jogador,
  JogadorEnvio, LinhaRanking, PerfilJogador, Regulamento, ResultadoMesa, Rodada, SituacaoDeck, Temporada, TemporadaEnvio,
} from './tipos'

/** Chaves do cache. Alterar algo "invalida" as chaves relacionadas para a tela buscar de novo. */
export const chaves = {
  temporadas: ['temporadas'] as const,
  temporada: (id: number) => ['temporadas', id] as const,
  ranking: (id: number) => ['temporadas', id, 'ranking'] as const,
  rodadasTemporada: (id: number) => ['temporadas', id, 'rodadas'] as const,
  etapasTemporada: (id: number) => ['temporadas', id, 'etapas'] as const,
  rodada: (id: number) => ['rodadas', id] as const,
  rodadasAtuais: ['rodadas', 'atuais'] as const,
  etapa: (id: number) => ['etapas', id] as const,
  jogadores: ['jogadores'] as const,
  decks: ['decks'] as const,
  estatisticasComandantes: (idTemporada: number | null) => ['estatisticas-comandantes', idTemporada ?? 'todas'] as const,
  perfil: (id: number, idTemporada: number | null) => ['perfil', id, idTemporada ?? 'todas'] as const,
  regulamento: ['regulamento'] as const,
  finalistas: (id: number, quantidade: number) => ['temporadas', id, 'finalistas', quantidade] as const,
}

// ---------- Consultas públicas
export const useTemporadas = () => useQuery({ queryKey: chaves.temporadas, queryFn: () => requisitar<Temporada[]>('/api/temporadas') })
export const useTemporada = (id: number) =>
  useQuery({ queryKey: chaves.temporada(id), queryFn: () => requisitar<Temporada>(`/api/temporadas/${id}`) })
export const useRanking = (id: number) =>
  useQuery({ queryKey: chaves.ranking(id), queryFn: () => requisitar<LinhaRanking[]>(`/api/temporadas/${id}/ranking`) })
/** Ranking de comandantes de uma temporada, ou de todas quando nulo (aí lista também quem só tem deck). */
export const useEstatisticasComandantes = (idTemporada: number | null) =>
  useQuery({
    queryKey: chaves.estatisticasComandantes(idTemporada),
    queryFn: () => requisitar<EstatisticaComandante[]>(
      idTemporada ? `/api/temporadas/${idTemporada}/comandantes` : '/api/comandantes/estatisticas'),
  })
export const useEtapasTemporada = (id: number) =>
  useQuery({ queryKey: chaves.etapasTemporada(id), queryFn: () => requisitar<Etapa[]>(`/api/temporadas/${id}/etapas`) })
export const useEtapa = (id: number, habilitada = true) =>
  useQuery({ queryKey: chaves.etapa(id), queryFn: () => requisitar<DetalheEtapa>(`/api/etapas/${id}`), enabled: habilitada })
export const usePerfilJogador = (id: number | null, idTemporada: number | null) =>
  useQuery({
    queryKey: chaves.perfil(id ?? 0, idTemporada),
    queryFn: () => requisitar<PerfilJogador>(`/api/jogadores/${id}/perfil${idTemporada ? `?idTemporada=${idTemporada}` : ''}`),
    enabled: id != null,
  })
export const useRegulamento = () =>
  useQuery({ queryKey: chaves.regulamento, queryFn: () => requisitar<Regulamento>('/api/regulamento') })
export const useRodadasTemporada = (id: number) =>
  useQuery({ queryKey: chaves.rodadasTemporada(id), queryFn: () => requisitar<Rodada[]>(`/api/temporadas/${id}/rodadas`) })

/** Dados ao vivo: busca de novo a cada intervalo para todo celular ver sorteio/resultados sem recarregar. */
export const useRodada = (id: number, intervaloMs = 0) =>
  useQuery({
    queryKey: chaves.rodada(id),
    queryFn: () => requisitar<Rodada>(`/api/rodadas/${id}`),
    refetchInterval: (consulta) => (intervaloMs && consulta.state.data?.situacao !== 'Encerrada' ? intervaloMs : false),
  })
export const useRodadasAtuais = (intervaloMs = 15000) =>
  useQuery({ queryKey: chaves.rodadasAtuais, queryFn: () => requisitar<Rodada[]>('/api/rodadas/atuais'), refetchInterval: intervaloMs })

// ---------- Consultas do admin
export const useJogadores = () => useQuery({ queryKey: chaves.jogadores, queryFn: () => requisitar<Jogador[]>('/api/jogadores') })
export const useDecks = () => useQuery({ queryKey: chaves.decks, queryFn: () => requisitar<Deck[]>('/api/decks') })
/** Busca de comandante no Scryfall (admin), a partir de 2 letras. */
export const useBuscaScryfall = (texto: string) =>
  useQuery({
    queryKey: ['scryfall', texto.trim().toLowerCase()],
    queryFn: () => requisitar<CartaScryfall[]>(`/api/scryfall/comandantes?busca=${encodeURIComponent(texto.trim())}`),
    enabled: texto.trim().length >= 2,
    staleTime: 60 * 60_000,
    retry: false,
  })

/** Top N do ranking com o último deck de cada um (sugestão para a mesa final). */
export const useFinalistasSugeridos = (idTemporada: number, quantidade: number, habilitada: boolean) =>
  useQuery({
    queryKey: chaves.finalistas(idTemporada, quantidade),
    queryFn: () => requisitar<FinalistaSugerido[]>(`/api/temporadas/${idTemporada}/finalistas?quantidade=${quantidade}`),
    enabled: habilitada,
  })

// ---------- Alterações
/** Alteração genérica que, ao dar certo, invalida as chaves informadas. */
function useAlteracao<TDados, TResultado = unknown>(funcao: (dados: TDados) => Promise<TResultado>, invalidar: readonly (readonly unknown[])[]) {
  const cache = useQueryClient()
  return useMutation({
    mutationFn: funcao,
    onSuccess: async () => {
      await Promise.all(invalidar.map((chave) => cache.invalidateQueries({ queryKey: chave })))
    },
  })
}

// Jogadores
export const useSalvarJogador = () =>
  useAlteracao(({ id, dados }: { id?: number; dados: JogadorEnvio }) =>
    id ? requisitar<Jogador>(`/api/jogadores/${id}`, { metodo: 'PUT', corpo: dados })
       : requisitar<Jogador>('/api/jogadores', { metodo: 'POST', corpo: dados }),
  [chaves.jogadores, chaves.decks, ['perfil']])
export const useExcluirJogador = () =>
  useAlteracao((id: number) => requisitar<void>(`/api/jogadores/${id}`, { metodo: 'DELETE' }), [chaves.jogadores, chaves.decks, ['perfil']])

// Decks
export const useSalvarDeck = () =>
  useAlteracao(({ id, dados }: { id?: number; dados: DeckEnvio }) =>
    id ? requisitar<Deck>(`/api/decks/${id}`, { metodo: 'PUT', corpo: dados })
       : requisitar<Deck>('/api/decks', { metodo: 'POST', corpo: dados }),
  [chaves.decks, chaves.jogadores, ['estatisticas-comandantes'], ['perfil']])
export const useRevisarDeck = () =>
  useAlteracao(({ id, situacao, observacao }: { id: number; situacao: SituacaoDeck; observacao: string | null }) =>
    requisitar<Deck>(`/api/decks/${id}/revisao`, { metodo: 'PATCH', corpo: { situacao, observacao } }),
  [chaves.decks, ['rodadas'], ['perfil']])
export const useExcluirDeck = () =>
  useAlteracao((id: number) => requisitar<void>(`/api/decks/${id}`, { metodo: 'DELETE' }),
    [chaves.decks, chaves.jogadores, ['estatisticas-comandantes'], ['perfil']])

// Temporadas
export const useSalvarTemporada = () =>
  useAlteracao(({ id, dados }: { id?: number; dados: TemporadaEnvio }) =>
    id ? requisitar<Temporada>(`/api/temporadas/${id}`, { metodo: 'PUT', corpo: dados })
       : requisitar<Temporada>('/api/temporadas', { metodo: 'POST', corpo: dados }),
  [chaves.temporadas])
export const useSalvarConfiguracaoTemporada = () =>
  useAlteracao(({ id, configuracao }: { id: number; configuracao: ConfiguracaoTemporada }) =>
    requisitar<Temporada>(`/api/temporadas/${id}/configuracao`, { metodo: 'PUT', corpo: configuracao }), [chaves.temporadas])
export const useDefinirProximaEtapa = () =>
  useAlteracao(({ id, proximaEtapaEm, prazoListas }: { id: number; proximaEtapaEm: string | null; prazoListas: string | null }) =>
    requisitar<Temporada>(`/api/temporadas/${id}/proxima-etapa`, { metodo: 'PUT', corpo: { proximaEtapaEm, prazoListas } }), [chaves.temporadas])
export const useSalvarRegulamento = () =>
  useAlteracao((conteudo: string) => requisitar<Regulamento>('/api/regulamento', { metodo: 'PUT', corpo: { conteudo } }), [chaves.regulamento])
export const useEncerrarTemporada = () =>
  useAlteracao((id: number) => requisitar<Temporada>(`/api/temporadas/${id}/encerrar`, { metodo: 'POST' }), [chaves.temporadas])
export const useExcluirTemporada = () =>
  useAlteracao((id: number) => requisitar<void>(`/api/temporadas/${id}`, { metodo: 'DELETE' }), [chaves.temporadas])

// Rodadas — toda alteração invalida temporadas (ranking, histórico, contagens), rodadas, etapas, estatísticas e perfis.
const chavesRodada = [chaves.temporadas, ['rodadas'], ['etapas'], ['estatisticas-comandantes'], ['perfil']] as const
/** Nova rodada: pela data (cria a etapa se for um dia novo; data passada = retroativa) ou numa etapa existente (idEtapa). */
export const useCriarRodada = () =>
  useAlteracao(({ idTemporada, data, duracaoMinutos, idEtapa }: { idTemporada: number; data: string | null; duracaoMinutos: number | null; idEtapa?: number | null }) =>
    requisitar<Rodada>(`/api/temporadas/${idTemporada}/rodadas`, { metodo: 'POST', corpo: { data, duracaoMinutos, idEtapa: idEtapa ?? null } }), chavesRodada)
export const useCriarMesaFinal = () =>
  useAlteracao(({ idTemporada, ...corpo }: { idTemporada: number; data: string; quantidadeRodadas: number; finalistas: { idJogador: number; idDeck: number }[] }) =>
    requisitar<Rodada>(`/api/temporadas/${idTemporada}/mesa-final`, { metodo: 'POST', corpo }), chavesRodada)
/** Mesas montadas à mão: cada mesa = ids de participação na ordem das cadeiras. Quem ficar de fora é bye. */
export const useMontarMesas = () =>
  useAlteracao(({ idRodada, mesas }: { idRodada: number; mesas: number[][] }) =>
    requisitar<Rodada>(`/api/rodadas/${idRodada}/mesas`, { metodo: 'PUT', corpo: { mesas } }), chavesRodada)
export const useDefinirQuemComeca = () =>
  useAlteracao(({ idMesa, idParticipacao }: { idMesa: number; idParticipacao: number }) =>
    requisitar<Rodada>(`/api/mesas/${idMesa}/quem-comeca`, { metodo: 'PUT', corpo: { idParticipacao } }), chavesRodada)
export const useDefinirParticipantes = () =>
  useAlteracao(({ idRodada, participantes }: { idRodada: number; participantes: { idJogador: number; idDeck: number }[] }) =>
    requisitar<Rodada>(`/api/rodadas/${idRodada}/participantes`, { metodo: 'PUT', corpo: participantes }), chavesRodada)
export const useSortearRodada = () =>
  useAlteracao((idRodada: number) => requisitar<Rodada>(`/api/rodadas/${idRodada}/sortear`, { metodo: 'POST' }), chavesRodada)
export const useIniciarRodada = () =>
  useAlteracao(({ idRodada, duracaoMinutos }: { idRodada: number; duracaoMinutos: number }) =>
    requisitar<Rodada>(`/api/rodadas/${idRodada}/iniciar`, { metodo: 'POST', corpo: { duracaoMinutos } }), chavesRodada)
export const useRegistrarResultadoMesa = () =>
  useAlteracao(({ idMesa, resultado, idParticipacaoVencedora }: { idMesa: number; resultado: ResultadoMesa; idParticipacaoVencedora: number | null }) =>
    requisitar<Rodada>(`/api/mesas/${idMesa}/resultado`, { metodo: 'PUT', corpo: { resultado, idParticipacaoVencedora } }), chavesRodada)
export const useEncerrarRodada = () =>
  useAlteracao((idRodada: number) => requisitar<Rodada>(`/api/rodadas/${idRodada}/encerrar`, { metodo: 'POST' }), chavesRodada)
export const useExcluirRodada = () =>
  useAlteracao((idRodada: number) => requisitar<void>(`/api/rodadas/${idRodada}`, { metodo: 'DELETE' }), chavesRodada)
