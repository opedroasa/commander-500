import { Hourglass, TimerOff } from 'lucide-react'
import { useMemo } from 'react'
import type { Timer } from '../api/types'
import { lastNow, useNow } from '../lib/clock'
import { formatClock } from '../lib/format'

/**
 * Round timer derived from the server's StartedAt + duration (spec 4.4). Uses the server clock
 * offset from the response, so every device shows the same time and reloading never resets it.
 */
export function Countdown({ timer, serverNow, extraTurns = 0, size = 'lg' }: {
  timer: Timer
  serverNow: string
  extraTurns?: number
  size?: 'lg' | 'sm'
}) {
  const clientNow = useNow()
  // Server clock offset, captured when a new response arrives (precision ±0.5 s is plenty for a round timer).
  const offset = useMemo(() => new Date(serverNow).getTime() - lastNow(), [serverNow])
  const endsAt = new Date(timer.endsAt).getTime()
  const now = clientNow + offset

  const remaining = Math.max(0, Math.ceil((endsAt - now) / 1000))
  const total = timer.durationMinutes * 60
  const pct = total > 0 ? remaining / total : 0
  const expired = remaining === 0
  const warning = !expired && remaining <= 5 * 60

  if (size === 'sm') {
    return (
      <span className={`inline-flex items-center gap-1 font-mono text-sm tabular-nums ${expired ? 'text-red-300' : warning ? 'text-amber-300' : 'text-zinc-200'}`}>
        {expired ? <TimerOff className="size-4" aria-hidden /> : <Hourglass className="size-4" aria-hidden />}
        {expired ? 'Tempo esgotado' : formatClock(remaining)}
      </span>
    )
  }

  return (
    <div
      className={`rounded-2xl border p-4 text-center ${
        expired ? 'border-red-800 bg-red-950/40' : warning ? 'border-amber-700 bg-amber-950/30' : 'border-zinc-800 bg-zinc-900/60'
      }`}
      role="timer"
      aria-live="off"
    >
      <div className={`font-mono text-5xl font-bold tabular-nums sm:text-6xl ${expired ? 'text-red-300' : warning ? 'text-amber-300' : 'text-zinc-50'}`}>
        {formatClock(remaining)}
      </div>
      <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-zinc-800">
        <div
          className={`h-full rounded-full transition-[width] duration-500 ${expired ? 'bg-red-500' : warning ? 'bg-amber-400' : 'bg-emerald-500'}`}
          style={{ width: `${pct * 100}%` }}
        />
      </div>
      <p className="mt-2 text-sm text-zinc-400">
        {expired
          ? extraTurns > 0
            ? `Tempo esgotado — joguem mais ${extraTurns} turno${extraTurns > 1 ? 's' : ''}.`
            : 'Tempo esgotado.'
          : `Rodada de ${timer.durationMinutes} min`}
      </p>
    </div>
  )
}
