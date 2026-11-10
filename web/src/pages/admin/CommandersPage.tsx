import { Check, Pencil, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import { useCommanders, useDeleteCommander, useRenameCommander } from '../../api/hooks'
import type { Commander } from '../../api/types'
import { Empty, ErrorBox, InlineError, PageHeader, Spinner } from '../../components/ui'
import { searchKey } from '../../lib/format'

export function CommandersPage() {
  const commanders = useCommanders()
  const del = useDeleteCommander()
  const [query, setQuery] = useState('')

  if (commanders.isPending) return <Spinner />
  if (commanders.error) return <ErrorBox error={commanders.error} onRetry={() => commanders.refetch()} />

  const q = searchKey(query)
  const list = commanders.data.filter((c) => !q || searchKey(c.name).includes(q))

  async function remove(c: Commander) {
    if (confirm(`Excluir o comandante "${c.name}"?`)) await del.mutateAsync(c.id)
  }

  return (
    <>
      <PageHeader
        title="Comandantes"
        subtitle="Criados automaticamente ao cadastrar decks. Corrija a grafia aqui para manter as estatísticas unificadas."
      />
      <input type="search" placeholder="Buscar" value={query} onChange={(e) => setQuery(e.target.value)} className="mb-3 w-full" />
      <InlineError error={del.error} />
      {list.length === 0 && <Empty>Nenhum comandante.</Empty>}
      <ul className="space-y-1.5">
        {list.map((c) => <CommanderRow key={c.id} commander={c} onDelete={() => remove(c)} />)}
      </ul>
    </>
  )
}

function CommanderRow({ commander, onDelete }: { commander: Commander; onDelete: () => void }) {
  const rename = useRenameCommander()
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(commander.name)

  async function save() {
    await rename.mutateAsync({ id: commander.id, name })
    setEditing(false)
  }

  return (
    <li className="card p-2.5">
      <div className="flex items-center gap-2">
        {editing ? (
          <>
            <input value={name} onChange={(e) => setName(e.target.value)} className="min-w-0 flex-1" autoFocus
              onKeyDown={(e) => { if (e.key === 'Enter') save(); if (e.key === 'Escape') setEditing(false) }} />
            <button className="btn-primary btn-sm" onClick={save} disabled={rename.isPending || !name.trim()} aria-label="Salvar"><Check className="size-4" /></button>
            <button className="btn-ghost btn-sm" onClick={() => { setEditing(false); setName(commander.name) }} aria-label="Cancelar"><X className="size-4" /></button>
          </>
        ) : (
          <>
            <span className="min-w-0 flex-1 truncate">{commander.name}</span>
            <button className="btn-ghost btn-sm" onClick={() => setEditing(true)} aria-label={`Renomear ${commander.name}`}><Pencil className="size-4" /></button>
            <button className="btn-ghost btn-sm text-red-300" onClick={onDelete} aria-label={`Excluir ${commander.name}`}><Trash2 className="size-4" /></button>
          </>
        )}
      </div>
      <InlineError error={rename.error} />
    </li>
  )
}
