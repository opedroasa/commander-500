import { Search, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { useCurrentRounds, useRound } from '../../api/hooks'
import type { Round } from '../../api/types'
import { RoundBoard } from '../../components/RoundBoard'
import { Empty, ErrorBox, PageHeader, RoundStatusBadge, Spinner } from '../../components/ui'
import { formatDate, searchKey } from '../../lib/format'

const POLL_MS = 10_000

/** Players matching the "find my seat" search across the given rounds. */
function useSeatSearch(rounds: Round[] | undefined) {
  const [query, setQuery] = useState('')
  const highlight = useMemo(() => {
    const q = searchKey(query)
    const ids = new Set<number>()
    if (q.length < 2 || !rounds) return ids
    for (const r of rounds)
      for (const e of r.entries)
        if (searchKey(`${e.playerName} ${e.nickname ?? ''}`).includes(q)) ids.add(e.playerId)
    return ids
  }, [query, rounds])

  const input = (
    <div className="relative mb-5">
      <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
      <input
        type="search"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Encontre sua mesa: digite seu nome"
        className="w-full pl-9 pr-9"
        aria-label="Buscar jogador"
      />
      {query && (
        <button className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-zinc-400 hover:text-zinc-100" onClick={() => setQuery('')} aria-label="Limpar busca">
          <X className="size-4" />
        </button>
      )}
      {query.length >= 2 && (
        <p className="mt-1.5 text-xs text-zinc-400">
          {highlight.size === 0 ? 'Nenhum jogador encontrado.' : `${highlight.size} jogador${highlight.size > 1 ? 'es' : ''} destacado${highlight.size > 1 ? 's' : ''}.`}
        </p>
      )}
    </div>
  )
  return { highlight, input }
}

/** /ao-vivo — every drawn / in-progress round of active seasons. */
export function LivePage() {
  const rounds = useCurrentRounds(POLL_MS)
  const { highlight, input } = useSeatSearch(rounds.data)

  return (
    <>
      <PageHeader title="Ao vivo" subtitle="Mesas sorteadas e tempo da rodada. Atualiza sozinho." />
      {rounds.isPending && <Spinner />}
      {rounds.error && <ErrorBox error={rounds.error} onRetry={() => rounds.refetch()} />}
      {rounds.data?.length === 0 && (
        <Empty>
          Nenhuma rodada acontecendo agora. <Link to="/" className="text-amber-300 hover:underline">Ver temporadas</Link>
        </Empty>
      )}
      {rounds.data && rounds.data.length > 0 && input}
      <div className="space-y-10">
        {rounds.data?.map((r) => (
          <section key={r.id}>
            <RoundHeading round={r} />
            <RoundBoard round={r} highlightPlayerIds={highlight} />
          </section>
        ))}
      </div>
    </>
  )
}

/** /rodadas/:id — a single round (live while open, results once closed). */
export function RoundPage() {
  const id = Number(useParams().id)
  const round = useRound(id, POLL_MS)
  const data = round.data
  const { highlight, input } = useSeatSearch(data ? [data] : undefined)

  if (round.isPending) return <Spinner />
  if (round.error) return <ErrorBox error={round.error} onRetry={() => round.refetch()} />
  const r = round.data

  return (
    <>
      <RoundHeading round={r} asPageTitle />
      {r.status === 'Draft' ? (
        <Empty>As mesas desta rodada ainda não foram sorteadas.</Empty>
      ) : (
        <>
          {r.status !== 'Closed' && input}
          <RoundBoard round={r} highlightPlayerIds={highlight} />
        </>
      )}
    </>
  )
}

function RoundHeading({ round, asPageTitle = false }: { round: Round; asPageTitle?: boolean }) {
  const Title = asPageTitle ? 'h1' : 'h2'
  return (
    <div className="mb-4 flex flex-wrap items-center gap-x-3 gap-y-1">
      <Title className="font-display text-2xl font-bold text-amber-100">Rodada {round.number}</Title>
      <RoundStatusBadge status={round.status} />
      <span className="w-full text-sm text-zinc-400 sm:w-auto">
        <Link to={`/temporadas/${round.seasonId}`} className="hover:text-zinc-200 hover:underline">{round.seasonName}</Link> · {formatDate(round.date)}
      </span>
    </div>
  )
}
