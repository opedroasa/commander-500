import { Coffee } from 'lucide-react'
import type { Round } from '../api/types'
import { Countdown } from './Countdown'
import { TableView } from './TableView'

/** Read-only round view: timer, tables (visual) and byes. Used by the public live page. */
export function RoundBoard({ round, highlightPlayerIds }: { round: Round; highlightPlayerIds?: Set<number> }) {
  return (
    <div className="space-y-5">
      {round.status === 'InProgress' && round.timer && (
        <Countdown timer={round.timer} serverNow={round.serverNow} extraTurns={round.extraTurnsAfterTime} />
      )}
      {round.status === 'Drawn' && (
        <p className="rounded-xl border border-sky-900 bg-sky-950/40 p-3 text-center text-sm text-sky-200">
          Mesas sorteadas — procurem seus lugares. O timer começa quando o lojista iniciar a rodada.
        </p>
      )}

      <TablesGrid round={round} highlightPlayerIds={highlightPlayerIds} />
      <ByeList round={round} highlightPlayerIds={highlightPlayerIds} />
    </div>
  )
}

export function TablesGrid({ round, highlightPlayerIds }: { round: Round; highlightPlayerIds?: Set<number> }) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {round.tables.map((t) => (
        <section key={t.id} className="card p-3" aria-label={`Mesa ${t.number}`}>
          <TableView table={t} highlightPlayerIds={highlightPlayerIds} />
        </section>
      ))}
    </div>
  )
}

export function ByeList({ round, highlightPlayerIds }: { round: Round; highlightPlayerIds?: Set<number> }) {
  if (round.byes.length === 0) return null
  return (
    <section className="card p-4">
      <h3 className="mb-2 flex items-center gap-2 text-sm font-semibold text-zinc-200">
        <Coffee className="size-4 text-amber-400" aria-hidden /> Bye nesta rodada
      </h3>
      <ul className="flex flex-wrap gap-2">
        {round.byes.map((b) => (
          <li
            key={b.id}
            className={`rounded-lg border px-3 py-1.5 text-sm ${
              highlightPlayerIds?.has(b.playerId) ? 'border-sky-400 bg-sky-500/15' : 'border-zinc-800 bg-zinc-900'
            }`}
          >
            {b.playerName}
          </li>
        ))}
      </ul>
      <p className="mt-2 text-xs text-zinc-500">Quem fica de bye recebe os pontos de bye da temporada sem jogar.</p>
    </section>
  )
}
