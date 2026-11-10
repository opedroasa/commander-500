import { AlertTriangle, Crown, Handshake } from 'lucide-react'
import type { Seat, Table } from '../api/types'
import { deckStatusLabel } from '../lib/format'

/** Grid cell for each seat around the table, clockwise from the top (seat 1 plays first). */
const seatArea: Record<number, string> = {
  1: 'col-start-2 row-start-1',
  2: 'col-start-3 row-start-2',
  3: 'col-start-2 row-start-3',
  4: 'col-start-1 row-start-2',
}

interface Props {
  table: Table
  /** Players to highlight (e.g. from the "find my seat" search). */
  highlightPlayerIds?: Set<number>
  /** Admin result entry: clicking a seat selects the winner. */
  onSeatClick?: (seat: Seat) => void
  /** Winner currently selected in admin mode (before saving). */
  selectedEntryId?: number | null
  showDeckWarnings?: boolean
}

/** Visual pod: a table with 4 chairs, player + commander in each (spec 4.3). */
export function TableView({ table, highlightPlayerIds, onSeatClick, selectedEntryId, showDeckWarnings = true }: Props) {
  const winnerId = selectedEntryId !== undefined ? selectedEntryId : table.winnerEntryId
  const decided = table.result !== 'Pending' || selectedEntryId != null
  const isDraw = table.result === 'Draw' && selectedEntryId == null

  return (
    <div className="grid grid-cols-3 grid-rows-[auto_auto_auto] items-center gap-1.5 sm:gap-2">
      {/* the table */}
      <div className="col-start-2 row-start-2 flex aspect-square flex-col items-center justify-center rounded-full border-4 border-amber-900/70 bg-[radial-gradient(circle_at_35%_30%,#365314,#1a2e05_70%)] shadow-[inset_0_0_24px_rgba(0,0,0,.6)]">
        <span className="font-display text-[0.65rem] uppercase tracking-widest text-lime-200/70">Mesa</span>
        <span className="font-display text-3xl font-bold leading-none text-lime-50">{table.number}</span>
        {isDraw && (
          <span className="mt-1 inline-flex items-center gap-1 rounded-full bg-zinc-950/60 px-2 py-0.5 text-[0.65rem] font-semibold text-zinc-200">
            <Handshake className="size-3" aria-hidden /> Empate
          </span>
        )}
      </div>

      {table.seats.map((seat) => (
        <SeatCard
          key={seat.seat}
          seat={seat}
          className={seatArea[seat.seat] ?? ''}
          isWinner={winnerId === seat.entry.id && !isDraw}
          dimmed={decided && !isDraw && winnerId !== seat.entry.id && winnerId != null}
          highlighted={highlightPlayerIds?.has(seat.entry.playerId) ?? false}
          onClick={onSeatClick ? () => onSeatClick(seat) : undefined}
          showDeckWarning={showDeckWarnings}
        />
      ))}
    </div>
  )
}

function SeatCard({ seat, className, isWinner, dimmed, highlighted, onClick, showDeckWarning }: {
  seat: Seat
  className: string
  isWinner: boolean
  dimmed: boolean
  highlighted: boolean
  onClick?: () => void
  showDeckWarning: boolean
}) {
  const { entry } = seat
  const notApproved = entry.deckStatus !== 'Approved'
  const Tag = onClick ? 'button' : 'div'

  return (
    <Tag
      type={onClick ? 'button' : undefined}
      onClick={onClick}
      aria-pressed={onClick ? isWinner : undefined}
      aria-label={onClick ? `Marcar ${entry.playerName} como vencedor` : undefined}
      className={`${className} relative flex min-h-24 min-w-0 flex-col justify-center rounded-xl border p-2 text-left transition ${
        isWinner
          ? 'border-amber-400 bg-amber-500/15 shadow-[0_0_0_1px_rgba(251,191,36,.5),0_0_18px_rgba(251,191,36,.25)]'
          : highlighted
            ? 'border-sky-400 bg-sky-500/15'
            : 'border-zinc-800 bg-zinc-900'
      } ${dimmed ? 'opacity-50' : ''} ${onClick ? 'cursor-pointer hover:border-amber-500/70 active:scale-[.98]' : ''}`}
    >
      <div className="mb-1 flex items-center gap-1.5">
        <span
          className={`flex size-5 shrink-0 items-center justify-center rounded-full text-[0.65rem] font-bold ${
            seat.seat === 1 ? 'bg-amber-500 text-zinc-950' : 'bg-zinc-700 text-zinc-200'
          }`}
          title={seat.seat === 1 ? 'Cadeira 1 — começa jogando' : `Cadeira ${seat.seat}`}
        >
          {seat.seat}
        </span>
        {seat.seat === 1 && <span className="text-[0.6rem] font-semibold uppercase tracking-wide text-amber-400">começa</span>}
        {isWinner && <Crown className="ml-auto size-4 shrink-0 text-amber-400" aria-label="Vencedor" />}
      </div>
      <div className="line-clamp-2 text-sm font-semibold leading-tight text-zinc-50 break-words">{entry.playerName}</div>
      <div className="mt-0.5 line-clamp-2 text-[0.7rem] leading-tight text-zinc-400 break-words">{entry.commanders || entry.deckName}</div>
      {showDeckWarning && notApproved && (
        <div
          className={`mt-1 inline-flex items-center gap-1 text-[0.65rem] font-medium ${entry.deckStatus === 'Rejected' ? 'text-red-300' : 'text-amber-300'}`}
          title={`Deck ${deckStatusLabel[entry.deckStatus].toLowerCase()}`}
        >
          <AlertTriangle className="size-3 shrink-0" aria-hidden /> Deck {deckStatusLabel[entry.deckStatus].toLowerCase()}
        </div>
      )}
    </Tag>
  )
}
