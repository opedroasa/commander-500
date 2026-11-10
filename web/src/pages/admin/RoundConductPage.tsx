import { AlertTriangle, Check, ExternalLink, Flag, Handshake, Pencil, Play, Search, Shuffle, Trash2, Users } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import {
  useCloseRound, useDecks, useDeleteRound, useDrawRound, usePlayers, useRound, useSetEntries, useSetTableResult, useStartRound,
} from '../../api/hooks'
import type { Deck, Round, Table } from '../../api/types'
import { Countdown } from '../../components/Countdown'
import { ByeList } from '../../components/RoundBoard'
import { TableView } from '../../components/TableView'
import { DeckStatusBadge, Empty, ErrorBox, InlineError, RoundStatusBadge, Spinner } from '../../components/ui'
import { deckStatusLabel, formatDate, formatDateTime, playerLabel, searchKey } from '../../lib/format'

const steps = [
  { status: 'Draft', label: 'Presentes' },
  { status: 'Drawn', label: 'Sorteio' },
  { status: 'InProgress', label: 'Jogo' },
  { status: 'Closed', label: 'Encerrada' },
] as const

/** Admin round flow (spec 6.4): presentes → sorteio → timer → resultados → encerrar. */
export function RoundConductPage() {
  const id = Number(useParams().id)
  const round = useRound(id)
  const navigate = useNavigate()
  const del = useDeleteRound()
  const [editingEntries, setEditingEntries] = useState(false)

  if (round.isPending) return <Spinner />
  if (round.error) return <ErrorBox error={round.error} onRetry={() => round.refetch()} />
  const r = round.data
  const stepIndex = steps.findIndex((s) => s.status === r.status)
  const canDelete = r.status === 'Draft' || r.status === 'Drawn'

  async function remove() {
    if (!confirm(`Excluir a rodada ${r.number}? Os presentes e o sorteio serão perdidos.`)) return
    await del.mutateAsync(r.id)
    navigate('/admin')
  }

  return (
    <>
      <div className="mb-4 flex flex-wrap items-center gap-x-3 gap-y-2">
        <h1 className="font-display text-2xl font-bold text-amber-100">Rodada {r.number}</h1>
        <RoundStatusBadge status={r.status} />
        <span className="text-sm text-zinc-400">{r.seasonName} · {formatDate(r.date)}</span>
        <div className="ml-auto flex gap-2">
          <Link to={`/rodadas/${r.id}`} target="_blank" className="btn-secondary btn-sm">
            <ExternalLink className="size-4" aria-hidden /> Tela pública
          </Link>
          {canDelete && (
            <button className="btn-danger btn-sm" onClick={remove} disabled={del.isPending}>
              <Trash2 className="size-4" aria-hidden /> Excluir
            </button>
          )}
        </div>
      </div>
      <InlineError error={del.error} />

      <ol className="mb-6 grid grid-cols-4 gap-1 text-center text-xs">
        {steps.map((s, i) => (
          <li
            key={s.status}
            className={`rounded-lg px-1 py-2 font-medium ${
              i < stepIndex ? 'bg-zinc-800 text-zinc-300' : i === stepIndex ? 'bg-amber-500 text-zinc-950' : 'bg-zinc-900 text-zinc-500'
            }`}
          >
            {i < stepIndex && <Check className="mr-1 inline size-3" aria-hidden />}{s.label}
          </li>
        ))}
      </ol>

      {r.status === 'Draft' && <EntriesEditor round={r} onDone={() => setEditingEntries(false)} />}

      {r.status === 'Drawn' && (editingEntries
        ? <EntriesEditor round={r} onDone={() => setEditingEntries(false)} onCancel={() => setEditingEntries(false)} />
        : <DrawnStep round={r} onEditEntries={() => setEditingEntries(true)} />)}

      {(r.status === 'InProgress' || r.status === 'Closed') && <PlayStep round={r} />}
    </>
  )
}

// ---------------------------------------------------------------- Step 1: presentes + decks

