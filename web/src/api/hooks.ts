import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type {
  Commander, CommanderStatsRow, Deck, DeckStatus, DeckUpsert, Player, PlayerUpsert,
  RankingRow, Round, Season, SeasonConfig, SeasonUpsert, TableResult,
} from './types'

export const keys = {
  seasons: ['seasons'] as const,
  season: (id: number) => ['seasons', id] as const,
  ranking: (id: number) => ['seasons', id, 'ranking'] as const,
  commanderStats: (id: number) => ['seasons', id, 'commanders'] as const,
  seasonRounds: (id: number) => ['seasons', id, 'rounds'] as const,
  round: (id: number) => ['rounds', id] as const,
  currentRounds: ['rounds', 'current'] as const,
  players: ['players'] as const,
  decks: ['decks'] as const,
  commanders: ['commanders'] as const,
}

// ---------- Public queries
export const useSeasons = () => useQuery({ queryKey: keys.seasons, queryFn: () => api<Season[]>('/api/seasons') })
export const useSeason = (id: number) => useQuery({ queryKey: keys.season(id), queryFn: () => api<Season>(`/api/seasons/${id}`) })
export const useRanking = (id: number) =>
  useQuery({ queryKey: keys.ranking(id), queryFn: () => api<RankingRow[]>(`/api/seasons/${id}/ranking`) })
export const useCommanderStats = (id: number) =>
  useQuery({ queryKey: keys.commanderStats(id), queryFn: () => api<CommanderStatsRow[]>(`/api/seasons/${id}/commanders`) })
export const useSeasonRounds = (id: number) =>
  useQuery({ queryKey: keys.seasonRounds(id), queryFn: () => api<Round[]>(`/api/seasons/${id}/rounds`) })

/** Live data: polled so every phone sees draws/results without reloading. */
export const useRound = (id: number, pollMs = 0) =>
  useQuery({
    queryKey: keys.round(id),
    queryFn: () => api<Round>(`/api/rounds/${id}`),
    refetchInterval: (q) => (pollMs && q.state.data?.status !== 'Closed' ? pollMs : false),
  })
export const useCurrentRounds = (pollMs = 15000) =>
  useQuery({ queryKey: keys.currentRounds, queryFn: () => api<Round[]>('/api/rounds/current'), refetchInterval: pollMs })

// ---------- Admin queries
export const usePlayers = () => useQuery({ queryKey: keys.players, queryFn: () => api<Player[]>('/api/players') })
export const useDecks = () => useQuery({ queryKey: keys.decks, queryFn: () => api<Deck[]>('/api/decks') })
export const useCommanders = () => useQuery({ queryKey: keys.commanders, queryFn: () => api<Commander[]>('/api/commanders') })

// ---------- Mutations
/** Generic mutation that invalidates the given query prefixes on success. */
function useApiMutation<TVars, TResult = unknown>(fn: (v: TVars) => Promise<TResult>, invalidate: readonly (readonly unknown[])[]) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: async () => {
      await Promise.all(invalidate.map((key) => qc.invalidateQueries({ queryKey: key })))
    },
  })
}

// Players
export const useSavePlayer = () =>
  useApiMutation(({ id, data }: { id?: number; data: PlayerUpsert }) =>
    id ? api<Player>(`/api/players/${id}`, { method: 'PUT', body: data }) : api<Player>('/api/players', { method: 'POST', body: data }),
  [keys.players, keys.decks])
export const useDeletePlayer = () =>
  useApiMutation((id: number) => api<void>(`/api/players/${id}`, { method: 'DELETE' }), [keys.players, keys.decks])

// Decks
export const useSaveDeck = () =>
  useApiMutation(({ id, data }: { id?: number; data: DeckUpsert }) =>
    id ? api<Deck>(`/api/decks/${id}`, { method: 'PUT', body: data }) : api<Deck>('/api/decks', { method: 'POST', body: data }),
  [keys.decks, keys.players, keys.commanders])
export const useReviewDeck = () =>
  useApiMutation(({ id, status, note }: { id: number; status: DeckStatus; note: string | null }) =>
    api<Deck>(`/api/decks/${id}/review`, { method: 'PATCH', body: { status, note } }), [keys.decks, ['rounds']])
export const useDeleteDeck = () =>
  useApiMutation((id: number) => api<void>(`/api/decks/${id}`, { method: 'DELETE' }), [keys.decks, keys.players])

// Commanders
export const useRenameCommander = () =>
  useApiMutation(({ id, name }: { id: number; name: string }) =>
    api<Commander>(`/api/commanders/${id}`, { method: 'PUT', body: { name } }), [keys.commanders, keys.decks, keys.seasons])
export const useDeleteCommander = () =>
  useApiMutation((id: number) => api<void>(`/api/commanders/${id}`, { method: 'DELETE' }), [keys.commanders])

// Seasons
export const useSaveSeason = () =>
  useApiMutation(({ id, data }: { id?: number; data: SeasonUpsert }) =>
    id ? api<Season>(`/api/seasons/${id}`, { method: 'PUT', body: data }) : api<Season>('/api/seasons', { method: 'POST', body: data }),
  [keys.seasons])
export const useSaveSeasonConfig = () =>
  useApiMutation(({ id, config }: { id: number; config: SeasonConfig }) =>
    api<Season>(`/api/seasons/${id}/config`, { method: 'PUT', body: config }), [keys.seasons])
export const useEndSeason = () =>
  useApiMutation((id: number) => api<Season>(`/api/seasons/${id}/end`, { method: 'POST' }), [keys.seasons])
export const useDeleteSeason = () =>
  useApiMutation((id: number) => api<void>(`/api/seasons/${id}`, { method: 'DELETE' }), [keys.seasons])

// Rounds — every change invalidates seasons (ranking, history, counts) and rounds.
const roundKeys = [keys.seasons, ['rounds']] as const
export const useCreateRound = () =>
  useApiMutation(({ seasonId, date, durationMinutes }: { seasonId: number; date: string | null; durationMinutes: number | null }) =>
    api<Round>(`/api/seasons/${seasonId}/rounds`, { method: 'POST', body: { date, durationMinutes } }), roundKeys)
export const useSetEntries = () =>
  useApiMutation(({ roundId, entries }: { roundId: number; entries: { playerId: number; deckId: number }[] }) =>
    api<Round>(`/api/rounds/${roundId}/entries`, { method: 'PUT', body: entries }), roundKeys)
export const useDrawRound = () =>
  useApiMutation((roundId: number) => api<Round>(`/api/rounds/${roundId}/draw`, { method: 'POST' }), roundKeys)
export const useStartRound = () =>
  useApiMutation(({ roundId, durationMinutes }: { roundId: number; durationMinutes: number }) =>
    api<Round>(`/api/rounds/${roundId}/start`, { method: 'POST', body: { durationMinutes } }), roundKeys)
export const useSetTableResult = () =>
  useApiMutation(({ tableId, result, winnerEntryId }: { tableId: number; result: TableResult; winnerEntryId: number | null }) =>
    api<Round>(`/api/tables/${tableId}/result`, { method: 'PUT', body: { result, winnerEntryId } }), roundKeys)
export const useCloseRound = () =>
  useApiMutation((roundId: number) => api<Round>(`/api/rounds/${roundId}/close`, { method: 'POST' }), roundKeys)
export const useDeleteRound = () =>
  useApiMutation((roundId: number) => api<void>(`/api/rounds/${roundId}`, { method: 'DELETE' }), roundKeys)
