import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react'
import { TamanhosPagina, type EstadoPaginacao } from '../utilitarios/paginacao'

/** Barra de paginação: "1–50 de 320", itens por página e botões de página. Some quando tudo cabe numa página só. */
export function Paginacao({ estado, rotulo = 'itens' }: { estado: EstadoPaginacao; rotulo?: string }) {
  const { pagina, totalPaginas, tamanho, total, irPara, mudarTamanho } = estado
  if (total <= TamanhosPagina[0]) return null
  const primeiro = (pagina - 1) * tamanho + 1
  const ultimo = Math.min(pagina * tamanho, total)
  const botao = 'botao-discreto botao-pequeno px-2 disabled:opacity-30'
  return (
    <nav className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-zinc-400" aria-label="Paginação">
      <span>{primeiro}–{ultimo} de {total} {rotulo}</span>
      <label className="flex items-center gap-2">
        Por página
        <select value={tamanho} onChange={(e) => mudarTamanho(Number(e.target.value))} className="py-1">
          {TamanhosPagina.map((n) => <option key={n} value={n}>{n}</option>)}
        </select>
      </label>
      <span className="ml-auto flex items-center gap-1">
        <button type="button" className={botao} disabled={pagina === 1} onClick={() => irPara(1)} aria-label="Primeira página"><ChevronsLeft className="size-4" /></button>
        <button type="button" className={botao} disabled={pagina === 1} onClick={() => irPara(pagina - 1)} aria-label="Página anterior"><ChevronLeft className="size-4" /></button>
        <span className="px-1 tabular-nums">Página {pagina} de {totalPaginas}</span>
        <button type="button" className={botao} disabled={pagina === totalPaginas} onClick={() => irPara(pagina + 1)} aria-label="Próxima página"><ChevronRight className="size-4" /></button>
        <button type="button" className={botao} disabled={pagina === totalPaginas} onClick={() => irPara(totalPaginas)} aria-label="Última página"><ChevronsRight className="size-4" /></button>
      </span>
    </nav>
  )
}
