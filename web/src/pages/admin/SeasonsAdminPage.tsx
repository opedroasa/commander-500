import { ArrowDown, ArrowUp, Flag, Lock, Pencil, Plus, Settings, Trash2, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useDeleteSeason, useEndSeason, useSaveSeason, useSaveSeasonConfig, useSeasons } from '../../api/hooks'
import type { Season, SeasonConfig, TiebreakCriterion } from '../../api/types'
import { Empty, ErrorBox, Field, InlineError, Modal, PageHeader, Spinner } from '../../components/ui'
import { formatDate, tiebreakLabel, todayIso } from '../../lib/format'

export function SeasonsAdminPage() {
  const seasons = useSeasons()
  const end = useEndSeason()
  const del = useDeleteSeason()
  const [editing, setEditing] = useState<Season | 'new' | null>(null)
  const [configuring, setConfiguring] = useState<Season | null>(null)

  if (seasons.isPending) return <Spinner />
  if (seasons.error) return <ErrorBox error={seasons.error} onRetry={() => seasons.refetch()} />

  async function endSeason(s: Season) {
    if (confirm(`Encerrar "${s.name}"? Ela deixa de estar ativa e não recebe novas rodadas.`)) await end.mutateAsync(s.id)
  }
  async function remove(s: Season) {
    if (confirm(`Excluir "${s.name}"?`)) await del.mutateAsync(s.id)
  }

  return (
    <>
      <PageHeader
        title="Temporadas"
        actions={<button className="btn-primary" onClick={() => setEditing('new')}><Plus className="size-4" aria-hidden />Nova temporada</button>}
      />
      <InlineError error={end.error ?? del.error} />
      {seasons.data.length === 0 && <Empty>Nenhuma temporada.</Empty>}
      <ul className="space-y-2">
        {seasons.data.map((s) => (
          <li key={s.id} className="card p-4">
            <div className="flex flex-wrap items-start gap-3">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-display text-lg font-bold">{s.name}</span>
                  {s.isActive
                    ? <span className="rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">ativa</span>
                    : <span className="rounded-full bg-zinc-800 px-2 py-0.5 text-xs text-zinc-400">encerrada</span>}
                  {s.configLocked && <span className="inline-flex items-center gap-1 text-xs text-zinc-400"><Lock className="size-3" aria-hidden />pontuação travada</span>}
                </div>
                <div className="text-sm text-zinc-400">
                  {formatDate(s.startDate)}{s.endDate ? ` – ${formatDate(s.endDate)}` : ''} · {s.roundsCount} rodada{s.roundsCount === 1 ? '' : 's'}
                </div>
                <div className="mt-1 text-xs text-zinc-500">
                  V {s.config.pointsWin} · E {s.config.pointsDraw} · Bye {s.config.pointsBye1}/{s.config.pointsBye2}/{s.config.pointsBye3} · {s.config.defaultRoundMinutes} min
                </div>
              </div>
              <div className="flex flex-wrap gap-1">
                <button className="btn-secondary btn-sm" onClick={() => setConfiguring(s)}><Settings className="size-4" aria-hidden />Pontuação</button>
                <button className="btn-ghost btn-sm" onClick={() => setEditing(s)} aria-label="Editar"><Pencil className="size-4" /></button>
                {s.isActive && <button className="btn-ghost btn-sm" onClick={() => endSeason(s)}><Flag className="size-4" aria-hidden />Encerrar</button>}
                {s.roundsCount === 0 && <button className="btn-ghost btn-sm text-red-300" onClick={() => remove(s)} aria-label="Excluir"><Trash2 className="size-4" /></button>}
              </div>
            </div>
          </li>
        ))}
      </ul>
      <SeasonModal key={`season-${editing === 'new' ? 'new' : editing?.id ?? 'none'}`} season={editing} onClose={() => setEditing(null)} />
      <ConfigModal key={`config-${configuring?.id ?? 'none'}`} season={configuring} onClose={() => setConfiguring(null)} />
    </>
  )
}

function SeasonModal({ season, onClose }: { season: Season | 'new' | null; onClose: () => void }) {
  const save = useSaveSeason()
  const existing = season && season !== 'new' ? season : null
  const [name, setName] = useState(existing?.name ?? '')
  const [startDate, setStartDate] = useState(existing?.startDate ?? todayIso())
  const [endDate, setEndDate] = useState(existing?.endDate ?? '')
  const [isActive, setIsActive] = useState(existing?.isActive ?? true)

  async function submit(e: FormEvent) {
    e.preventDefault()
    await save.mutateAsync({ id: existing?.id, data: { name, startDate, endDate: endDate || null, isActive } })
    onClose()
  }

  return (
    <Modal open={season !== null} onClose={onClose} title={existing ? 'Editar temporada' : 'Nova temporada'}>
      <form onSubmit={submit} className="space-y-4">
        <Field label="Nome"><input required value={name} onChange={(e) => setName(e.target.value)} className="w-full" autoFocus /></Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Início"><input type="date" required value={startDate} onChange={(e) => setStartDate(e.target.value)} className="w-full" /></Field>
          <Field label="Fim (opcional)"><input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} className="w-full" /></Field>
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} /> Ativa (aceita novas rodadas)
        </label>
        {!existing && <p className="text-xs text-zinc-500">A pontuação começa com os valores padrão; ajuste em “Pontuação”.</p>}
        <InlineError error={save.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className="btn-primary" disabled={save.isPending}>Salvar</button>
        </div>
      </form>
    </Modal>
  )
}

