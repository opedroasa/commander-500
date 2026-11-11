import { ChevronDown, Crown, Handshake, Medal } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { useCommanderStats, useRanking, useSeason, useSeasonStages, useStage } from '../../api/hooks'
import type { Round, Season, Stage } from '../../api/types'
import { CommanderArtCards, CommanderList } from '../../components/CommanderRankings'
import { commanderSortLabel, type CommanderSort } from '../../lib/commanders'
import { PlayerProfileModal } from '../../components/PlayerProfileModal'
import { Empty, ErrorBox, PageHeader, RoundStatusBadge, Spinner, Tabs } from '../../components/ui'
import { formatDate, formatPercent, tiebreakLabel } from '../../lib/format'

type Tab = 'ranking' | 'etapas' | 'comandantes'
const tabs: Tab[] = ['ranking', 'etapas', 'comandantes']

export function SeasonDetailPage() {
  const id = Number(useParams().id)
  const [params, setParams] = useSearchParams()
  const tab = (tabs.includes(params.get('aba') as Tab) ? params.get('aba') : 'ranking') as Tab
  const season = useSeason(id)
  const [profileId, setProfileId] = useState<number | null>(null)

  if (season.isPending) return <Spinner />
  if (season.error) return <ErrorBox error={season.error} onRetry={() => season.refetch()} />
  const s = season.data

  return (
    <>
      <PageHeader
        title={s.name}
        subtitle={
          <>
            {formatDate(s.startDate)}{s.endDate ? ` – ${formatDate(s.endDate)}` : ' – em aberto'}
            {s.isActive && <span className="ml-2 rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">em andamento</span>}
          </>
        }
      />
      <Tabs
        value={tab}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'ranking', label: 'Ranking' },
          { value: 'etapas', label: 'Etapas' },
          { value: 'comandantes', label: 'Comandantes' },
        ]}
      />
      {tab === 'ranking' && <RankingTab season={s} onPlayer={setProfileId} />}
      {tab === 'etapas' && <StagesTab seasonId={s.id} onPlayer={setProfileId} />}
      {tab === 'comandantes' && <CommandersTab season={s} />}

      <PlayerProfileModal playerId={profileId} seasonId={s.id} onClose={() => setProfileId(null)} />
    </>
  )
}

/** Player name that opens the mini profile. */
function PlayerButton({ id, name, onPlayer }: { id: number; name: string; onPlayer: (id: number) => void }) {
  return (
    <button type="button" onClick={() => onPlayer(id)} className="max-w-full truncate text-left font-medium text-zinc-100 underline-offset-2 hover:text-amber-300 hover:underline">
      {name}
    </button>
  )
}

