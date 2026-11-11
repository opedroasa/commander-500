import type { Commander, CommanderStatsRow } from '../api/types'

export type CommanderSort = 'played' | 'wins' | 'rate' | 'decks'

export const commanderSortLabel: Record<CommanderSort, string> = {
  played: 'Mais jogados',
  wins: 'Mais vitórias',
  rate: '% de vitória',
  decks: 'Mais decks',
}

const byName = (a: CommanderStatsRow, b: CommanderStatsRow) => a.name.localeCompare(b.name)
const sorters: Record<CommanderSort, (a: CommanderStatsRow, b: CommanderStatsRow) => number> = {
  played: (a, b) => b.matches - a.matches || b.wins - a.wins || byName(a, b),
  wins: (a, b) => b.wins - a.wins || (b.winRate ?? -1) - (a.winRate ?? -1) || byName(a, b),
  rate: (a, b) => (b.winRate ?? -1) - (a.winRate ?? -1) || b.matches - a.matches || byName(a, b),
  decks: (a, b) => b.decks - a.decks || b.matches - a.matches || byName(a, b),
}

/** Sorted copy. "rate" lists only commanders that reached the minimum matches; "decks" only those with decks. */
export function sortCommanders(rows: CommanderStatsRow[], sort: CommanderSort): CommanderStatsRow[] {
  const list = sort === 'rate'
    ? rows.filter((r) => r.winRate !== null)
    : sort === 'decks' ? rows.filter((r) => r.decks > 0) : rows.filter((r) => r.matches > 0)
  return [...list].sort(sorters[sort])
}

/** Union of the color identities of a commander set (pair = both), WUBRG order. Null when unknown. */
export function combinedIdentity(commanders: Pick<Commander, 'colorIdentity'>[]): string | null {
  if (commanders.every((c) => c.colorIdentity == null)) return null
  const all = commanders.map((c) => c.colorIdentity ?? '').join('')
  return 'WUBRG'.split('').filter((l) => all.includes(l)).join('')
}
