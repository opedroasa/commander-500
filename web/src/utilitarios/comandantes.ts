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

/** "12-34": ids dos comandantes em ordem (endereço da página do comandante; dupla de parceiros = uma chave). */
export function chaveComandantes(comandantes: Pick<Comandante, 'id'>[]): string {
  return comandantes.map((c) => c.id).sort((a, b) => a - b).join('-')
}

/** Cores do filtro: W U B R G e C (incolor). */
export type CorFiltro = 'W' | 'U' | 'B' | 'R' | 'G' | 'C'

/**
 * Filtro "exatamente essas cores": a identidade do comandante (ou dupla) tem que ser igual às cores marcadas.
 * Nada marcado = todos. "C" sozinho = só incolores. Identidade desconhecida não entra quando há filtro.
 */
export function temExatamenteAsCores(comandantes: Pick<Comandante, 'identidadeCor'>[], cores: CorFiltro[]): boolean {
  if (cores.length === 0) return true
  const identidade = identidadeCombinada(comandantes)
  if (identidade == null) return false
  const marcadas = cores.filter((c) => c !== 'C').join('')
  const ordenadas = 'WUBRG'.split('').filter((letra) => marcadas.includes(letra)).join('')
  return identidade === ordenadas
}