function RankingTab({ season, onPlayer }: { season: Season; onPlayer: (id: number) => void }) {
  const ranking = useRanking(season.id)
  if (ranking.isPending) return <Spinner />
  if (ranking.error) return <ErrorBox error={ranking.error} onRetry={() => ranking.refetch()} />
  const c = season.config

  return (
    <>
      {ranking.data.length === 0 ? (
        <Empty>Nenhuma rodada encerrada ainda. O ranking aparece depois da primeira rodada.</Empty>
      ) : (
        <div className="card overflow-x-auto">
          <table className="w-full text-[13px] tabular-nums sm:text-sm">
            <thead className="text-xs uppercase tracking-wide text-zinc-400">
              <tr className="border-b border-zinc-800">
                <th className="px-2 py-2.5 text-left sm:px-3">#</th>
                <th className="px-1.5 py-2.5 text-left">Jogador</th>
                <th className="px-1.5 py-2.5 text-right" title="Pontos">Pts</th>
                <th className="px-1.5 py-2.5 text-right" title="Vitórias">V</th>
                <th className="px-1.5 py-2.5 text-right" title="Empates">E</th>
                <th className="px-1.5 py-2.5 text-right" title="Byes">B</th>
                <th className="px-1.5 py-2.5 text-right" title="Partidas jogadas">P</th>
                <th className="px-2 py-2.5 text-right sm:px-3" title="% de vitória">%V</th>
              </tr>
            </thead>
            <tbody>
              {ranking.data.map((r) => (
                <tr key={r.playerId} className="border-b border-zinc-800/60 last:border-0">
                  <td className="px-2 py-2.5 sm:px-3"><PositionBadge position={r.position} /></td>
                  <td className="max-w-[7.5rem] px-1.5 py-2.5 sm:max-w-none">
                    <PlayerButton id={r.playerId} name={r.playerName} onPlayer={onPlayer} />
                  </td>
                  <td className="px-1.5 py-2.5 text-right text-base font-bold text-amber-300">{r.points}</td>
                  <td className="px-1.5 py-2.5 text-right">{r.wins}</td>
                  <td className="px-1.5 py-2.5 text-right">{r.draws}</td>
                  <td className="px-1.5 py-2.5 text-right">{r.byes}</td>
                  <td className="px-1.5 py-2.5 text-right">{r.matches}</td>
                  <td className="px-2 py-2.5 text-right text-zinc-300 sm:px-3">{formatPercent(r.winRate)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <p className="mt-2 text-xs text-zinc-500">Toque no nome do jogador para ver o perfil.</p>
      <details className="mt-3 text-sm text-zinc-400">
        <summary className="cursor-pointer select-none text-zinc-300">Como funciona a pontuação</summary>
        <ul className="mt-2 list-disc space-y-1 pl-5">
          <li>Vitória: <b>{c.pointsWin}</b> pts (só o vencedor da mesa pontua).</li>
          <li>Empate / tempo esgotado: <b>{c.pointsDraw}</b> pt para cada jogador da mesa.</li>
          <li>Bye: <b>{c.pointsBye1}</b> pts (1 de fora), <b>{c.pointsBye2}</b> (2 de fora), <b>{c.pointsBye3}</b> (3 de fora). Bye não conta como partida.</li>
          <li>Desempate: pontos → {c.tiebreakers.map((t) => tiebreakLabel[t].toLowerCase()).join(' → ')}{c.tiebreakers.length ? ' → ' : ''}nome.</li>
          <li>V = vitórias, E = empates, B = byes, P = partidas jogadas, %V = vitórias ÷ partidas.</li>
        </ul>
      </details>
    </>
  )
}

function PositionBadge({ position, tied = false }: { position: number; tied?: boolean }) {
  return (
    <span
      className={`inline-flex size-6 items-center justify-center rounded-full text-xs font-bold ${
        position === 1 ? 'bg-brand-orange text-white'
          : position === 2 ? 'bg-zinc-300 text-zinc-900'
            : position === 3 ? 'bg-amber-800 text-amber-50' : 'text-zinc-400'
      }`}
      title={tied ? 'Empatado' : undefined}
    >
      {position}{tied && <span className="sr-only"> (empatado)</span>}
    </span>
  )
}

// ---------------------------------------------------------------- Etapas

function StagesTab({ seasonId, onPlayer }: { seasonId: number; onPlayer: (id: number) => void }) {
  const stages = useSeasonStages(seasonId)
  if (stages.isPending) return <Spinner />
  if (stages.error) return <ErrorBox error={stages.error} onRetry={() => stages.refetch()} />
  if (stages.data.length === 0) return <Empty>Nenhuma etapa ainda.</Empty>
  return (
    <div className="space-y-2">
      {stages.data.map((st, i) => <StageCard key={st.id} stage={st} defaultOpen={i === 0} onPlayer={onPlayer} />)}
      <p className="pt-2 text-xs text-zinc-500">
        Ranking da etapa: mais vitórias (bye conta como vitória) → quem teve bye fica abaixo → confronto direto entre os empatados → persistindo, dividem a posição.
      </p>
    </div>
  )
}

function StageCard({ stage, defaultOpen, onPlayer }: { stage: Stage; defaultOpen: boolean; onPlayer: (id: number) => void }) {
  const [open, setOpen] = useState(defaultOpen)
  const detail = useStage(stage.id, open)

  return (
    <div className="card">
      <button className="flex w-full flex-wrap items-center gap-x-3 gap-y-1 p-4 text-left" onClick={() => setOpen(!open)} aria-expanded={open}>
        <span className="font-display text-lg font-bold text-zinc-100">Etapa {stage.number}</span>
        <span className="text-sm text-zinc-400">{formatDate(stage.date)} · {stage.roundsCount} rodada{stage.roundsCount === 1 ? '' : 's'}</span>
        <span className="ml-auto flex items-center gap-2">
          {stage.leaders.length > 0 && (
            <span className="inline-flex items-center gap-1 text-sm text-amber-300">
              <Medal className="size-4" aria-hidden />
              {stage.isComplete ? '' : 'Líder: '}{stage.leaders.map((l) => l.name).join(', ')}
            </span>
          )}
          {!stage.isComplete && <span className="rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">em andamento</span>}
          <ChevronDown className={`size-4 text-zinc-500 transition ${open ? 'rotate-180' : ''}`} aria-hidden />
        </span>
      </button>
      {open && (
        <div className="space-y-4 border-t border-zinc-800 p-4">
          {detail.isPending && <Spinner />}
          {detail.error && <ErrorBox error={detail.error} />}
          {detail.data && (
            <>
              {detail.data.ranking.length === 0 ? (
                <p className="text-sm text-zinc-400">Nenhuma rodada encerrada nesta etapa ainda.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-[13px] tabular-nums sm:text-sm">
                    <thead className="text-xs uppercase tracking-wide text-zinc-400">
                      <tr className="border-b border-zinc-800">
                        <th className="px-1.5 py-2 text-left">#</th>
                        <th className="px-1.5 py-2 text-left">Jogador</th>
                        <th className="px-1.5 py-2 text-right" title="Vitórias na etapa (bye conta)">V</th>
                        <th className="px-1.5 py-2 text-right" title="Byes">B</th>
                        <th className="px-1.5 py-2 text-right" title="Empates">E</th>
                        <th className="px-1.5 py-2 text-right" title="Vitórias em confronto direto entre empatados">CD</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.data.ranking.map((r) => (
                        <tr key={r.playerId} className="border-b border-zinc-800/60 last:border-0">
                          <td className="px-1.5 py-2"><PositionBadge position={r.position} tied={r.tied} /></td>
                          <td className="max-w-[9rem] px-1.5 py-2 sm:max-w-none">
                            <span className="flex items-center gap-1.5">
                              <PlayerButton id={r.playerId} name={r.playerName} onPlayer={onPlayer} />
                              {r.tied && r.wins > 0 && <span className="shrink-0 rounded bg-zinc-800 px-1 text-[0.6rem] uppercase text-zinc-400">empate</span>}
                            </span>
                          </td>
                          <td className="px-1.5 py-2 text-right text-base font-bold text-amber-300">{r.wins}</td>
                          <td className="px-1.5 py-2 text-right">{r.byes}</td>
                          <td className="px-1.5 py-2 text-right">{r.draws}</td>
                          <td className="px-1.5 py-2 text-right text-zinc-400">{r.headToHeadWins || '–'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              <div className="space-y-2">
                {detail.data.rounds.filter((r) => r.status !== 'Draft').map((r) => <RoundSummary key={r.id} round={r} onPlayer={onPlayer} />)}
              </div>
            </>
          )}
        </div>
      )}
    </div>
  )
}

function RoundSummary({ round, onPlayer }: { round: Round; onPlayer: (id: number) => void }) {
  const [open, setOpen] = useState(false)
  return (
    <div className="rounded-xl border border-zinc-800">
      <button className="flex w-full items-center gap-3 px-3 py-2.5 text-left" onClick={() => setOpen(!open)} aria-expanded={open}>
        <span className="font-semibold text-zinc-100">Rodada {round.numberInStage}</span>
        <span className="ml-auto"><RoundStatusBadge status={round.status} /></span>
        <ChevronDown className={`size-4 text-zinc-500 transition ${open ? 'rotate-180' : ''}`} aria-hidden />
      </button>
      {open && (
        <div className="space-y-3 border-t border-zinc-800 p-3">
          {round.tables.map((t) => (
            <div key={t.id}>
              <div className="mb-1 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-zinc-400">
                Mesa {t.number}
                {t.result === 'Draw' && <span className="inline-flex items-center gap-1 normal-case text-zinc-300"><Handshake className="size-3.5" aria-hidden />empate</span>}
                {t.result === 'Pending' && <span className="normal-case text-zinc-500">sem resultado</span>}
              </div>
              <ul className="grid gap-1 sm:grid-cols-2">
                {t.seats.map((s) => {
                  const won = t.result === 'Win' && t.winnerEntryId === s.entry.id
                  return (
                    <li key={s.seat} className={`flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm ${won ? 'bg-amber-500/15' : 'bg-zinc-900'}`}>
                      <span className="w-4 text-xs text-zinc-500">{s.seat}</span>
                      <span className="min-w-0 flex-1 truncate">
                        <button type="button" onClick={() => onPlayer(s.entry.playerId)} className="font-medium hover:text-amber-300 hover:underline">{s.entry.playerName}</button>
                        <span className="text-zinc-500"> · {s.entry.commanders}</span>
                      </span>
                      {won && <Crown className="size-4 shrink-0 text-amber-400" aria-label="Vencedor" />}
                    </li>
                  )
                })}
              </ul>
            </div>
          ))}
          {round.byes.length > 0 && <p className="text-sm text-zinc-400">Bye: {round.byes.map((b) => b.playerName).join(', ')}</p>}
          <Link to={`/rodadas/${round.id}`} className="inline-block text-sm font-medium text-amber-300 hover:underline">Ver mesas desenhadas →</Link>
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------- Comandantes

function CommandersTab({ season }: { season: Season }) {
  const stats = useCommanderStats(season.id)
  const [sort, setSort] = useState<CommanderSort>('played')
  if (stats.isPending) return <Spinner />
  if (stats.error) return <ErrorBox error={stats.error} onRetry={() => stats.refetch()} />
  const min = season.config.minMatchesForCommanderWinRate

  if (!stats.data.some((r) => r.matches > 0)) return <Empty>Nenhuma partida registrada nesta temporada ainda.</Empty>

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap gap-1.5">
        {(['played', 'wins', 'rate'] as const).map((v) => (
          <button key={v} onClick={() => setSort(v)} className={`btn btn-sm ${sort === v ? 'bg-brand-orange text-white' : 'border border-zinc-700 text-zinc-300 hover:bg-zinc-800'}`}>
            {commanderSortLabel[v]}
          </button>
        ))}
      </div>
      {sort !== 'rate' && <CommanderArtCards rows={stats.data} sort={sort} />}
      <div className="card px-4 py-1"><CommanderList rows={stats.data} sort={sort} minMatches={min} /></div>
      <p className="text-xs text-zinc-500">
        Duplas de parceiros/background aparecem como uma linha. Bye não conta como partida nem vitória. % de vitória a partir de {min} partidas.
      </p>
    </div>
  )
}
