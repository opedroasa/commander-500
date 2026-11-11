import { useSearchParams } from 'react-router'
import { useCommanderStats, useSeasons } from '../../api/hooks'
import { CommanderArtCards, CommanderList } from '../../components/CommanderRankings'
import { commanderSortLabel, type CommanderSort } from '../../lib/commanders'
import { ErrorBox, PageHeader, Spinner } from '../../components/ui'

const sorts = Object.keys(commanderSortLabel) as CommanderSort[]

/** Public commander ranking: every season or one, sortable. Partner/background pairs are one row. */
export function CommandersPage() {
  const [params, setParams] = useSearchParams()
  const seasonId = Number(params.get('temporada')) || null
  const sort = (sorts.includes(params.get('ordem') as CommanderSort) ? params.get('ordem') : 'played') as CommanderSort
  const seasons = useSeasons()
  const stats = useCommanderStats(seasonId)
  const minMatches = seasonId ? seasons.data?.find((s) => s.id === seasonId)?.config.minMatchesForCommanderWinRate ?? 3 : 3

  function set(key: string, value: string | null) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  return (
    <>
      <PageHeader title="Comandantes" subtitle="O que está sendo jogado e vencendo na liga da Fantasy Store." />

      <div className="mb-5 flex flex-wrap items-center gap-2">
        <select value={seasonId ?? ''} onChange={(e) => set('temporada', e.target.value || null)} aria-label="Temporada" className="min-w-0">
          <option value="">Todas as temporadas</option>
          {seasons.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
        <div className="flex flex-wrap gap-1.5">
          {sorts.map((s) => (
            <button key={s} onClick={() => set('ordem', s === 'played' ? null : s)}
              className={`btn btn-sm ${sort === s ? 'bg-brand-orange text-white' : 'border border-zinc-700 text-zinc-300 hover:bg-zinc-800'}`}>
              {commanderSortLabel[s]}
            </button>
          ))}
        </div>
      </div>

      {stats.isPending && <Spinner />}
      {stats.error && <ErrorBox error={stats.error} onRetry={() => stats.refetch()} />}
      {stats.data && (
        <div className="space-y-6">
          {sort !== 'rate' && <CommanderArtCards rows={stats.data} sort={sort} />}
          <div className="card px-4 py-1">
            <CommanderList rows={stats.data} sort={sort} minMatches={minMatches} />
          </div>
          <p className="text-xs text-zinc-500">
            Duplas de parceiros/background contam como uma linha. Bye não conta como partida nem vitória.
            % de vitória a partir de {minMatches} partidas. "Mais decks" conta os decks cadastrados na loja.
          </p>
        </div>
      )}
    </>
  )
}
