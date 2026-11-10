import { CalendarDays, ChevronRight, Radio } from 'lucide-react'
import { Link } from 'react-router'
import { useCurrentRounds, useSeasons } from '../../api/hooks'
import type { Season } from '../../api/types'
import { Empty, ErrorBox, PageHeader, Spinner } from '../../components/ui'
import { formatDate } from '../../lib/format'

export function SeasonsPage() {
  const seasons = useSeasons()
  const live = useCurrentRounds()

  const active = seasons.data?.filter((s) => s.isActive) ?? []
  const past = seasons.data?.filter((s) => !s.isActive) ?? []

  return (
    <>
      <PageHeader title="Temporadas" subtitle="Ranking, comandantes e histórico da liga Commander 500." />

      {live.data && live.data.length > 0 && (
        <Link
          to="/ao-vivo"
          className="mb-6 flex items-center gap-3 rounded-2xl border border-emerald-800 bg-emerald-950/40 p-4 transition hover:bg-emerald-950/70"
        >
          <span className="relative flex size-3">
            <span className="absolute inline-flex size-full animate-ping rounded-full bg-emerald-400 opacity-60" />
            <span className="relative inline-flex size-3 rounded-full bg-emerald-400" />
          </span>
          <span className="flex-1">
            <span className="block font-semibold text-emerald-100">Rodada acontecendo agora</span>
            <span className="block text-sm text-emerald-300/80">Veja as mesas sorteadas e encontre seu lugar.</span>
          </span>
          <Radio className="size-5 text-emerald-300" aria-hidden />
        </Link>
      )}

      {seasons.isPending && <Spinner />}
      {seasons.error && <ErrorBox error={seasons.error} onRetry={() => seasons.refetch()} />}

      {seasons.data && seasons.data.length === 0 && <Empty>Nenhuma temporada cadastrada ainda.</Empty>}

      {active.length > 0 && (
        <section className="mb-8">
          <h2 className="mb-3 text-xs font-semibold uppercase tracking-widest text-zinc-500">Em andamento</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            {active.map((s) => <SeasonCard key={s.id} season={s} highlight />)}
          </div>
        </section>
      )}

      {past.length > 0 && (
        <section>
          <h2 className="mb-3 text-xs font-semibold uppercase tracking-widest text-zinc-500">Temporadas passadas</h2>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {past.map((s) => <SeasonCard key={s.id} season={s} />)}
          </div>
        </section>
      )}
    </>
  )
}

function SeasonCard({ season, highlight = false }: { season: Season; highlight?: boolean }) {
  return (
    <Link
      to={`/temporadas/${season.id}`}
      className={`group flex items-center gap-4 rounded-2xl border p-4 transition ${
        highlight
          ? 'border-amber-700/60 bg-gradient-to-br from-amber-950/50 to-zinc-900 hover:border-amber-500'
          : 'border-zinc-800 bg-zinc-900/60 hover:border-zinc-600'
      }`}
    >
      <div className="min-w-0 flex-1">
        <div className={`truncate font-display text-lg font-bold ${highlight ? 'text-amber-100' : 'text-zinc-100'}`}>{season.name}</div>
        <div className="mt-1 flex items-center gap-1.5 text-sm text-zinc-400">
          <CalendarDays className="size-4" aria-hidden />
          {formatDate(season.startDate)}
          {season.endDate ? ` – ${formatDate(season.endDate)}` : ' – em aberto'}
        </div>
        <div className="mt-1 text-xs text-zinc-500">{season.roundsCount} rodada{season.roundsCount === 1 ? '' : 's'}</div>
      </div>
      <ChevronRight className="size-5 text-zinc-500 transition group-hover:translate-x-0.5 group-hover:text-zinc-300" aria-hidden />
    </Link>
  )
}
