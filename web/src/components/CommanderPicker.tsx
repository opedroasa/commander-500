import { Loader2, Search, X } from 'lucide-react'
import { useEffect, useId, useState } from 'react'
import { ApiError } from '../api/client'
import { useScryfallSearch } from '../api/hooks'
import type { CommanderInput, ScryfallCard } from '../api/types'
import { ColorPips } from './CommanderArt'

function useDebounced<T>(value: T, ms: number): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const t = window.setTimeout(() => setDebounced(value), ms)
    return () => window.clearTimeout(t)
  }, [value, ms])
  return debounced
}

export interface PickedCommander extends CommanderInput {
  artUrl?: string | null
  colorIdentity?: string | null
}

/**
 * Commander field with Scryfall search. Picking a result stores its Scryfall id (name, art and colors come
 * from Scryfall). If Scryfall is down or the card isn't found, the typed name is used as is.
 */
export function CommanderPicker({ label, value, onChange, required = false }: {
  label: string
  value: PickedCommander | null
  onChange: (v: PickedCommander | null) => void
  required?: boolean
}) {
  const [query, setQuery] = useState(value?.name ?? '')
  const [open, setOpen] = useState(false)
  const debounced = useDebounced(query, 300)
  const search = useScryfallSearch(open ? debounced : '')
  const listId = useId()
  const unavailable = search.error instanceof ApiError && search.error.status === 503

  function pick(card: ScryfallCard) {
    onChange({ scryfallId: card.id, name: card.name, artUrl: card.artUrl, colorIdentity: card.colorIdentity })
    setQuery(card.name)
    setOpen(false)
  }

  function type(text: string) {
    setQuery(text)
    setOpen(true)
    onChange(text.trim() ? { scryfallId: null, name: text } : null)
  }

  return (
    <div className="relative">
      <span className="label">{label}</span>
      <div className="relative">
        {value?.artUrl ? (
          <img src={value.artUrl} alt="" className="pointer-events-none absolute left-1.5 top-1/2 h-7 w-10 -translate-y-1/2 rounded object-cover" />
        ) : (
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
        )}
        <input
          value={query}
          required={required}
          onChange={(e) => type(e.target.value)}
          onFocus={() => setOpen(true)}
          onBlur={() => window.setTimeout(() => setOpen(false), 150)}
          placeholder="Digite o nome (ex.: Atraxa)"
          autoComplete="off"
          role="combobox"
          aria-expanded={open}
          aria-controls={listId}
          className={`w-full pr-9 ${value?.artUrl ? 'pl-14' : 'pl-9'}`}
        />
        {query && (
          <button type="button" className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-zinc-400 hover:text-zinc-100"
            onClick={() => { setQuery(''); onChange(null) }} aria-label="Limpar">
            <X className="size-4" />
          </button>
        )}
      </div>
      <p className="mt-1 text-xs text-zinc-500">
        {value?.scryfallId
          ? 'Encontrado no Scryfall.'
          : unavailable
            ? 'Scryfall indisponível agora — o nome digitado será usado e a arte é buscada depois.'
            : query.trim().length >= 2 ? 'Escolha um resultado para trazer a arte e as cores.' : 'Busca no Scryfall.'}
      </p>

      {open && debounced.trim().length >= 2 && !unavailable && (
        <ul id={listId} role="listbox" className="absolute z-20 mt-1 max-h-72 w-full overflow-y-auto rounded-xl border border-zinc-700 bg-zinc-900 p-1 shadow-xl">
          {search.isFetching && (
            <li className="flex items-center gap-2 px-3 py-2 text-sm text-zinc-400"><Loader2 className="size-4 animate-spin" aria-hidden />Buscando…</li>
          )}
          {!search.isFetching && search.data?.length === 0 && (
            <li className="px-3 py-2 text-sm text-zinc-400">Nenhum comandante encontrado. O nome digitado será usado.</li>
          )}
          {search.data?.map((card) => (
            <li key={card.id} role="option" aria-selected={value?.scryfallId === card.id}>
              <button type="button" onMouseDown={(e) => e.preventDefault()} onClick={() => pick(card)}
                className="flex w-full items-center gap-3 rounded-lg p-1.5 text-left hover:bg-zinc-800">
                {card.artUrl
                  ? <img src={card.artUrl} alt="" loading="lazy" className="h-9 w-12 shrink-0 rounded object-cover" />
                  : <span className="h-9 w-12 shrink-0 rounded bg-zinc-800" />}
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-medium text-zinc-100">{card.name}</span>
                  <span className="block truncate text-xs text-zinc-400">{card.typeLine}</span>
                </span>
                <ColorPips identity={card.colorIdentity} />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
