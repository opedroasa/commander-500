// Mirrors src/Commander500.Api/Contracts/Dtos.cs (enums are serialized as strings).

export type DeckStatus = 'Pending' | 'Approved' | 'Rejected'
export type RoundStatus = 'Draft' | 'Drawn' | 'InProgress' | 'Closed'
export type TableResult = 'Pending' | 'Win' | 'Draw'
export type TiebreakCriterion = 'Wins' | 'FewerMatches' | 'MoreMatches' | 'Draws' | 'WinRate'

export interface SeasonConfig {
  pointsWin: number
  pointsDraw: number
  pointsBye1: number
  pointsBye2: number
  pointsBye3: number
  defaultRoundMinutes: number
  extraTurnsAfterTime: number
  minMatchesForCommanderWinRate: number
  tiebreakers: TiebreakCriterion[]
}

export interface Season {
  id: number
  name: string
  startDate: string
  endDate: string | null
  isActive: boolean
  configLocked: boolean
  config: SeasonConfig
  roundsCount: number
}

export interface SeasonUpsert {
  name: string
  startDate: string
  endDate: string | null
  isActive: boolean
  config?: SeasonConfig | null
}

export interface RankingRow {
  position: number
  playerId: number
  playerName: string
  points: number
  wins: number
  draws: number
  losses: number
  byes: number
  matches: number
  winRate: number | null
}

export interface CommanderStatsRow {
  name: string
  commanderIds: number[]
  matches: number
  wins: number
  draws: number
  winRate: number | null
}

export interface Player {
  id: number
  name: string
  nickname: string | null
  isActive: boolean
  createdAt: string
  deckCount: number
}

export interface PlayerUpsert {
  name: string
  nickname: string | null
  isActive: boolean
}

export interface Commander {
  id: number
  name: string
}

export interface Deck {
  id: number
  playerId: number
  playerName: string
  name: string
  listUrl: string | null
  status: DeckStatus
  reviewNote: string | null
  createdAt: string
  commanders: Commander[]
}

export interface DeckUpsert {
  playerId: number
  name: string
  listUrl: string | null
  commanders: string[]
}

export interface Entry {
  id: number
  playerId: number
  playerName: string
  nickname: string | null
  deckId: number
  deckName: string
  deckStatus: DeckStatus
  commanders: string
  isBye: boolean
}

export interface Seat {
  seat: number
  entry: Entry
}

export interface Table {
  id: number
  number: number
  result: TableResult
  winnerEntryId: number | null
  updatedAt: string | null
  updatedBy: string | null
  seats: Seat[]
}

export interface Timer {
  startedAt: string
  durationMinutes: number
  endsAt: string
  remainingSeconds: number
  expired: boolean
}

export interface Round {
  id: number
  seasonId: number
  seasonName: string
  number: number
  date: string
  status: RoundStatus
  statusLabel: string
  durationMinutes: number
  extraTurnsAfterTime: number
  serverNow: string
  timer: Timer | null
  closedAt: string | null
  entries: Entry[]
  byes: Entry[]
  tables: Table[]
}

export interface LoginResponse {
  token: string
  expiresAt: string
  email: string
}
