import type { Commander } from '../api/types'

/** Mana-color fallback for commanders without Scryfall art. */
const colorHex: Record<string, string> = { W: '#f8f1d4', U: '#3b82c4', B: '#4b4453', R: '#d9483b', G: '#2f8a4b' }

function fallbackGradient(identity: string | null | undefined): string {
  const colors = (identity ?? '').split('').map((c) => colorHex[c]).filter(Boolean)
  if (colors.length === 0) return 'linear-gradient(135deg, #2d3c63, #121c38)'
  if (colors.length === 1) return `linear-gradient(135deg, ${colors[0]}, #121c38)`
  return `linear-gradient(135deg, ${colors.join(', ')})`
}

/**
 * Commander artwork (Scryfall art crop). Partner/background pairs are shown side by side.
 * Fills its container: give the wrapper a size/aspect ratio via className.
 */
export function CommanderArt({ commanders, className = '', rounded = 'rounded-xl' }: {
  commanders: Pick<Commander, 'name' | 'artUrl' | 'colorIdentity'>[]
  className?: string
  rounded?: string
}) {
  return (
    <div className={`relative flex overflow-hidden bg-zinc-800 ${rounded} ${className}`}>
      {commanders.map((c) => (
        <div key={c.name} className="relative h-full min-w-0 flex-1" style={{ background: fallbackGradient(c.colorIdentity) }}>
          {c.artUrl && (
            <img src={c.artUrl} alt={c.name} loading="lazy" className="absolute inset-0 size-full object-cover" />
          )}
        </div>
      ))}
    </div>
  )
}

const pipStyle: Record<string, string> = {
  W: 'bg-[#f8f1d4] text-zinc-900',
  U: 'bg-[#3b82c4] text-white',
  B: 'bg-[#4b4453] text-white',
  R: 'bg-[#d9483b] text-white',
  G: 'bg-[#2f8a4b] text-white',
  C: 'bg-[#b8b4ad] text-zinc-900',
}

/** Small WUBRG color-identity pips. */
export function ColorPips({ identity, className = '' }: { identity: string | null | undefined; className?: string }) {
  if (identity == null) return null
  const letters = identity === '' ? ['C'] : identity.split('')
  return (
    <span className={`inline-flex gap-0.5 ${className}`} aria-label={`Cores: ${letters.join('')}`}>
      {letters.map((l) => (
        <span key={l} className={`inline-flex size-4 items-center justify-center rounded-full text-[0.55rem] font-bold ring-1 ring-black/30 ${pipStyle[l]}`}>
          {l}
        </span>
      ))}
    </span>
  )
}
