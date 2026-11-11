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
  stagesCount: number
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
  /** "A" or "A + B" (partner/background pair = one row) */
  name: string
  commanders: Commander[]
  matches: number
  wins: number
  draws: number
  winRate: number | null
  /** registered decks with this commander set */
  decks: number
  /** distinct players who played it */
  players: number
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
  /** cropped artwork from Scryfall */
  artUrl: string | null
  imageUrl: string | null
  /** WUBRG letters; '' = colorless; null = unknown */
  colorIdentity: string | null
}

/** Scryfall search result (commander picker). */
export interface ScryfallCard {
  id: string
  name: string
  artUrl: string | null
  imageUrl: string | null
  colorIdentity: string
  typeLine: string
}

/** Commander chosen in the deck form: from Scryfall (with id) or typed by hand. */
export interface CommanderInput {
  scryfallId: string | null
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
  /** optional: defaults to the commander name(s) */
  name: string | null
  listUrl: string | null
  commanders: CommanderInput[]
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
  commanderArtUrl: string | null
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
  stageId: number
  stageNumber: number
  /** 1, 2, 3… inside the stage (etapa) */
  numberInStage: number
  /** sequential in the season */
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

// ---------- Stages (etapas)
export interface PlayerRef {
  id: number
  name: string
}

export interface Stage {
  id: number
  seasonId: number
  seasonName: string
  number: number
  date: string
  roundsCount: number
  closedRounds: number
  /** all rounds closed: ranking and winners are final */
  isComplete: boolean
  /** position 1 (several when tied) */
  leaders: PlayerRef[]
}

export interface StageRankingRow {
  /** shared by tied players (1, 1, 3…) */
  position: number
  playerId: number
  playerName: string
  /** table wins + byes (a bye counts as a win in the stage) */
  wins: number
  tableWins: number
  byes: number
  draws: number
  losses: number
  matches: number
  headToHeadWins: number
  tied: boolean
}

export interface StageDetail {
  stage: Stage
  ranking: StageRankingRow[]
  rounds: Round[]
}

// ---------- Player profile
export interface PlayerStats {
  /** rounds attended (matches + byes) */
  rounds: number
  matches: number
  wins: number
  draws: number
  losses: number
  byes: number
  winRate: number | null
}

export interface PlayerProfile {
  id: number
  name: string
  nickname: string | null
  isActive: boolean
  overall: PlayerStats
  seasonsPlayed: number
  season: { seasonId: number; seasonName: string; position: number | null; points: number; stats: PlayerStats } | null
  seasonsWon: { id: number; name: string }[]
  stagesWon: { id: number; seasonId: number; seasonName: string; number: number; date: string }[]
  decks: Deck[]
}
