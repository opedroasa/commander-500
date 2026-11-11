import { AlertTriangle, Crown, Handshake } from 'lucide-react'
import type { Seat, Table } from '../api/types'
import { deckStatusLabel } from '../lib/format'

/**
 * Rectangular store table: two players on each side.
 * Turn order runs clockwise: 1 (top-left, starts) → 2 (top-right) → 3 (bottom-right) → 4 (bottom-left).
 */
const seatArea: Record<number, string> = {
  1: 'col-start-1 row-start-1',
  2: 'col-start-2 row-start-1',
  3: 'col-start-2 row-start-3',
  4: 'col-start-1 row-start-3',
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

/** Visual pod: a rectangular table with 2 chairs per side, player + commander in each (spec 4.3). */
export function TableView({ table, highlightPlayerIds, onSeatClick, selectedEntryId, showDeckWarnings = true }: Props) {
  const winnerId = selectedEntryId !== undefined ? selectedEntryId : table.winnerEntryId
  const decided = table.result !== 'Pending' || selectedEntryId != null
  const isDraw = table.result === 'Draw' && selectedEntryId == null

  return (
    <div className="grid grid-cols-2 gap-1.5 sm:gap-2">
      {/* the table: felt with wooden border, spanning both columns between the two sides */}
      <div className="col-span-2 row-start-2 flex h-16 items-center justify-center gap-3 rounded-lg border-[5px] border-[#6b3f1d] bg-[radial-gradient(ellipse_at_center,#1f5c3a,#0f3a24_75%)] shadow-[inset_0_0_18px_rgba(0,0,0,.55),0_2px_6px_rgba(0,0,0,.35)]">
        <span className="font-display text-xs uppercase tracking-[0.3em] text-[#cfe8d6]/80">Mesa</span>
        <span className="font-display text-3xl font-bold leading-none text-white">{table.number}</span>
        {isDraw && (
          <span className="inline-flex items-center gap-1 rounded-full bg-black/40 px-2 py-0.5 text-[0.7rem] font-semibold text-white">
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
      className={`${className} relative isolate flex min-h-[5.5rem] min-w-0 flex-col justify-end overflow-hidden rounded-xl border p-2 text-left transition ${
        isWinner
          ? 'border-amber-400 shadow-[0_0_0_1px_rgba(242,101,34,.6),0_0_18px_rgba(242,101,34,.3)]'
          : highlighted
            ? 'border-sky-400 ring-2 ring-sky-400/40'
            : 'border-zinc-800'
      } bg-zinc-900 ${dimmed ? 'opacity-50' : ''} ${onClick ? 'cursor-pointer hover:border-amber-500/70 active:scale-[.98]' : ''}`}
    >
      {/* commander art as a faded background */}
      {entry.commanderArtUrl && (
        <>
          <img src={entry.commanderArtUrl} alt="" loading="lazy" className="absolute inset-0 -z-20 size-full object-cover opacity-60" />
          <div className="absolute inset-0 -z-10 bg-gradient-to-t from-zinc-950 via-zinc-950/80 to-zinc-950/30" />
        </>
      )}
      <div className="mb-auto flex items-center gap-1.5 pb-1">
        <span
          className={`flex size-5 shrink-0 items-center justify-center rounded-full text-[0.65rem] font-bold ${
            seat.seat === 1 ? 'bg-brand-orange text-white' : 'bg-zinc-700 text-zinc-100'
          }`}
          title={seat.seat === 1 ? 'Cadeira 1 — começa jogando' : `Cadeira ${seat.seat}`}
        >
          {seat.seat}
        </span>
        {seat.seat === 1 && <span className="text-[0.6rem] font-semibold uppercase tracking-wide text-amber-400">começa</span>}
        {isWinner && <Crown className="ml-auto size-4 shrink-0 text-amber-400" aria-label="Vencedor" />}
      </div>
      <div className="line-clamp-2 text-sm font-semibold leading-tight text-zinc-50 break-words">{entry.playerName}</div>
      <div className="mt-0.5 line-clamp-2 text-[0.7rem] leading-tight text-zinc-300 break-words">{entry.commanders || entry.deckName}</div>
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
