import type { Comandante, EstatisticaComandante } from '../api/tipos'

/** Ordenações das listas de comandantes (o valor vai na URL: /comandantes?ordem=percentual). */
export type OrdemComandantes = 'jogados' | 'vitorias' | 'percentual' | 'decks'

export const rotuloOrdemComandantes: Record<OrdemComandantes, string> = {
  jogados: 'Mais jogados',
  vitorias: 'Mais vitórias',
  percentual: '% de vitória',
  decks: 'Mais decks',
}

const porNome = (a: EstatisticaComandante, b: EstatisticaComandante) => a.nome.localeCompare(b.nome)
const comparadores: Record<OrdemComandantes, (a: EstatisticaComandante, b: EstatisticaComandante) => number> = {
  jogados: (a, b) => b.partidas - a.partidas || b.vitorias - a.vitorias || porNome(a, b),
  vitorias: (a, b) => b.vitorias - a.vitorias || (b.percentualVitoria ?? -1) - (a.percentualVitoria ?? -1) || porNome(a, b),
  percentual: (a, b) => (b.percentualVitoria ?? -1) - (a.percentualVitoria ?? -1) || b.partidas - a.partidas || porNome(a, b),
  decks: (a, b) => b.decks - a.decks || b.partidas - a.partidas || porNome(a, b),
}

/** Cópia ordenada. "percentual" só lista quem atingiu o mínimo de partidas; "decks" só quem tem deck. */
export function ordenarComandantes(linhas: EstatisticaComandante[], ordem: OrdemComandantes): EstatisticaComandante[] {
  const lista = ordem === 'percentual'
    ? linhas.filter((l) => l.percentualVitoria !== null)
    : ordem === 'decks' ? linhas.filter((l) => l.decks > 0) : linhas.filter((l) => l.partidas > 0)
  return [...lista].sort(comparadores[ordem])
}

/** Junta as cores de uma dupla de comandantes, na ordem WUBRG. Nulo quando ainda não se conhece. */
export function identidadeCombinada(comandantes: Pick<Comandante, 'identidadeCor'>[]): string | null {
  if (comandantes.every((c) => c.identidadeCor == null)) return null
  const todas = comandantes.map((c) => c.identidadeCor ?? '').join('')
  return 'WUBRG'.split('').filter((letra) => todas.includes(letra)).join('')
}
