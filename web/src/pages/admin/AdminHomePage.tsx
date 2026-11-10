import { ChevronRight, Plus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useCreateRound, useSeasonRounds, useSeasons } from '../../api/hooks'
import type { Season } from '../../api/types'
import { Empty, ErrorBox, Field, InlineError, Modal, PageHeader, RoundStatusBadge, Spinner } from '../../components/ui'
import { formatDate, todayIso } from '../../lib/format'

/** Admin landing: rounds of each active season + "new round". */
export function AdminHomePage() {
  const seasons = useSeasons()
  if (seasons.isPending) return <Spinner />
  if (seasons.error) return <ErrorBox error={seasons.error} onRetry={() => seasons.refetch()} />
  const active = seasons.data.filter((s) => s.isActive)

  return (
    <>
      <PageHeader title="Rodadas" subtitle="Crie e conduza as rodadas das temporadas ativas." />
      {active.length === 0 && (
        <Empty>
          Nenhuma temporada ativa. <Link className="text-amber-300 hover:underline" to="/admin/temporadas">Crie ou ative uma temporada</Link>.
        </Empty>
      )}
      <div className="space-y-8">
        {active.map((s) => <SeasonRounds key={s.id} season={s} />)}
      </div>
    </>
  )
}

function SeasonRounds({ season }: { season: Season }) {
  const rounds = useSeasonRounds(season.id)
  const [creating, setCreating] = useState(false)

  return (
    <section>
      <div className="mb-3 flex items-center gap-3">
        <h2 className="font-display text-lg font-bold text-zinc-100">{season.name}</h2>
        <button className="btn-primary btn-sm ml-auto" onClick={() => setCreating(true)}>
          <Plus className="size-4" aria-hidden /> Nova rodada
        </button>
      </div>
      {rounds.isPending && <Spinner />}
      {rounds.error && <ErrorBox error={rounds.error} />}
      {rounds.data?.length === 0 && <Empty>Nenhuma rodada nesta temporada ainda.</Empty>}
      <ul className="space-y-2">
        {rounds.data?.map((r) => (
          <li key={r.id}>
            <Link to={`/admin/rodadas/${r.id}`} className="card flex items-center gap-3 p-3 transition hover:border-zinc-600">
              <span className="font-semibold text-zinc-100">Rodada {r.number}</span>
              <span className="text-sm text-zinc-400">{formatDate(r.date)}</span>
              <span className="hidden text-xs text-zinc-500 sm:inline">{r.entries.length} presentes</span>
              <span className="ml-auto"><RoundStatusBadge status={r.status} /></span>
              <ChevronRight className="size-4 text-zinc-500" aria-hidden />
            </Link>
          </li>
        ))}
      </ul>
      <NewRoundModal season={season} open={creating} onClose={() => setCreating(false)} />
    </section>
  )
}

function NewRoundModal({ season, open, onClose }: { season: Season; open: boolean; onClose: () => void }) {
  const create = useCreateRound()
  const navigate = useNavigate()
  const [date, setDate] = useState(todayIso)
  const [minutes, setMinutes] = useState(season.config.defaultRoundMinutes)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const round = await create.mutateAsync({ seasonId: season.id, date, durationMinutes: minutes })
    onClose()
    navigate(`/admin/rodadas/${round.id}`)
  }

  return (
    <Modal open={open} onClose={onClose} title={`Nova rodada · ${season.name}`}>
      <form onSubmit={submit} className="space-y-4">
        <Field label="Data">
          <input type="date" required value={date} onChange={(e) => setDate(e.target.value)} className="w-full" />
        </Field>
        <Field label="Duração (min)" hint="Pode ser ajustada de novo ao iniciar o timer.">
          <input type="number" min={1} max={600} required value={minutes} onChange={(e) => setMinutes(Number(e.target.value))} className="w-full" />
        </Field>
        <InlineError error={create.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className="btn-primary" disabled={create.isPending}>Criar e marcar presentes</button>
        </div>
      </form>
    </Modal>
  )
}
