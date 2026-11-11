import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useDeletePlayer, usePlayers, useSavePlayer } from '../../api/hooks'
import type { Player } from '../../api/types'
import { PlayerProfileModal } from '../../components/PlayerProfileModal'
import { Empty, ErrorBox, Field, InlineError, Modal, PageHeader, Spinner } from '../../components/ui'
import { searchKey } from '../../lib/format'

export function PlayersPage() {
  const players = usePlayers()
  const del = useDeletePlayer()
  const [editing, setEditing] = useState<Player | 'new' | null>(null)
  const [query, setQuery] = useState('')
  const [showInactive, setShowInactive] = useState(false)
  const [profileId, setProfileId] = useState<number | null>(null)

  if (players.isPending) return <Spinner />
  if (players.error) return <ErrorBox error={players.error} onRetry={() => players.refetch()} />

  const q = searchKey(query)
  const list = players.data
    .filter((p) => showInactive || p.isActive)
    .filter((p) => !q || searchKey(`${p.name} ${p.nickname ?? ''}`).includes(q))

  async function remove(p: Player) {
    if (confirm(`Excluir ${p.name}? Os decks dele também serão excluídos.`)) await del.mutateAsync(p.id)
  }

  return (
    <>
      <PageHeader
        title="Jogadores"
        subtitle={`${players.data.filter((p) => p.isActive).length} ativos`}
        actions={<button className="btn-primary" onClick={() => setEditing('new')}><Plus className="size-4" aria-hidden />Novo jogador</button>}
      />
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <input type="search" placeholder="Buscar" value={query} onChange={(e) => setQuery(e.target.value)} className="min-w-0 flex-1" />
        <label className="flex items-center gap-2 text-sm text-zinc-300">
          <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} /> Mostrar inativos
        </label>
      </div>
      <InlineError error={del.error} />
      {list.length === 0 && <Empty>Nenhum jogador.</Empty>}
      <ul className="space-y-1.5">
        {list.map((p) => (
          <li key={p.id} className={`card flex items-center gap-3 p-1.5 pl-3 ${p.isActive ? '' : 'opacity-60'}`}>
            <button className="min-w-0 flex-1 rounded-lg py-1.5 text-left hover:text-amber-300" onClick={() => setProfileId(p.id)}
              aria-label={`Abrir perfil de ${p.name}`}>
              <div className="truncate font-medium">
                {p.name}
                {p.nickname && <span className="text-zinc-400"> ({p.nickname})</span>}
                {!p.isActive && <span className="ml-2 rounded bg-zinc-800 px-1.5 py-0.5 text-xs text-zinc-400">inativo</span>}
              </div>
              <div className="text-xs text-zinc-400">
                {p.deckCount} deck{p.deckCount === 1 ? '' : 's'} · ver perfil e decks
              </div>
            </button>
            <button className="btn-ghost btn-sm" onClick={() => setEditing(p)} aria-label={`Editar ${p.name}`}><Pencil className="size-4" /></button>
            <button className="btn-ghost btn-sm text-red-300" onClick={() => remove(p)} aria-label={`Excluir ${p.name}`}><Trash2 className="size-4" /></button>
          </li>
        ))}
      </ul>
      <PlayerModal key={`player-${editing === 'new' ? 'new' : editing?.id ?? 'none'}`} player={editing} onClose={() => setEditing(null)} />
      <PlayerProfileModal playerId={profileId} admin onClose={() => setProfileId(null)} />
    </>
  )
}

function PlayerModal({ player, onClose }: { player: Player | 'new' | null; onClose: () => void }) {
  const save = useSavePlayer()
  const existing = player && player !== 'new' ? player : null
  const [name, setName] = useState(existing?.name ?? '')
  const [nickname, setNickname] = useState(existing?.nickname ?? '')
  const [isActive, setIsActive] = useState(existing?.isActive ?? true)

  async function submit(e: FormEvent) {
    e.preventDefault()
    await save.mutateAsync({ id: existing?.id, data: { name, nickname: nickname || null, isActive } })
    onClose()
  }

  return (
    <Modal open={player !== null} onClose={onClose} title={existing ? 'Editar jogador' : 'Novo jogador'}>
      <form onSubmit={submit} className="space-y-4">
        <Field label="Nome"><input required value={name} onChange={(e) => setName(e.target.value)} className="w-full" autoFocus /></Field>
        <Field label="Apelido (opcional)"><input value={nickname} onChange={(e) => setNickname(e.target.value)} className="w-full" /></Field>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} /> Ativo (aparece na lista de presentes)
        </label>
        <InlineError error={save.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className="btn-primary" disabled={save.isPending}>Salvar</button>
        </div>
      </form>
    </Modal>
  )
}
