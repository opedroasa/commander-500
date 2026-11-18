import type { EstatisticaComandante } from '../api/tipos'
import { identidadeCombinada, ordenarComandantes, type OrdemComandantes } from '../utilitarios/comandantes'
import { formatarPercentual } from '../utilitarios/formatacao'
import { ArteComandante, CoresMana } from './ArteComandante'

const plural = (n: number, singular: string, varios: string) => `${n} ${n === 1 ? singular : varios}`

/** Cartões grandes com a arte (os N primeiros), como "Comandantes mais jogados". */
export function CartoesArteComandante({ linhas, ordem = 'jogados', limite = 4 }: { linhas: EstatisticaComandante[]; ordem?: OrdemComandantes; limite?: number }) {
  const primeiros = ordenarComandantes(linhas, ordem).slice(0, limite)
  if (primeiros.length === 0) return null
  return (
    <ol className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      {primeiros.map((linha, indice) => (
        <li key={linha.nome} className="group relative overflow-hidden rounded-2xl border border-zinc-800 shadow-lg">
          <ArteComandante comandantes={linha.comandantes} className="aspect-[4/3] w-full transition duration-500 group-hover:scale-105" arredondamento="rounded-none" />
          <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/30 to-transparent" />
          <span className="absolute left-2 top-2 flex size-7 items-center justify-center rounded-full bg-black/60 text-sm font-bold text-white ring-1 ring-white/20">
            {indice + 1}
          </span>
          <div className="absolute inset-x-0 bottom-0 p-3">
            <div className="line-clamp-2 text-sm font-bold leading-tight text-white drop-shadow sm:text-base">{linha.nome}</div>
            <div className="mt-0.5 text-xs text-white/80">
              {ordem === 'decks'
                ? plural(linha.decks, 'deck', 'decks')
                : `${plural(linha.partidas, 'partida', 'partidas')} · ${plural(linha.jogadores, 'jogador', 'jogadores')}`}
            </div>
          </div>
        </li>
      ))}
    </ol>
  )
}

/** Lista numerada com miniatura, como "Top comandantes". */
export function ListaComandantes({ linhas, ordem, limite, minimoPartidas }: {
  linhas: EstatisticaComandante[]
  ordem: OrdemComandantes
  limite?: number
  minimoPartidas?: number
}) {
  const lista = ordenarComandantes(linhas, ordem).slice(0, limite)
  if (lista.length === 0) {
    return (
      <p className="rounded-xl border border-dashed border-zinc-800 p-6 text-center text-sm text-zinc-400">
        {ordem === 'percentual' && minimoPartidas ? `Nenhum comandante com ${minimoPartidas}+ partidas ainda.` : 'Nenhum dado ainda.'}
      </p>
    )
  }
  return (
    <ol className="divide-y divide-zinc-800/80">
      {lista.map((linha, indice) => {
        const valorPrincipal = ordem === 'jogados' ? linha.partidas : ordem === 'vitorias' ? linha.vitorias
          : ordem === 'decks' ? linha.decks : formatarPercentual(linha.percentualVitoria)
        return (
          <li key={linha.nome} className="flex items-center gap-3 py-2.5">
            <span className="w-5 shrink-0 text-center text-sm font-semibold text-zinc-500">{indice + 1}</span>
            <ArteComandante comandantes={linha.comandantes} className="h-10 w-14 shrink-0" arredondamento="rounded-md" />
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-semibold text-zinc-100">{linha.nome}</div>
              <div className="flex items-center gap-2 text-xs text-zinc-400">
                <CoresMana identidadeCor={identidadeCombinada(linha.comandantes)} />
                <span className="truncate">
                  {plural(linha.partidas, 'partida', 'partidas')} · {plural(linha.vitorias, 'vitória', 'vitórias')} · {plural(linha.decks, 'deck', 'decks')}
                </span>
              </div>
            </div>
            <div className="text-right">
              <div className="text-base font-bold tabular-nums text-amber-300">{valorPrincipal}</div>
              {ordem !== 'percentual' && (
                <div className="text-[0.7rem] text-zinc-500">
                  {linha.percentualVitoria === null ? (minimoPartidas ? `% após ${minimoPartidas} partidas` : '–') : `${formatarPercentual(linha.percentualVitoria)} de vitória`}
                </div>
              )}
            </div>
          </li>
        )
      })}
    </ol>
  )
}
