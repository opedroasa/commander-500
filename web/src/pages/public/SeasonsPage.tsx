import { CalendarDays, ChevronRight, Crown, Radio, Trophy } from 'lucide-react'
import { Link } from 'react-router'
import { useCommanderStats, useCurrentRounds, useRanking, useSeasons } from '../../api/hooks'
import type { CommanderStatsRow, Season } from '../../api/types'
import { CommanderArt } from '../../components/CommanderArt'
import { CommanderArtCards, CommanderList } from '../../components/CommanderRankings'
import { StoreLogo } from '../../components/Layout'
import { Empty, ErrorBox, Spinner } from '../../components/ui'
import { formatDate } from '../../lib/format'

/** Commander with the most wins (tie: better win rate, then fewer matches). */
function featured(rows: CommanderStatsRow[] | undefined): CommanderStatsRow | undefined {
  return [...(rows ?? [])]
    .filter((r) => r.wins > 0)
    .sort((a, b) => b.wins - a.wins || (b.winRate ?? 0) - (a.winRate ?? 0) || a.matches - b.matches)[0]
}

export function SeasonsPage() {
  const seasons = useSeasons()
  const live = useCurrentRounds()
  const commanders = useCommanderStats(null)

  const active = seasons.data?.filter((s) => s.isActive) ?? []
  const past = seasons.data?.filter((s) => !s.isActive) ?? []
  const hasCommanderData = (commanders.data ?? []).some((r) => r.matches > 0 || r.decks > 0)

  return (
    <div className="space-y-10">
      {live.data && live.data.length > 0 && (
        <Link
          to="/ao-vivo"
          className="flex items-center gap-3 rounded-2xl border border-emerald-800 bg-emerald-950/40 p-4 transition hover:bg-emerald-950/70"
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

      {seasons.data && seasons.data.length === 0 && (
        <div className="flex flex-col items-center gap-4 py-10 text-center">
          <StoreLogo className="size-24" />
          <Empty>Nenhuma temporada cadastrada ainda.</Empty>
        </div>
      )}

      {active.length > 0 && (
        <section className="space-y-4" aria-label="Temporadas em andamento">
          {active.map((s) => <SeasonHero key={s.id} season={s} />)}
        </section>
      )}

      {hasCommanderData && commanders.data && (
        <>
          <section>
            <SectionHeader title="Comandantes mais jogados" subtitle="todas as temporadas" link={{ to: '/comandantes', label: 'Ver ranking completo' }} />
            <CommanderArtCards rows={commanders.data} sort="played" />
          </section>

          <section className="grid gap-4 lg:grid-cols-2">
            <div className="card p-4">
              <SectionHeader title="Top comandantes" subtitle="% de vitória" link={{ to: '/comandantes?ordem=rate', label: 'Ver todos' }} />
              <CommanderList rows={commanders.data} sort="rate" limit={5} minMatches={3} />
            </div>
            <div className="card p-4">
              <SectionHeader title="Mais decks cadastrados" subtitle="na loja" link={{ to: '/comandantes?ordem=decks', label: 'Ver todos' }} />
              <CommanderList rows={commanders.data} sort="decks" limit={5} />
            </div>
          </section>
        </>
      )}

      {past.length > 0 && (
        <section>
          <SectionHeader title="Temporadas passadas" />
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {past.map((s) => <PastSeasonCard key={s.id} season={s} />)}
          </div>
        </section>
      )}
    </div>
  )
}

function SectionHeader({ title, subtitle, link }: { title: string; subtitle?: string; link?: { to: string; label: string } }) {
  return (
    <div className="mb-3 flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
      <h2 className="text-lg font-bold text-zinc-50">{title}</h2>
      {subtitle && <span className="hidden text-sm text-zinc-400 sm:inline">· {subtitle}</span>}
      {link && <Link to={link.to} className="ml-auto whitespace-nowrap text-sm font-medium text-amber-300 hover:underline">{link.label}</Link>}
    </div>
  )
}

/**
 * Full-width banner of a running season: the art of the commander with the most wins in the season,
 * a dark gradient for legibility and the main call to action.
 */
function SeasonHero({ season }: { season: Season }) {
  const stats = useCommanderStats(season.id)
  const ranking = useRanking(season.id)
  const star = featured(stats.data)
  const leader = ranking.data?.[0]

  return (
    <Link
      to={`/temporadas/${season.id}`}
      className="group relative isolate block min-h-[18rem] overflow-hidden rounded-3xl border border-zinc-800 shadow-2xl sm:min-h-[22rem]"
      aria-label={`${season.name}: ver ranking da temporada`}
    >
      {/* background: commander art, or brand gradient */}
      {star ? (
        <div className="absolute inset-0 -z-20">
          <CommanderArt commanders={star.commanders} className="size-full transition duration-700 group-hover:scale-105" rounded="rounded-none" />
        </div>
      ) : (
        <div className="absolute inset-0 -z-20 bg-[linear-gradient(135deg,var(--color-brand-navy),#0a1128_55%,var(--color-brand-red))]" />
      )}
      {/* shadow so the text reads on any art (fixed dark colors in both themes) */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-r from-black/80 via-black/35 to-transparent" />
      <div className="absolute inset-0 -z-10 bg-gradient-to-t from-black/75 via-black/10 to-transparent" />

      <div className="flex h-full min-h-[18rem] flex-col justify-end gap-3 p-5 text-white sm:min-h-[22rem] sm:p-8">
        <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-brand-orange px-3 py-1 text-xs font-bold uppercase tracking-wider text-white shadow">
          <span className="size-1.5 animate-pulse rounded-full bg-white" aria-hidden /> Temporada em andamento
        </span>
        <h1 className="font-display text-3xl font-bold leading-tight drop-shadow-lg sm:text-5xl">{season.name}</h1>
        <p className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-white/85">
          <span className="inline-flex items-center gap-1.5">
            <CalendarDays className="size-4" aria-hidden />
            {formatDate(season.startDate)}{season.endDate ? ` – ${formatDate(season.endDate)}` : ' – em aberto'}
          </span>
          <span>{season.stagesCount} etapa{season.stagesCount === 1 ? '' : 's'} · {season.roundsCount} rodada{season.roundsCount === 1 ? '' : 's'}</span>
        </p>

        <div className="flex flex-wrap gap-2 text-sm">
          {leader && (
            <span className="inline-flex items-center gap-1.5 rounded-xl bg-black/45 px-3 py-1.5 ring-1 ring-white/15 backdrop-blur-sm">
              <Trophy className="size-4 text-amber-300" aria-hidden /> Líder: <b>{leader.playerName}</b> · {leader.points} pts
            </span>
          )}
          {star && (
            <span className="inline-flex items-center gap-1.5 rounded-xl bg-black/45 px-3 py-1.5 ring-1 ring-white/15 backdrop-blur-sm">
              <Crown className="size-4 text-amber-300" aria-hidden /> Destaque: <b>{star.name}</b> · {star.wins} vitória{star.wins === 1 ? '' : 's'}
            </span>
          )}
        </div>

        <span className="mt-1 inline-flex w-fit items-center gap-2 rounded-xl bg-white px-5 py-2.5 text-sm font-bold text-[#0a1128] shadow-lg transition group-hover:bg-amber-50">
          Ver ranking da temporada <ChevronRight className="size-4 transition group-hover:translate-x-0.5" aria-hidden />
        </span>
      </div>
    </Link>
  )
}

function PastSeasonCard({ season }: { season: Season }) {
  return (
    <Link
      to={`/temporadas/${season.id}`}
      className="group flex items-center gap-4 rounded-2xl border border-zinc-800 bg-zinc-900/60 p-4 transition hover:border-zinc-600"
    >
      <div className="min-w-0 flex-1">
        <div className="truncate font-display text-lg font-bold text-zinc-100">{season.name}</div>
        <div className="mt-1 flex items-center gap-1.5 text-sm text-zinc-400">
          <CalendarDays className="size-4" aria-hidden />
          {formatDate(season.startDate)}{season.endDate ? ` – ${formatDate(season.endDate)}` : ''}
        </div>
        <div className="mt-1 text-xs text-zinc-500">{season.stagesCount} etapas · {season.roundsCount} rodadas</div>
      </div>
      <ChevronRight className="size-5 text-zinc-500 transition group-hover:translate-x-0.5 group-hover:text-zinc-300" aria-hidden />
    </Link>
  )
}
