import { useState } from 'react'

/** Tamanhos de página oferecidos (o padrão é o primeiro). */
export const TamanhosPagina = [50, 100, 250, 500] as const

/**
 * Paginação no navegador para listas que crescem com o tempo (comandantes, jogadores, rankings, cadastros).
 * Devolve só os itens da página atual e o estado para o componente <Paginacao>.
 * Mudar o filtro (chaveFiltro) volta para a página 1.
 */
export function usePaginacao<T>(itens: T[], chaveFiltro = '') {
  const [tamanho, definirTamanho] = useState<number>(TamanhosPagina[0])
  const [pagina, definirPagina] = useState(1)
  const [filtroAnterior, definirFiltroAnterior] = useState(chaveFiltro)
  // Filtro novo → volta para a primeira página (ajuste durante o render, sem efeito).
  if (filtroAnterior !== chaveFiltro) {
    definirFiltroAnterior(chaveFiltro)
    definirPagina(1)
  }
  const totalPaginas = Math.max(1, Math.ceil(itens.length / tamanho))
  const atual = Math.min(pagina, totalPaginas)
  const inicio = (atual - 1) * tamanho
  return {
    itensDaPagina: itens.slice(inicio, inicio + tamanho),
    /** Quantos itens vêm antes desta página (para numerar: a página 2 começa no 51). */
    deslocamento: inicio,
    estado: {
      pagina: atual, totalPaginas, tamanho, total: itens.length,
      irPara: (n: number) => definirPagina(Math.min(Math.max(1, n), totalPaginas)),
      mudarTamanho: (n: number) => { definirTamanho(n); definirPagina(1) },
    },
  }
}

export type EstadoPaginacao = ReturnType<typeof usePaginacao>['estado']