function EntriesEditor({ round, onDone, onCancel }: { round: Round; onDone: () => void; onCancel?: () => void }) {
  const players = usePlayers()
  const decks = useDecks()
  const setEntries = useSetEntries()
  const draw = useDrawRound()
  const [query, setQuery] = useState('')
  const [selected, setSelected] = useState<Map<number, number>>(
    () => new Map(round.entries.map((e) => [e.playerId, e.deckId])),
  )

  const decksByPlayer = useMemo(() => {
    const map = new Map<number, Deck[]>()
    for (const d of decks.data ?? []) map.set(d.playerId, [...(map.get(d.playerId) ?? []), d])
    return map
  }, [decks.data])

  if (players.isPending || decks.isPending) return <Spinner />
  if (players.error) return <ErrorBox error={players.error} />
  if (decks.error) return <ErrorBox error={decks.error} />

  const q = searchKey(query)
  const list = players.data
    .filter((p) => p.isActive || selected.has(p.id))
    .filter((p) => !q || searchKey(`${p.name} ${p.nickname ?? ''}`).includes(q))

  const n = selected.size
  const tables = Math.floor(n / 4)
  const byes = n % 4

  function toggle(playerId: number) {
    const next = new Map(selected)
    if (next.has(playerId)) next.delete(playerId)
    else {
      const playerDecks = decksByPlayer.get(playerId) ?? []
      const preferred = playerDecks.find((d) => d.status === 'Approved') ?? playerDecks[0]
      if (!preferred) return
      next.set(playerId, preferred.id)
    }
    setSelected(next)
  }

  function chooseDeck(playerId: number, deckId: number) {
    setSelected(new Map(selected).set(playerId, deckId))
  }

  async function save(andDraw: boolean) {
    const entries = [...selected].map(([playerId, deckId]) => ({ playerId, deckId }))
    await setEntries.mutateAsync({ roundId: round.id, entries })
    if (andDraw) await draw.mutateAsync(round.id)
    onDone()
  }

  const busy = setEntries.isPending || draw.isPending

  return (
    <section>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <h2 className="flex items-center gap-2 font-semibold"><Users className="size-4 text-amber-400" aria-hidden />Quem está presente?</h2>
        {round.status === 'Drawn' && (
          <p className="w-full rounded-lg border border-amber-900 bg-amber-950/40 p-2 text-xs text-amber-200">
            Salvar alterações desfaz o sorteio atual — será preciso sortear de novo.
          </p>
        )}
      </div>
      <div className="relative mb-3">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
        <input type="search" placeholder="Filtrar jogadores" value={query} onChange={(e) => setQuery(e.target.value)} className="w-full pl-9" />
      </div>

      {list.length === 0 && <Empty>Nenhum jogador encontrado. <Link className="text-amber-300 hover:underline" to="/admin/jogadores">Cadastrar jogadores</Link></Empty>}

      <ul className="space-y-1.5 pb-28">
        {list.map((p) => {
          const playerDecks = decksByPlayer.get(p.id) ?? []
          const deckId = selected.get(p.id)
          const deck = playerDecks.find((d) => d.id === deckId)
          const checked = selected.has(p.id)
          return (
            <li key={p.id} className={`rounded-xl border p-2.5 transition ${checked ? 'border-amber-700/60 bg-amber-950/20' : 'border-zinc-800 bg-zinc-900/40'}`}>
              <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
                <label className="flex min-w-0 flex-1 cursor-pointer items-center gap-3">
                  <input type="checkbox" checked={checked} disabled={playerDecks.length === 0} onChange={() => toggle(p.id)} />
                  <span className="min-w-0 truncate font-medium">{playerLabel(p.name, p.nickname)}</span>
                </label>
                {playerDecks.length === 0 ? (
                  <Link to="/admin/decks" className="text-xs text-zinc-500 hover:text-amber-300">sem deck — cadastrar</Link>
                ) : checked && (
                  <div className="flex w-full items-center gap-2 sm:w-auto">
                    <select value={deckId} onChange={(e) => chooseDeck(p.id, Number(e.target.value))} className="min-w-0 flex-1 sm:w-72" aria-label={`Deck de ${p.name}`}>
                      {playerDecks.map((d) => (
                        <option key={d.id} value={d.id}>
                          {d.name} — {d.commanders.map((c) => c.name).join(' + ')}{d.status !== 'Approved' ? ` (${deckStatusLabel[d.status]})` : ''}
                        </option>
                      ))}
                    </select>
                    {deck && deck.status !== 'Approved' && <DeckStatusBadge status={deck.status} />}
                  </div>
                )}
              </div>
            </li>
          )
        })}
      </ul>

      {/* sticky action bar */}
      <div className="fixed inset-x-0 bottom-0 z-10 border-t border-zinc-800 bg-zinc-950/95 px-4 py-3 backdrop-blur" style={{ paddingBottom: 'max(0.75rem, env(safe-area-inset-bottom))' }}>
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-2">
          <div className="mr-auto text-sm">
            <b className="text-zinc-100">{n}</b> presente{n === 1 ? '' : 's'}
            {n >= 4
              ? <span className="text-zinc-400"> · {tables} mesa{tables > 1 ? 's' : ''}{byes ? ` · ${byes} bye${byes > 1 ? 's' : ''}` : ''}</span>
              : <span className="text-amber-300"> · mínimo de 4 para sortear</span>}
            <InlineError error={setEntries.error ?? draw.error} />
          </div>
          {onCancel && <button className="btn-ghost" onClick={onCancel} disabled={busy}>Cancelar</button>}
          <button className="btn-secondary" onClick={() => save(false)} disabled={busy}>Salvar</button>
          <button className="btn-primary" onClick={() => save(true)} disabled={busy || n < 4}>
            <Shuffle className="size-4" aria-hidden /> {busy ? 'Salvando…' : 'Salvar e sortear'}
          </button>
        </div>
      </div>
    </section>
  )
}

