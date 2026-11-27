import type { ReactNode } from 'react'

/** Quadro de número em destaque ("Partidas 42"), usado nas páginas do jogador e do comandante. */
export function Numero({ rotulo, valor, detalhe, destaque = false }: { rotulo: string; valor: ReactNode; detalhe?: ReactNode; destaque?: boolean }) {
  return (
    <div className={`cartao px-4 py-3 ${destaque ? 'ring-1 ring-amber-500/60' : ''}`}>
      <div className="text-xs font-medium uppercase tracking-wide text-zinc-400">{rotulo}</div>
      <div className={`mt-0.5 font-display text-2xl font-bold tabular-nums ${destaque ? 'text-amber-300' : 'text-zinc-50'}`}>{valor}</div>
      {detalhe && <div className="text-xs text-zinc-500">{detalhe}</div>}
    </div>
  )
}

/** Grade de números: 2 colunas no celular, até 4 ou 6 em telas maiores. */
export function GradeNumeros({ children }: { children: ReactNode }) {
  return <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">{children}</div>
}
