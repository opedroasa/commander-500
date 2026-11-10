import { ChevronDown, Crown, Handshake } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { useCommanderStats, useRanking, useSeason, useSeasonRounds } from '../../api/hooks'
import type { CommanderStatsRow, Round, Season } from '../../api/types'
import { Empty, ErrorBox, PageHeader, RoundStatusBadge, Spinner, Tabs } from '../../components/ui'
import { formatDate, formatPercent, tiebreakLabel } from '../../lib/format'

type Tab = 'ranking' | 'comandantes' | 'rodadas'

export function SeasonDetailPage() {
  const id = Number(useParams().id)
  const [params, setParams] = useSearchParams()
  const tab = (['ranking', 'comandantes', 'rodadas'].includes(params.get('aba') ?? '') ? params.get('aba') : 'ranking') as Tab
  const season = useSeason(id)

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
            {s.isActive && <span className="ml-2 rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">ativa</span>}
          </>
        }
      />
      <Tabs
        value={tab}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'ranking', label: 'Ranking' },
          { value: 'comandantes', label: 'Comandantes' },
          { value: 'rodadas', label: 'Rodadas' },
        ]}
      />
      {tab === 'ranking' && <RankingTab season={s} />}
      {tab === 'comandantes' && <CommandersTab season={s} />}
      {tab === 'rodadas' && <RoundsTab seasonId={s.id} />}
    </>
  )
}

