import { useSyncExternalStore } from 'react'

/** Relógio compartilhado: um único intervalo para todos os timers da página. */
const relogio = { agora: Date.now(), ouvintes: new Set<() => void>(), intervalo: 0 }

function inscrever(ouvinte: () => void) {
  relogio.ouvintes.add(ouvinte)
  if (relogio.ouvintes.size === 1) {
    relogio.agora = Date.now()
    relogio.intervalo = window.setInterval(() => {
      relogio.agora = Date.now()
      relogio.ouvintes.forEach((o) => o())
    }, 500)
  }
  return () => {
    relogio.ouvintes.delete(ouvinte)
    if (relogio.ouvintes.size === 0) window.clearInterval(relogio.intervalo)
  }
}

/** Hora atual do navegador (ms), atualizando a tela a cada 500 ms enquanto o componente está aberto. */
export function useAgora(): number {
  return useSyncExternalStore(inscrever, () => relogio.agora)
}

/** Leitura nova do relógio, para cálculos pontuais (ex.: diferença para o relógio do servidor). */
export function lerAgora(): number {
  relogio.agora = Date.now()
  return relogio.agora
}
