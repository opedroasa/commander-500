import type { DeckStatus, RoundStatus, TiebreakCriterion } from '../api/types'

export const roundStatusLabel: Record<RoundStatus, string> = {
  Draft: 'Rascunho',
  Drawn: 'Sorteada',
  InProgress: 'Em andamento',
  Closed: 'Encerrada',
}

export const deckStatusLabel: Record<DeckStatus, string> = {
  Pending: 'Pendente',
  Approved: 'Aprovado',
  Rejected: 'Reprovado',
}

export const tiebreakLabel: Record<TiebreakCriterion, string> = {
  Wins: 'Mais vitórias',
  FewerMatches: 'Menos partidas jogadas',
  MoreMatches: 'Mais partidas jogadas',
  Draws: 'Mais empates',
  WinRate: 'Maior % de vitória',
}

/** "2026-11-10" → "10/11/2026" (date-only strings, no timezone shift). */
export function formatDate(iso: string | null | undefined): string {
  if (!iso) return ''
  const [y, m, d] = iso.slice(0, 10).split('-')
  return `${d}/${m}/${y}`
}

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return ''
  return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

export function formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined) return '–'
  return `${Math.round(value * 100)}%`
}

export function formatClock(totalSeconds: number): string {
  const s = Math.max(0, Math.floor(totalSeconds))
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const sec = s % 60
  const mm = String(m).padStart(h ? 2 : 1, '0')
  const ss = String(sec).padStart(2, '0')
  return h ? `${h}:${mm}:${ss}` : `${mm}:${ss}`
}

/** Today in the browser's timezone as yyyy-mm-dd. */
export function todayIso(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Case/accent-insensitive search key. */
export function searchKey(s: string): string {
  return s.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase().trim()
}

export function playerLabel(name: string, nickname: string | null | undefined): string {
  return nickname ? `${name} (${nickname})` : name
}
