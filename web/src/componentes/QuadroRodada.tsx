import { Coffee } from 'lucide-react'
import type { Rodada } from '../api/tipos'
import { Cronometro } from './Cronometro'
import { DesenhoMesa } from './DesenhoMesa'

/** Rodada só para ver: timer, mesas desenhadas e byes. Usado na página pública "Ao vivo". */
export function QuadroRodada({ rodada, idsJogadoresDestacados }: { rodada: Rodada; idsJogadoresDestacados?: Set<number> }) {
  return (
    <div className="space-y-5">
      {rodada.situacao === 'EmAndamento' && rodada.cronometro && (
        <Cronometro cronometro={rodada.cronometro} agoraServidor={rodada.agoraServidor} turnosExtras={rodada.turnosExtrasAposTempo} />
      )}
      {rodada.situacao === 'Sorteada' && (
        <p className="rounded-xl border border-sky-900 bg-sky-950/40 p-3 text-center text-sm text-sky-200">
          Mesas sorteadas — procurem seus lugares. O timer começa quando o lojista iniciar a rodada.
        </p>
      )}

      <GradeMesas rodada={rodada} idsJogadoresDestacados={idsJogadoresDestacados} />
      <ListaByes rodada={rodada} idsJogadoresDestacados={idsJogadoresDestacados} />
    </div>
  )
}

export function GradeMesas({ rodada, idsJogadoresDestacados }: { rodada: Rodada; idsJogadoresDestacados?: Set<number> }) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {rodada.mesas.map((mesa) => (
        <section key={mesa.id} className="cartao p-3" aria-label={`Mesa ${mesa.numero}`}>
          <DesenhoMesa mesa={mesa} idsJogadoresDestacados={idsJogadoresDestacados} />
        </section>
      ))}
    </div>
  )
}

export function ListaByes({ rodada, idsJogadoresDestacados }: { rodada: Rodada; idsJogadoresDestacados?: Set<number> }) {
  if (rodada.byes.length === 0) return null
  return (
    <section className="cartao p-4">
      <h3 className="mb-2 flex items-center gap-2 text-sm font-semibold text-zinc-200">
        <Coffee className="size-4 text-amber-400" aria-hidden /> Bye nesta rodada
      </h3>
      <ul className="flex flex-wrap gap-2">
        {rodada.byes.map((bye) => (
          <li
            key={bye.id}
            className={`rounded-lg border px-3 py-1.5 text-sm ${
              idsJogadoresDestacados?.has(bye.idJogador) ? 'border-sky-400 bg-sky-500/15' : 'border-zinc-800 bg-zinc-900'
            }`}
          >
            {bye.nomeJogador}
          </li>
        ))}
      </ul>
      <p className="mt-2 text-xs text-zinc-500">Quem fica de bye recebe os pontos de bye da temporada sem jogar.</p>
    </section>
  )
}
