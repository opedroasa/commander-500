import { ExternalLink, Flag, Medal, Pencil, Plus, Trophy } from 'lucide-react'
import { useState } from 'react'
import { usePlayerProfile } from '../api/hooks'
import type { Deck, PlayerStats } from '../api/types'
import { formatDate, formatPercent } from '../lib/format'
import { combinedIdentity } from '../lib/commanders'
import { ColorPips, CommanderArt } from './CommanderArt'
import { DeckFormModal } from './DeckFormModal'
import { DeckStatusBadge, ErrorBox, Modal, Spinner } from './ui'

/**
 * Player mini profile: overall stats, the current season (when opened from one), titles and decks.
 * In admin mode it also shows deck status/notes and lets the store add or edit decks.
 */
export function PlayerProfileModal({ playerId, seasonId = null, admin = false, onClose }: {
  playerId: number | null
  seasonId?: number | null
  admin?: boolean
  onClose: () => void
}) {
  const profile = usePlayerProfile(playerId, seasonId)
  const [deckForm, setDeckForm] = useState<Deck | 'new' | null>(null)
  const p = profile.data

  return (
    <>
      {/* The profile stays open under the deck form (native <dialog>s stack; Esc closes the top one). */}
      <Modal open={playerId != null} onClose={onClose} title={p ? p.name + (p.nickname ? ` (${p.nickname})` : '') : 'Jogador'} wide>
        {profile.isPending && <Spinner />}
        {profile.error && <ErrorBox error={profile.error} onRetry={() => profile.refetch()} />}
        {p && (
          <div className="space-y-5">
            {admin && !p.isActive && <p className="rounded-lg bg-zinc-800 px-3 py-2 text-sm text-zinc-300">Jogador inativo.</p>}

            {p.season && (
              <section>
                <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-zinc-400">{p.season.seasonName}</h3>
                <div className="mb-2 flex flex-wrap items-baseline gap-x-4 gap-y-1">
                  <span className="font-display text-3xl font-bold text-amber-100">{p.season.position ? `${p.season.position}º` : '–'}</span>
                  <span className="text-lg font-semibold text-amber-300">{p.season.points} pts</span>
                </div>
                <StatGrid stats={p.season.stats} />
              </section>
            )}

            <section>
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-zinc-400">
                Geral · {p.seasonsPlayed} temporada{p.seasonsPlayed === 1 ? '' : 's'} jogada{p.seasonsPlayed === 1 ? '' : 's'}
              </h3>
              <StatGrid stats={p.overall} />
            </section>

            {(p.seasonsWon.length > 0 || p.stagesWon.length > 0) && (
              <section>
                <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-zinc-400">Conquistas</h3>
                <ul className="flex flex-wrap gap-2">
                  {p.seasonsWon.map((s) => (
                    <li key={`s${s.id}`} className="inline-flex items-center gap-1.5 rounded-full bg-amber-500/15 px-3 py-1 text-sm font-medium text-amber-200 ring-1 ring-amber-500/40">
                      <Trophy className="size-4" aria-hidden /> Campeão · {s.name}
                    </li>
                  ))}
                  {p.stagesWon.map((s) => (
                    <li key={`e${s.id}`} className="inline-flex items-center gap-1.5 rounded-full bg-zinc-800 px-3 py-1 text-sm text-zinc-200">
                      <Medal className="size-4 text-amber-400" aria-hidden /> Etapa {s.number} · {formatDate(s.date)}
                    </li>
                  ))}
                </ul>
              </section>
            )}

            <section>
              <div className="mb-2 flex items-center gap-2">
                <h3 className="flex-1 text-xs font-semibold uppercase tracking-widest text-zinc-400">Decks ({p.decks.length})</h3>
                {admin && (
                  <button className="btn-primary btn-sm" onClick={() => setDeckForm('new')}>
                    <Plus className="size-4" aria-hidden /> Novo deck
                  </button>
                )}
              </div>
              {p.decks.length === 0 && <p className="text-sm text-zinc-400">Nenhum deck cadastrado.</p>}
              <ul className="grid gap-2 sm:grid-cols-2">
                {p.decks.map((d) => (
                  <li key={d.id} className="flex gap-3 rounded-xl border border-zinc-800 bg-zinc-900 p-2">
                    <CommanderArt commanders={d.commanders} className="h-16 w-20 shrink-0" rounded="rounded-lg" />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-start gap-1">
                        <span className="min-w-0 flex-1 truncate text-sm font-semibold">{d.name}</span>
                        {admin && (
                          <button className="rounded p-0.5 text-zinc-400 hover:text-zinc-100" onClick={() => setDeckForm(d)} aria-label={`Editar ${d.name}`}>
                            <Pencil className="size-3.5" />
                          </button>
                        )}
                      </div>
                      <div className="truncate text-xs text-zinc-400">{d.commanders.map((c) => c.name).join(' + ')}</div>
                      <div className="mt-1 flex flex-wrap items-center gap-2">
                        <ColorPips identity={combinedIdentity(d.commanders)} />
                        {admin && <DeckStatusBadge status={d.status} />}
                        {d.listUrl && (
                          <a href={d.listUrl} target="_blank" rel="noreferrer noopener" className="inline-flex items-center gap-1 text-xs text-amber-300 hover:underline">
                            Lista <ExternalLink className="size-3" aria-hidden />
                          </a>
                        )}
                      </div>
                      {admin && d.reviewNote && <p className="mt-1 text-xs text-zinc-500"><Flag className="mr-1 inline size-3" aria-hidden />{d.reviewNote}</p>}
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          </div>
        )}
      </Modal>

      {deckForm !== null && playerId != null && (
        <DeckFormModal
          open
          deck={deckForm === 'new' ? null : deckForm}
          playerId={playerId}
          onClose={() => setDeckForm(null)}
        />
      )}
    </>
  )
}

function StatGrid({ stats }: { stats: PlayerStats }) {
  const items = [
    { label: 'Participações', value: stats.rounds, hint: 'rodadas (inclui byes)' },
    { label: 'Vitórias', value: stats.wins },
    { label: 'Derrotas', value: stats.losses },
    { label: 'Empates', value: stats.draws },
    { label: 'Byes', value: stats.byes },
    { label: '% Vitória', value: formatPercent(stats.winRate), hint: 'vitórias ÷ partidas jogadas' },
  ]
  return (
    <dl className="grid grid-cols-3 gap-2 sm:grid-cols-6">
      {items.map((i) => (
        <div key={i.label} className="flex flex-col-reverse rounded-xl border border-zinc-800 bg-zinc-900 px-2 py-2 text-center" title={i.hint}>
          <dt className="text-[0.65rem] uppercase tracking-wide text-zinc-400">{i.label}</dt>
          <dd className="text-xl font-bold tabular-nums text-zinc-50">{i.value}</dd>
        </div>
      ))}
    </dl>
  )
}
