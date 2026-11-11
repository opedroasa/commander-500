import { ChevronRight, Medal, Plus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useCreateRound, useSeasonRounds, useSeasons } from '../../api/hooks'
import type { Round, Season } from '../../api/types'
import { Empty, ErrorBox, Field, InlineError, Modal, PageHeader, RoundStatusBadge, Spinner } from '../../components/ui'
import { formatDate, todayIso } from '../../lib/format'

/** Admin landing: stages (etapas) of each active season with their rounds, and "new round". */
export function AdminHomePage() {
  const seasons = useSeasons()
  if (seasons.isPending) return <Spinner />
  if (seasons.error) return <ErrorBox error={seasons.error} onRetry={() => seasons.refetch()} />
  const active = seasons.data.filter((s) => s.isActive)

  return (
    <>
      <PageHeader title="Etapas e rodadas" subtitle="Cada dia de liga é uma etapa, com uma ou mais rodadas." />
      {active.length === 0 && (
        <Empty>
          Nenhuma temporada ativa. <Link className="text-amber-300 hover:underline" to="/admin/temporadas">Crie ou ative uma temporada</Link>.
        </Empty>
      )}
      <div className="space-y-8">
        {active.map((s) => <SeasonStages key={s.id} season={s} />)}
      </div>
    </>
  )
}

interface StageGroup {
  stageId: number
  number: number
  date: string
  rounds: Round[]
}

function SeasonStages({ season }: { season: Season }) {
  const rounds = useSeasonRounds(season.id)
  const [newRoundDate, setNewRoundDate] = useState<string | null>(null)

  const groups: StageGroup[] = []
  for (const r of rounds.data ?? []) {
    let g = groups.find((x) => x.stageId === r.stageId)
    if (!g) groups.push((g = { stageId: r.stageId, number: r.stageNumber, date: r.date, rounds: [] }))
    g.rounds.push(r)
  }
  groups.sort((a, b) => b.number - a.number)
  groups.forEach((g) => g.rounds.sort((a, b) => a.numberInStage - b.numberInStage))
  const today = todayIso()
  const hasToday = groups.some((g) => g.date === today)

  return (
    <section>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <h2 className="font-display text-lg font-bold text-zinc-100">{season.name}</h2>
        <button className="btn-primary btn-sm ml-auto" onClick={() => setNewRoundDate(today)}>
          <Plus className="size-4" aria-hidden /> {hasToday ? 'Nova rodada hoje' : 'Nova etapa (hoje)'}
        </button>
        <button className="btn-secondary btn-sm" onClick={() => setNewRoundDate('')}>Outra data…</button>
      </div>
      {rounds.isPending && <Spinner />}
      {rounds.error && <ErrorBox error={rounds.error} />}
      {rounds.data?.length === 0 && <Empty>Nenhuma etapa nesta temporada ainda.</Empty>}

      <div className="space-y-3">
        {groups.map((g) => (
          <div key={g.stageId} className="card p-3">
            <div className="mb-2 flex items-center gap-2">
              <Medal className="size-4 text-amber-400" aria-hidden />
              <span className="font-semibold text-zinc-100">Etapa {g.number}</span>
              <span className="text-sm text-zinc-400">{formatDate(g.date)}</span>
              <button className="btn-ghost btn-sm ml-auto" onClick={() => setNewRoundDate(g.date)}>
                <Plus className="size-4" aria-hidden /> Rodada nesta etapa
              </button>
            </div>
            <ul className="space-y-1.5">
              {g.rounds.map((r) => (
                <li key={r.id}>
                  <Link to={`/admin/rodadas/${r.id}`} className="flex items-center gap-3 rounded-xl border border-zinc-800 bg-zinc-900 p-3 transition hover:border-zinc-600">
                    <span className="font-medium text-zinc-100">Rodada {r.numberInStage}</span>
                    <span className="text-xs text-zinc-500">{r.entries.length} presentes</span>
                    <span className="ml-auto"><RoundStatusBadge status={r.status} /></span>
                    <ChevronRight className="size-4 text-zinc-500" aria-hidden />
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      {newRoundDate !== null && (
        <NewRoundModal key={newRoundDate} season={season} initialDate={newRoundDate} onClose={() => setNewRoundDate(null)} />
      )}
    </section>
  )
}

function NewRoundModal({ season, initialDate, onClose }: { season: Season; initialDate: string; onClose: () => void }) {
  const create = useCreateRound()
  const navigate = useNavigate()
  const [date, setDate] = useState(initialDate || todayIso())
  const [minutes, setMinutes] = useState(season.config.defaultRoundMinutes)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const round = await create.mutateAsync({ seasonId: season.id, date, durationMinutes: minutes })
    onClose()
    navigate(`/admin/rodadas/${round.id}`)
  }

  return (
    <Modal open onClose={onClose} title={`Nova rodada · ${season.name}`}>
      <form onSubmit={submit} className="space-y-4">
        <Field label="Data da etapa" hint="Mesma data = mesma etapa, já com os presentes da rodada anterior. Data nova cria outra etapa.">
          <input type="date" required value={date} min={season.startDate} max={season.endDate ?? undefined}
            onChange={(e) => setDate(e.target.value)} className="w-full" />
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
