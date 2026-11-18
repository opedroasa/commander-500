import { useCallback, useState } from 'react'

export type Tema = 'claro' | 'escuro'
const CHAVE = 'c500.tema'

/** O CSS usa html[data-tema="claro"] (ver index.css). */
function temaAtual(): Tema {
  return document.documentElement.dataset.tema === 'claro' ? 'claro' : 'escuro'
}

/** Tema claro/escuro. O valor inicial é aplicado pelo script no index.html (sem piscar a tela). */
export function useTema() {
  const [tema, definirEstado] = useState<Tema>(temaAtual)

  const definirTema = useCallback((novo: Tema) => {
    document.documentElement.dataset.tema = novo
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', novo === 'claro' ? '#ffffff' : '#0a1128')
    try {
      localStorage.setItem(CHAVE, novo)
    } catch {
      /* navegador sem armazenamento: o tema vale só nesta página */
    }
    definirEstado(novo)
  }, [])

  const alternar = useCallback(() => definirTema(temaAtual() === 'claro' ? 'escuro' : 'claro'), [definirTema])
  return { tema, definirTema, alternar }
}