// ---------------------------------------------------------------- Step 2: sorteio

function DrawnStep({ round, onEditEntries }: { round: Round; onEditEntries: () => void }) {
  const draw = useDrawRound()
  const start = useStartRound()
  const [minutes, setMinutes] = useState(round.durationMinutes)
  const warnings = round.entries.filter((e) => !e.isBye && e.deckStatus !== 'Approved')

  async function redraw() {
    if (confirm('Sortear as mesas novamente? O sorteio atual será substituído.')) await draw.mutateAsync(round.id)
  }

  return (
    <section className="space-y-5">
      <div className="card flex flex-wrap items-end gap-3 p-4">
        <label className="block">
          <span className="label">Duração (min)</span>
          <input type="number" min={1} max={600} value={minutes} onChange={(e) => setMinutes(Number(e.target.value))} className="w-28" />
        </label>
        <button
          className="btn-primary"
          disabled={start.isPending || minutes < 1}
          onClick={() => start.mutate({ roundId: round.id, durationMinutes: minutes })}
        >
          <Play className="size-4" aria-hidden /> Iniciar rodada
        </button>
        <div className="ml-auto flex flex-wrap gap-2">
          <button className="btn-secondary" onClick={redraw} disabled={draw.isPending}>
            <Shuffle className="size-4" aria-hidden /> Sortear novamente
          </button>
          <button className="btn-secondary" onClick={onEditEntries}>
            <Pencil className="size-4" aria-hidden /> Editar presentes
          </button>
        </div>
        <div className="w-full"><InlineError error={start.error ?? draw.error} /></div>
      </div>

      {warnings.length > 0 && <DeckWarnings names={warnings.map((w) => `${w.playerName} (${deckStatusLabel[w.deckStatus].toLowerCase()})`)} />}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {round.tables.map((t) => (
          <section key={t.id} className="card p-3" aria-label={`Mesa ${t.number}`}><TableView table={t} /></section>
        ))}
      </div>
      <ByeList round={round} />
    </section>
  )
}

function DeckWarnings({ names }: { names: string[] }) {
  return (
    <div className="flex items-start gap-2 rounded-xl border border-amber-900 bg-amber-950/30 p-3 text-sm text-amber-200">
      <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
      <span>Deck não aprovado: {names.join(', ')}. Não bloqueia a rodada.</span>
    </div>
  )
}

// ---------------------------------------------------------------- Step 3/4: jogo, resultados, encerrar

