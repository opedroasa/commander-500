import type { CriterioDesempate, SituacaoDeck, SituacaoRodada } from '../api/tipos'

export const rotuloSituacaoRodada: Record<SituacaoRodada, string> = {
  Rascunho: 'Rascunho',
  Sorteada: 'Sorteada',
  EmAndamento: 'Em andamento',
  Encerrada: 'Encerrada',
}

export const rotuloSituacaoDeck: Record<SituacaoDeck, string> = {
  Pendente: 'Pendente',
  Aprovado: 'Aprovado',
  Reprovado: 'Reprovado',
}

export const rotuloCriterioDesempate: Record<CriterioDesempate, string> = {
  Vitorias: 'Mais vitórias',
  MenosPartidas: 'Menos partidas jogadas',
  MaisPartidas: 'Mais partidas jogadas',
  Empates: 'Mais empates',
  PercentualVitoria: 'Maior % de vitória',
}

/** "2026-11-10" → "10/11/2026" (data sem hora, sem mudar o dia por causa do fuso). */
export function formatarData(iso: string | null | undefined): string {
  if (!iso) return ''
  const [ano, mes, dia] = iso.slice(0, 10).split('-')
  return `${dia}/${mes}/${ano}`
}

export function formatarDataHora(iso: string | null | undefined): string {
  if (!iso) return ''
  return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

/** 0.333 → "33%"; nulo → "–". */
export function formatarPercentual(valor: number | null | undefined): string {
  if (valor === null || valor === undefined) return '–'
  return `${Math.round(valor * 100)}%`
}

/** Segundos → "59:18" ou "1:05:00". */
export function formatarRelogio(totalSegundos: number): string {
  const s = Math.max(0, Math.floor(totalSegundos))
  const horas = Math.floor(s / 3600)
  const minutos = Math.floor((s % 3600) / 60)
  const segundos = s % 60
  const mm = String(minutos).padStart(horas ? 2 : 1, '0')
  const ss = String(segundos).padStart(2, '0')
  return horas ? `${horas}:${mm}:${ss}` : `${mm}:${ss}`
}

/** Hoje no fuso do navegador, no formato aaaa-mm-dd. */
export function hojeIso(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Chave de busca que ignora acento e maiúscula. */
export function chaveBusca(texto: string): string {
  return texto.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase().trim()
}

export function nomeComApelido(nome: string, apelido: string | null | undefined): string {
  return apelido ? `${nome} (${apelido})` : nome
}
