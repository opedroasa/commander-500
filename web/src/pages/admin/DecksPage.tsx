import { Check, ExternalLink, Pencil, Plus, Trash2, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { useCommanders, useDecks, useDeleteDeck, usePlayers, useReviewDeck, useSaveDeck } from '../../api/hooks'
import type { Deck, DeckStatus } from '../../api/types'
import { DeckStatusBadge, Empty, ErrorBox, Field, InlineError, Modal, PageHeader, Spinner } from '../../components/ui'
import { deckStatusLabel, playerLabel, searchKey } from '../../lib/format'

export function DecksPage() {
  const decks = useDecks()
  const players = usePlayers()
  const del = useDeleteDeck()
  const [params, setParams] = useSearchParams()
  const playerFilter = Number(params.get('jogador')) || 0
  const statusFilter = (params.get('status') ?? '') as DeckStatus | ''
  const [query, setQuery] = useState('')
  const [editing, setEditing] = useState<Deck | 'new' | null>(null)
  const [reviewing, setReviewing] = useState<Deck | null>(null)

  if (decks.isPending || players.isPending) return <Spinner />
  if (decks.error) return <ErrorBox error={decks.error} onRetry={() => decks.refetch()} />
  if (players.error) return <ErrorBox error={players.error} />

  const q = searchKey(query)
  const list = decks.data
    .filter((d) => !playerFilter || d.playerId === playerFilter)
    .filter((d) => !statusFilter || d.status === statusFilter)
    .filter((d) => !q || searchKey(`${d.name} ${d.playerName} ${d.commanders.map((c) => c.name).join(' ')}`).includes(q))

  function setFilter(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  async function remove(d: Deck) {
    if (confirm(`Excluir o deck "${d.name}" de ${d.playerName}?`)) await del.mutateAsync(d.id)
  }

  const pendingCount = decks.data.filter((d) => d.status === 'Pending').length

  return (
    <>
      <PageHeader
        title="Decks"
        subtitle={pendingCount ? `${pendingCount} aguardando aprovação` : 'Todos revisados'}
        actions={<button className="btn-primary" onClick={() => setEditing('new')}><Plus className="size-4" aria-hidden />Novo deck</button>}
      />
      <div className="mb-3 grid gap-2 sm:grid-cols-3">
        <input type="search" placeholder="Buscar deck, jogador ou comandante" value={query} onChange={(e) => setQuery(e.target.value)} />
        <select value={playerFilter || ''} onChange={(e) => setFilter('jogador', e.target.value)} aria-label="Filtrar por jogador">
          <option value="">Todos os jogadores</option>
          {players.data.map((p) => <option key={p.id} value={p.id}>{playerLabel(p.name, p.nickname)}</option>)}
        </select>
        <select value={statusFilter} onChange={(e) => setFilter('status', e.target.value)} aria-label="Filtrar por status">
          <option value="">Todos os status</option>
          {(['Pending', 'Approved', 'Rejected'] as const).map((s) => <option key={s} value={s}>{deckStatusLabel[s]}</option>)}
        </select>
      </div>
      <InlineError error={del.error} />
      {list.length === 0 && <Empty>Nenhum deck encontrado.</Empty>}
      <ul className="space-y-2">
        {list.map((d) => (
          <li key={d.id} className="card p-3">
            <div className="flex flex-wrap items-start gap-x-3 gap-y-1">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-semibold">{d.name}</span>
                  <DeckStatusBadge status={d.status} />
                </div>
                <div className="text-sm text-amber-100/80">{d.commanders.map((c) => c.name).join(' + ')}</div>
                <div className="text-xs text-zinc-400">{d.playerName}</div>
                {d.reviewNote && <div className="mt-1 text-xs text-zinc-400">Obs.: {d.reviewNote}</div>}
              </div>
              <div className="flex items-center gap-1">
                {d.listUrl && (
                  <a href={d.listUrl} target="_blank" rel="noreferrer noopener" className="btn-ghost btn-sm" aria-label="Abrir lista">
                    <ExternalLink className="size-4" />
                  </a>
                )}
                <button className="btn-secondary btn-sm" onClick={() => setReviewing(d)}>Revisar</button>
                <button className="btn-ghost btn-sm" onClick={() => setEditing(d)} aria-label="Editar"><Pencil className="size-4" /></button>
                <button className="btn-ghost btn-sm text-red-300" onClick={() => remove(d)} aria-label="Excluir"><Trash2 className="size-4" /></button>
              </div>
            </div>
          </li>
        ))}
      </ul>
      <DeckModal
        key={`deck-${editing === 'new' ? `new-${playerFilter}` : editing?.id ?? 'none'}`}
        deck={editing}
        defaultPlayerId={playerFilter}
        onClose={() => setEditing(null)}
      />
      <ReviewModal key={`review-${reviewing?.id ?? 'none'}`} deck={reviewing} onClose={() => setReviewing(null)} />
    </>
  )
}

function DeckModal({ deck, defaultPlayerId, onClose }: { deck: Deck | 'new' | null; defaultPlayerId: number; onClose: () => void }) {
  const players = usePlayers()
  const commanders = useCommanders()
  const save = useSaveDeck()
  const existing = deck && deck !== 'new' ? deck : null
  const [playerId, setPlayerId] = useState(existing?.playerId ?? defaultPlayerId ?? 0)
  const [name, setName] = useState(existing?.name ?? '')
  const [listUrl, setListUrl] = useState(existing?.listUrl ?? '')
  const [cmd1, setCmd1] = useState(existing?.commanders[0]?.name ?? '')
  const [cmd2, setCmd2] = useState(existing?.commanders[1]?.name ?? '')
  const [hasPartner, setHasPartner] = useState((existing?.commanders.length ?? 1) > 1)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const names = [cmd1, hasPartner ? cmd2 : ''].map((s) => s.trim()).filter(Boolean)
    await save.mutateAsync({ id: existing?.id, data: { playerId, name, listUrl: listUrl.trim() || null, commanders: names } })
    onClose()
  }

  return (
    <Modal open={deck !== null} onClose={onClose} title={existing ? 'Editar deck' : 'Novo deck'}>
      <form onSubmit={submit} className="space-y-4">
        <Field label="Jogador">
          <select required value={playerId || ''} onChange={(e) => setPlayerId(Number(e.target.value))} className="w-full">
            <option value="" disabled>Selecione…</option>
            {players.data?.map((p) => <option key={p.id} value={p.id}>{playerLabel(p.name, p.nickname)}</option>)}
          </select>
        </Field>
        <Field label="Nome do deck"><input required value={name} onChange={(e) => setName(e.target.value)} className="w-full" /></Field>
        <datalist id="commander-names">
          {commanders.data?.map((c) => <option key={c.id} value={c.name} />)}
        </datalist>
        <Field label="Comandante" hint="Escolha um nome existente para não dividir as estatísticas.">
          <input required list="commander-names" value={cmd1} onChange={(e) => setCmd1(e.target.value)} className="w-full" autoComplete="off" />
        </Field>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={hasPartner} onChange={(e) => setHasPartner(e.target.checked)} /> Tem parceiro / background
        </label>
        {hasPartner && (
          <Field label="Segundo comandante">
            <input required list="commander-names" value={cmd2} onChange={(e) => setCmd2(e.target.value)} className="w-full" autoComplete="off" />
          </Field>
        )}
        <Field label="Link da lista (opcional)">
          <input type="url" placeholder="https://…" value={listUrl} onChange={(e) => setListUrl(e.target.value)} className="w-full" />
        </Field>
        {!existing && <p className="text-xs text-zinc-500">Decks novos entram como Pendente.</p>}
        <InlineError error={save.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className="btn-primary" disabled={save.isPending}>Salvar</button>
        </div>
      </form>
    </Modal>
  )
}

function ReviewModal({ deck, onClose }: { deck: Deck | null; onClose: () => void }) {
  const review = useReviewDeck()
  const [note, setNote] = useState(deck?.reviewNote ?? '')

  async function set(status: DeckStatus) {
    if (!deck) return
    await review.mutateAsync({ id: deck.id, status, note: note.trim() || null })
    onClose()
  }

  return (
    <Modal open={deck !== null} onClose={onClose} title={`Revisar: ${deck?.name ?? ''}`}>
      {deck && (
        <div className="space-y-4">
          <p className="text-sm text-zinc-300">
            {deck.playerName} · {deck.commanders.map((c) => c.name).join(' + ')}
            {deck.listUrl && <> · <a href={deck.listUrl} target="_blank" rel="noreferrer noopener" className="text-amber-300 hover:underline">ver lista</a></>}
          </p>
          <p className="text-xs text-zinc-500">O orçamento é conferido fora do sistema. O status não bloqueia o uso do deck nas rodadas.</p>
          <Field label="Observação" hint="Obrigatória para reprovar.">
            <textarea rows={3} value={note} onChange={(e) => setNote(e.target.value)} className="w-full" />
          </Field>
          <InlineError error={review.error} />
          <div className="flex flex-wrap justify-end gap-2">
            <button className="btn-ghost" onClick={() => set('Pending')} disabled={review.isPending}>Voltar a pendente</button>
            <button className="btn-danger" onClick={() => set('Rejected')} disabled={review.isPending || !note.trim()}>
              <X className="size-4" aria-hidden /> Reprovar
            </button>
            <button className="btn-primary" onClick={() => set('Approved')} disabled={review.isPending}>
              <Check className="size-4" aria-hidden /> Aprovar
            </button>
          </div>
        </div>
      )}
    </Modal>
  )
}
