import type { CommanderStatsRow } from '../api/types'
import { formatPercent } from '../lib/format'
import { combinedIdentity, sortCommanders, type CommanderSort } from '../lib/commanders'
import { ColorPips, CommanderArt } from './CommanderArt'

const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`

/** Big art cards (top N), like "Comandantes mais jogados". */
export function CommanderArtCards({ rows, sort = 'played', limit = 4 }: { rows: CommanderStatsRow[]; sort?: CommanderSort; limit?: number }) {
  const top = sortCommanders(rows, sort).slice(0, limit)
  if (top.length === 0) return null
  return (
    <ol className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      {top.map((r, i) => (
        <li key={r.name} className="group relative overflow-hidden rounded-2xl border border-zinc-800 shadow-lg">
          <CommanderArt commanders={r.commanders} className="aspect-[4/3] w-full transition duration-500 group-hover:scale-105" rounded="rounded-none" />
          <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/30 to-transparent" />
          <span className="absolute left-2 top-2 flex size-7 items-center justify-center rounded-full bg-black/60 text-sm font-bold text-white ring-1 ring-white/20">
            {i + 1}
          </span>
          <div className="absolute inset-x-0 bottom-0 p-3">
            <div className="line-clamp-2 text-sm font-bold leading-tight text-white drop-shadow sm:text-base">{r.name}</div>
            <div className="mt-0.5 text-xs text-white/80">
              {sort === 'decks'
                ? plural(r.decks, 'deck', 'decks')
                : `${plural(r.matches, 'partida', 'partidas')} · ${plural(r.players, 'jogador', 'jogadores')}`}
            </div>
          </div>
        </li>
      ))}
    </ol>
  )
}

/** Ranked list with thumbnail, like "Top comandantes". */
export function CommanderList({ rows, sort, limit, minMatches }: {
  rows: CommanderStatsRow[]
  sort: CommanderSort
  limit?: number
  minMatches?: number
}) {
  const list = sortCommanders(rows, sort).slice(0, limit)
  if (list.length === 0) {
    return (
      <p className="rounded-xl border border-dashed border-zinc-800 p-6 text-center text-sm text-zinc-400">
        {sort === 'rate' && minMatches ? `Nenhum comandante com ${minMatches}+ partidas ainda.` : 'Nenhum dado ainda.'}
      </p>
    )
  }
  return (
    <ol className="divide-y divide-zinc-800/80">
      {list.map((r, i) => {
        const main = sort === 'played' ? r.matches : sort === 'wins' ? r.wins : sort === 'decks' ? r.decks : formatPercent(r.winRate)
        return (
          <li key={r.name} className="flex items-center gap-3 py-2.5">
            <span className="w-5 shrink-0 text-center text-sm font-semibold text-zinc-500">{i + 1}</span>
            <CommanderArt commanders={r.commanders} className="h-10 w-14 shrink-0" rounded="rounded-md" />
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-semibold text-zinc-100">{r.name}</div>
              <div className="flex items-center gap-2 text-xs text-zinc-400">
                <ColorPips identity={combinedIdentity(r.commanders)} />
                <span className="truncate">
                  {plural(r.matches, 'partida', 'partidas')} · {plural(r.wins, 'vitória', 'vitórias')} · {plural(r.decks, 'deck', 'decks')}
                </span>
              </div>
            </div>
            <div className="text-right">
              <div className="text-base font-bold tabular-nums text-amber-300">{main}</div>
              {sort !== 'rate' && (
                <div className="text-[0.7rem] text-zinc-500">
                  {r.winRate === null ? (minMatches ? `% após ${minMatches}` : '–') : `${formatPercent(r.winRate)} vit.`}
                </div>
              )}
            </div>
          </li>
        )
      })}
    </ol>
  )
}
