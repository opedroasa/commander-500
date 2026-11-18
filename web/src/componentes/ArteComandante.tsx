import type { Comandante } from '../api/tipos'

/** Cores de mana usadas quando o comandante ainda não tem arte do Scryfall. */
const corMana: Record<string, string> = { W: '#f8f1d4', U: '#3b82c4', B: '#4b4453', R: '#d9483b', G: '#2f8a4b' }

function degradeReserva(identidadeCor: string | null | undefined): string {
  const cores = (identidadeCor ?? '').split('').map((letra) => corMana[letra]).filter(Boolean)
  if (cores.length === 0) return 'linear-gradient(135deg, #2d3c63, #121c38)'
  if (cores.length === 1) return `linear-gradient(135deg, ${cores[0]}, #121c38)`
  return `linear-gradient(135deg, ${cores.join(', ')})`
}

/**
 * Arte do comandante (recorte do Scryfall). Duplas de parceiros/background aparecem lado a lado.
 * Ocupa o espaço do container: o tamanho/proporção vem pelo className.
 */
export function ArteComandante({ comandantes, className = '', arredondamento = 'rounded-xl' }: {
  comandantes: Pick<Comandante, 'nome' | 'urlArte' | 'identidadeCor'>[]
  className?: string
  arredondamento?: string
}) {
  return (
    <div className={`relative flex overflow-hidden bg-zinc-800 ${arredondamento} ${className}`}>
      {comandantes.map((c) => (
        <div key={c.nome} className="relative h-full min-w-0 flex-1" style={{ background: degradeReserva(c.identidadeCor) }}>
          {c.urlArte && <img src={c.urlArte} alt={c.nome} loading="lazy" className="absolute inset-0 size-full object-cover" />}
        </div>
      ))}
    </div>
  )
}

const estiloBolinha: Record<string, string> = {
  W: 'bg-[#f8f1d4] text-zinc-900',
  U: 'bg-[#3b82c4] text-white',
  B: 'bg-[#4b4453] text-white',
  R: 'bg-[#d9483b] text-white',
  G: 'bg-[#2f8a4b] text-white',
  C: 'bg-[#b8b4ad] text-zinc-900',
}

/** Bolinhas das cores (WUBRG) do comandante. */
export function CoresMana({ identidadeCor, className = '' }: { identidadeCor: string | null | undefined; className?: string }) {
  if (identidadeCor == null) return null
  const letras = identidadeCor === '' ? ['C'] : identidadeCor.split('')
  return (
    <span className={`inline-flex gap-0.5 ${className}`} aria-label={`Cores: ${letras.join('')}`}>
      {letras.map((letra) => (
        <span key={letra} className={`inline-flex size-4 items-center justify-center rounded-full text-[0.55rem] font-bold ring-1 ring-black/30 ${estiloBolinha[letra]}`}>
          {letra}
        </span>
      ))}
    </span>
  )
}