function PlayStep({ round }: { round: Round }) {
  const close = useCloseRound()
  const pending = round.tables.filter((t) => t.result === 'Pending')
  const isClosed = round.status === 'Closed'

  async function closeRound() {
    if (confirm(`Encerrar a rodada ${round.number}? Os pontos entram no ranking.`)) await close.mutateAsync(round.id)
  }

  return (
    <section className="space-y-5">
      {!isClosed && round.timer && (
        <Countdown timer={round.timer} serverNow={round.serverNow} extraTurns={round.extraTurnsAfterTime} />
      )}

      {isClosed ? (
        <p className="rounded-xl border border-zinc-800 bg-zinc-900/60 p-3 text-sm text-zinc-300">
          Rodada encerrada em {formatDateTime(round.closedAt)}. Resultados ainda podem ser corrigidos — o ranking é recalculado.
        </p>
      ) : (
        <p className="text-sm text-zinc-400">Toque no jogador vencedor de cada mesa e confirme, ou marque empate.</p>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {round.tables.map((t) => <TableResultCard key={t.id} round={round} table={t} />)}
      </div>
      <ByeList round={round} />

      {!isClosed && (
        <div className="card flex flex-wrap items-center gap-3 p-4">
          <span className="mr-auto text-sm text-zinc-300">
            {pending.length === 0 ? 'Todas as mesas têm resultado.' : `Faltam ${pending.length} mesa${pending.length > 1 ? 's' : ''}: ${pending.map((t) => t.number).join(', ')}.`}
          </span>
          <button className="btn-primary" onClick={closeRound} disabled={pending.length > 0 || close.isPending}>
            <Flag className="size-4" aria-hidden /> Encerrar rodada
          </button>
          <div className="w-full"><InlineError error={close.error} /></div>
        </div>
      )}
    </section>
  )
}

function TableResultCard({ round, table }: { round: Round; table: Table }) {
  const save = useSetTableResult()
  const isClosed = round.status === 'Closed'
  const [editing, setEditing] = useState(table.result === 'Pending' && !isClosed)
  const [winner, setWinner] = useState<number | null>(null)
  const winnerName = table.seats.find((s) => s.entry.id === winner)?.entry.playerName

  async function submit(result: 'Win' | 'Draw') {
    if (isClosed && !confirm('Corrigir o resultado de uma rodada encerrada? O ranking será recalculado.')) return
    await save.mutateAsync({ tableId: table.id, result, winnerEntryId: result === 'Win' ? winner : null })
    setWinner(null)
    setEditing(false)
  }

  return (
    <section className={`card p-3 ${editing ? 'ring-1 ring-amber-700/60' : ''}`} aria-label={`Mesa ${table.number}`}>
      <TableView
        table={table}
        onSeatClick={editing ? (s) => setWinner(s.entry.id === winner ? null : s.entry.id) : undefined}
        selectedEntryId={editing ? winner : undefined}
      />
      <div className="mt-3 flex flex-wrap items-center gap-2">
        {editing ? (
          <>
            <button className="btn-primary btn-sm flex-1" disabled={winner == null || save.isPending} onClick={() => submit('Win')}>
              <Check className="size-4" aria-hidden /> {winnerName ? `Vitória: ${winnerName}` : 'Selecione o vencedor'}
            </button>
            <button className="btn-secondary btn-sm" disabled={save.isPending} onClick={() => submit('Draw')}>
              <Handshake className="size-4" aria-hidden /> Empate
            </button>
            {table.result !== 'Pending' && (
              <button className="btn-ghost btn-sm" onClick={() => { setEditing(false); setWinner(null) }}>Cancelar</button>
            )}
          </>
        ) : (
          <>
            <span className="mr-auto text-sm text-zinc-300">
              {table.result === 'Draw' ? 'Empate' : table.result === 'Win'
                ? <>Vitória: <b>{table.seats.find((s) => s.entry.id === table.winnerEntryId)?.entry.playerName}</b></>
                : 'Sem resultado'}
            </span>
            <button className="btn-ghost btn-sm" onClick={() => setEditing(true)}>
              <Pencil className="size-3.5" aria-hidden /> {isClosed ? 'Corrigir' : 'Alterar'}
            </button>
          </>
        )}
      </div>
      {table.updatedBy && !editing && (
        <p className="mt-1 text-[0.7rem] text-zinc-500">Registrado por {table.updatedBy} em {formatDateTime(table.updatedAt)}</p>
      )}
      <InlineError error={save.error} />
    </section>
  )
}