function RankingTab({ season }: { season: Season }) {
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
                  <td className="px-2 py-2.5 sm:px-3">
                    <span className={`inline-flex size-6 items-center justify-center rounded-full text-xs font-bold ${
                      r.position === 1 ? 'bg-amber-500 text-zinc-950'
                        : r.position === 2 ? 'bg-zinc-300 text-zinc-900'
                          : r.position === 3 ? 'bg-amber-800 text-amber-50' : 'text-zinc-400'
                    }`}>{r.position}</span>
                  </td>
                  <td className="max-w-[7.5rem] truncate px-1.5 py-2.5 font-medium text-zinc-100 sm:max-w-none">{r.playerName}</td>
                  <td className="px-1.5 py-2.5 text-right text-base font-bold text-amber-200">{r.points}</td>
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
      <details className="mt-4 text-sm text-zinc-400">
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

type CmdSort = 'played' | 'wins' | 'rate'

function CommandersTab({ season }: { season: Season }) {
  const stats = useCommanderStats(season.id)
  const [sort, setSort] = useState<CmdSort>('played')
  if (stats.isPending) return <Spinner />
  if (stats.error) return <ErrorBox error={stats.error} onRetry={() => stats.refetch()} />

  const min = season.config.minMatchesForCommanderWinRate
  const sorted = [...stats.data].sort(sorters[sort])
  const rows = sort === 'rate' ? sorted.filter((r) => r.winRate !== null) : sorted

  return (
    <>
      <div className="mb-3 flex flex-wrap gap-2">
        {([['played', 'Mais jogados'], ['wins', 'Mais vitórias'], ['rate', '% de vitória']] as const).map(([v, label]) => (
          <button key={v} onClick={() => setSort(v)} className={`btn-sm btn ${sort === v ? 'bg-zinc-100 text-zinc-900' : 'border border-zinc-700 text-zinc-300 hover:bg-zinc-800'}`}>
            {label}
          </button>
        ))}
      </div>
      {rows.length === 0 ? (
        <Empty>{sort === 'rate' ? `Nenhum comandante com ${min}+ partidas ainda.` : 'Nenhuma partida registrada ainda.'}</Empty>
      ) : (
        <ol className="space-y-2">
          {rows.map((r, i) => (
            <li key={r.commanderIds.join('-')} className="card flex items-center gap-3 p-3">
              <span className="w-6 shrink-0 text-center text-sm font-bold text-zinc-500">{i + 1}</span>
              <div className="min-w-0 flex-1">
                <div className="font-medium text-zinc-100">{r.name}</div>
                <div className="text-xs text-zinc-400">
                  {r.matches} partida{r.matches === 1 ? '' : 's'} · {r.wins} vitória{r.wins === 1 ? '' : 's'} · {r.draws} empate{r.draws === 1 ? '' : 's'}
                </div>
              </div>
              <div className="text-right">
                <div className="text-lg font-bold tabular-nums text-amber-200">
                  {sort === 'played' ? r.matches : sort === 'wins' ? r.wins : formatPercent(r.winRate)}
                </div>
                {sort !== 'rate' && <div className="text-[0.7rem] text-zinc-500">{r.winRate === null ? `% após ${min} partidas` : `${formatPercent(r.winRate)} vit.`}</div>}
              </div>
            </li>
          ))}
        </ol>
      )}
      <p className="mt-3 text-xs text-zinc-500">
        Duplas de parceiros/background aparecem como uma linha. Bye não conta como partida nem vitória. % de vitória a partir de {min} partidas.
      </p>
    </>
  )
}

const sorters: Record<CmdSort, (a: CommanderStatsRow, b: CommanderStatsRow) => number> = {
  played: (a, b) => b.matches - a.matches || b.wins - a.wins || a.name.localeCompare(b.name),
  wins: (a, b) => b.wins - a.wins || a.matches - b.matches || a.name.localeCompare(b.name),
  rate: (a, b) => (b.winRate ?? -1) - (a.winRate ?? -1) || b.matches - a.matches || a.name.localeCompare(b.name),
}

function RoundsTab({ seasonId }: { seasonId: number }) {
  const rounds = useSeasonRounds(seasonId)
  if (rounds.isPending) return <Spinner />
  if (rounds.error) return <ErrorBox error={rounds.error} onRetry={() => rounds.refetch()} />
  const visible = rounds.data.filter((r) => r.status !== 'Draft')
  if (visible.length === 0) return <Empty>Nenhuma rodada sorteada ainda.</Empty>
  return (
    <div className="space-y-2">
      {visible.map((r) => <RoundSummary key={r.id} round={r} />)}
    </div>
  )
}

function RoundSummary({ round }: { round: Round }) {
  const [open, setOpen] = useState(false)
  return (
    <div className="card">
      <button className="flex w-full items-center gap-3 p-4 text-left" onClick={() => setOpen(!open)} aria-expanded={open}>
        <span className="font-display text-lg font-bold text-zinc-100">Rodada {round.number}</span>
        <span className="text-sm text-zinc-400">{formatDate(round.date)}</span>
        <span className="ml-auto"><RoundStatusBadge status={round.status} /></span>
        <ChevronDown className={`size-4 text-zinc-500 transition ${open ? 'rotate-180' : ''}`} aria-hidden />
      </button>
      {open && (
        <div className="space-y-3 border-t border-zinc-800 p-4">
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
                    <li key={s.seat} className={`flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm ${won ? 'bg-amber-500/15 text-amber-100' : 'bg-zinc-900 text-zinc-300'}`}>
                      <span className="w-4 text-xs text-zinc-500">{s.seat}</span>
                      <span className="min-w-0 flex-1 truncate">
                        <span className="font-medium">{s.entry.playerName}</span>
                        <span className="text-zinc-500"> · {s.entry.commanders}</span>
                      </span>
                      {won && <Crown className="size-4 shrink-0 text-amber-400" aria-label="Vencedor" />}
                    </li>
                  )
                })}
              </ul>
            </div>
          ))}
          {round.byes.length > 0 && (
            <p className="text-sm text-zinc-400">Bye: {round.byes.map((b) => b.playerName).join(', ')}</p>
          )}
          <Link to={`/rodadas/${round.id}`} className="inline-block text-sm font-medium text-amber-300 hover:underline">Ver mesas desenhadas →</Link>
        </div>
      )}
    </div>
  )
}
