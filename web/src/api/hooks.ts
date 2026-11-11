import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type {
  CommanderStatsRow, Deck, DeckStatus, DeckUpsert, Player, PlayerProfile, PlayerUpsert,
  RankingRow, Round, ScryfallCard, Season, SeasonConfig, SeasonUpsert, Stage, StageDetail, TableResult,
} from './types'

export const keys = {
  seasons: ['seasons'] as const,
  season: (id: number) => ['seasons', id] as const,
  ranking: (id: number) => ['seasons', id, 'ranking'] as const,
  seasonRounds: (id: number) => ['seasons', id, 'rounds'] as const,
  round: (id: number) => ['rounds', id] as const,
  currentRounds: ['rounds', 'current'] as const,
  players: ['players'] as const,
  decks: ['decks'] as const,
  commanderStats: (seasonId: number | null) => ['commander-stats', seasonId ?? 'all'] as const,
  seasonStages: (id: number) => ['seasons', id, 'stages'] as const,
  stage: (id: number) => ['stages', id] as const,
  profile: (id: number, seasonId: number | null) => ['profile', id, seasonId ?? 'all'] as const,
}

// ---------- Public queries
export const useSeasons = () => useQuery({ queryKey: keys.seasons, queryFn: () => api<Season[]>('/api/seasons') })
export const useSeason = (id: number) => useQuery({ queryKey: keys.season(id), queryFn: () => api<Season>(`/api/seasons/${id}`) })
export const useRanking = (id: number) =>
  useQuery({ queryKey: keys.ranking(id), queryFn: () => api<RankingRow[]>(`/api/seasons/${id}/ranking`) })
/** Commander ranking for one season, or every season when null (also lists commanders with decks only). */
export const useCommanderStats = (seasonId: number | null) =>
  useQuery({
    queryKey: keys.commanderStats(seasonId),
    queryFn: () => api<CommanderStatsRow[]>(seasonId ? `/api/seasons/${seasonId}/commanders` : '/api/commanders/stats'),
  })
export const useSeasonStages = (id: number) =>
  useQuery({ queryKey: keys.seasonStages(id), queryFn: () => api<Stage[]>(`/api/seasons/${id}/stages`) })
export const useStage = (id: number, enabled = true) =>
  useQuery({ queryKey: keys.stage(id), queryFn: () => api<StageDetail>(`/api/stages/${id}`), enabled })
export const usePlayerProfile = (id: number | null, seasonId: number | null) =>
  useQuery({
    queryKey: keys.profile(id ?? 0, seasonId),
    queryFn: () => api<PlayerProfile>(`/api/players/${id}/profile${seasonId ? `?seasonId=${seasonId}` : ''}`),
    enabled: id != null,
  })
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
/** Scryfall commander search (admin), at least 2 letters. */
export const useScryfallSearch = (q: string) =>
  useQuery({
    queryKey: ['scryfall', q.trim().toLowerCase()],
    queryFn: () => api<ScryfallCard[]>(`/api/scryfall/commanders?q=${encodeURIComponent(q.trim())}`),
    enabled: q.trim().length >= 2,
    staleTime: 60 * 60_000,
    retry: false,
  })

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
  [keys.players, keys.decks, ['profile']])
export const useDeletePlayer = () =>
  useApiMutation((id: number) => api<void>(`/api/players/${id}`, { method: 'DELETE' }), [keys.players, keys.decks, ['profile']])

// Decks
export const useSaveDeck = () =>
  useApiMutation(({ id, data }: { id?: number; data: DeckUpsert }) =>
    id ? api<Deck>(`/api/decks/${id}`, { method: 'PUT', body: data }) : api<Deck>('/api/decks', { method: 'POST', body: data }),
  [keys.decks, keys.players, ['commander-stats'], ['profile']])
export const useReviewDeck = () =>
  useApiMutation(({ id, status, note }: { id: number; status: DeckStatus; note: string | null }) =>
    api<Deck>(`/api/decks/${id}/review`, { method: 'PATCH', body: { status, note } }), [keys.decks, ['rounds'], ['profile']])
export const useDeleteDeck = () =>
  useApiMutation((id: number) => api<void>(`/api/decks/${id}`, { method: 'DELETE' }), [keys.decks, keys.players, ['commander-stats'], ['profile']])

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
const roundKeys = [keys.seasons, ['rounds'], ['stages'], ['commander-stats'], ['profile']] as const
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