const allCriteria = Object.keys(tiebreakLabel) as TiebreakCriterion[]

function ConfigModal({ season, onClose }: { season: Season | null; onClose: () => void }) {
  const save = useSaveSeasonConfig()
  const [cfg, setCfg] = useState<SeasonConfig | null>(season?.config ?? null)
  if (!season || !cfg) return <Modal open={false} onClose={onClose} title="">{null}</Modal>
  const locked = season.configLocked

  const num = (key: keyof SeasonConfig, label: string, hint?: string, min = 0, max = 100) => (
    <Field label={label} hint={hint}>
      <input type="number" min={min} max={max} required disabled={locked} value={cfg[key] as number}
        onChange={(e) => setCfg({ ...cfg, [key]: Number(e.target.value) })} className="w-full" />
    </Field>
  )

  function move(i: number, delta: number) {
    const list = [...cfg!.tiebreakers]
    const j = i + delta
    if (j < 0 || j >= list.length) return
    ;[list[i], list[j]] = [list[j], list[i]]
    setCfg({ ...cfg!, tiebreakers: list })
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    await save.mutateAsync({ id: season!.id, config: cfg! })
    onClose()
  }

  const available = allCriteria.filter((c) => !cfg.tiebreakers.includes(c))

  return (
    <Modal open onClose={onClose} title={`Pontuação · ${season.name}`}>
      <form onSubmit={submit} className="space-y-4">
        {locked && (
          <p className="flex items-start gap-2 rounded-lg border border-zinc-700 bg-zinc-800/60 p-2 text-sm text-zinc-300">
            <Lock className="mt-0.5 size-4 shrink-0" aria-hidden /> Travada: a primeira rodada já foi encerrada.
          </p>
        )}
        <div className="grid grid-cols-2 gap-3">
          {num('pointsWin', 'Vitória')}
          {num('pointsDraw', 'Empate (cada)')}
          {num('pointsBye1', 'Bye · 1 de fora')}
          {num('pointsBye2', 'Bye · 2 de fora')}
          {num('pointsBye3', 'Bye · 3 de fora')}
          {num('defaultRoundMinutes', 'Timer padrão (min)', undefined, 1, 600)}
          {num('extraTurnsAfterTime', 'Turnos após o tempo', 'Só exibido no timer.', 0, 20)}
          {num('minMatchesForCommanderWinRate', 'Mín. partidas p/ % cmd.', undefined, 1, 100)}
        </div>

        <div>
          <span className="label">Desempate (depois dos pontos)</span>
          <ol className="space-y-1">
            {cfg.tiebreakers.map((t, i) => (
              <li key={t} className="flex items-center gap-2 rounded-lg border border-zinc-800 bg-zinc-900 px-2 py-1.5 text-sm">
                <span className="w-4 text-zinc-500">{i + 1}.</span>
                <span className="flex-1">{tiebreakLabel[t]}</span>
                {!locked && (
                  <>
                    <button type="button" className="btn-ghost btn-sm px-1.5" onClick={() => move(i, -1)} aria-label="Subir"><ArrowUp className="size-3.5" /></button>
                    <button type="button" className="btn-ghost btn-sm px-1.5" onClick={() => move(i, 1)} aria-label="Descer"><ArrowDown className="size-3.5" /></button>
                    <button type="button" className="btn-ghost btn-sm px-1.5" onClick={() => setCfg({ ...cfg, tiebreakers: cfg.tiebreakers.filter((x) => x !== t) })} aria-label="Remover"><X className="size-3.5" /></button>
                  </>
                )}
              </li>
            ))}
            <li className="px-2 text-xs text-zinc-500">{cfg.tiebreakers.length + 1}. Nome (sempre por último)</li>
          </ol>
          {!locked && available.length > 0 && (
            <select className="mt-2 w-full" value="" onChange={(e) => e.target.value && setCfg({ ...cfg, tiebreakers: [...cfg.tiebreakers, e.target.value as TiebreakCriterion] })}>
              <option value="">+ Adicionar critério…</option>
              {available.map((c) => <option key={c} value={c}>{tiebreakLabel[c]}</option>)}
            </select>
          )}
        </div>

        <InlineError error={save.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="btn-secondary" onClick={onClose}>{locked ? 'Fechar' : 'Cancelar'}</button>
          {!locked && <button className="btn-primary" disabled={save.isPending}>Salvar</button>}
        </div>
      </form>
    </Modal>
  )
}
