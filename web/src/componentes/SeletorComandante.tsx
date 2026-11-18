import { Loader2, Search, X } from 'lucide-react'
import { useEffect, useId, useState } from 'react'
import { ErroApi } from '../api/cliente'
import { useBuscaScryfall } from '../api/consultas'
import type { CartaScryfall, ComandanteEnvio } from '../api/tipos'
import { CoresMana } from './ArteComandante'

/** Espera o usuário parar de digitar antes de buscar. */
function useAtrasado<T>(valor: T, ms: number): T {
  const [atrasado, definirAtrasado] = useState(valor)
  useEffect(() => {
    const temporizador = window.setTimeout(() => definirAtrasado(valor), ms)
    return () => window.clearTimeout(temporizador)
  }, [valor, ms])
  return atrasado
}

export interface ComandanteEscolhido extends ComandanteEnvio {
  urlArte?: string | null
  identidadeCor?: string | null
}

/**
 * Campo de comandante com busca no Scryfall. Escolher um resultado guarda o id do Scryfall (nome, arte e cores
 * vêm de lá). Se o Scryfall estiver fora do ar ou a carta não for encontrada, o nome digitado é usado como está.
 */
export function SeletorComandante({ rotulo, valor, aoMudar, obrigatorio = false }: {
  rotulo: string
  valor: ComandanteEscolhido | null
  aoMudar: (novo: ComandanteEscolhido | null) => void
  obrigatorio?: boolean
}) {
  const [texto, definirTexto] = useState(valor?.nome ?? '')
  const [aberto, definirAberto] = useState(false)
  const textoAtrasado = useAtrasado(texto, 300)
  const busca = useBuscaScryfall(aberto ? textoAtrasado : '')
  const idLista = useId()
  const scryfallForaDoAr = busca.error instanceof ErroApi && busca.error.status === 503

  function escolher(carta: CartaScryfall) {
    aoMudar({ idScryfall: carta.id, nome: carta.nome, urlArte: carta.urlArte, identidadeCor: carta.identidadeCor })
    definirTexto(carta.nome)
    definirAberto(false)
  }

  function digitar(novoTexto: string) {
    definirTexto(novoTexto)
    definirAberto(true)
    aoMudar(novoTexto.trim() ? { idScryfall: null, nome: novoTexto } : null)
  }

  return (
    <div className="relative">
      <span className="rotulo">{rotulo}</span>
      <div className="relative">
        {valor?.urlArte ? (
          <img src={valor.urlArte} alt="" className="pointer-events-none absolute left-1.5 top-1/2 h-7 w-10 -translate-y-1/2 rounded object-cover" />
        ) : (
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
        )}
        <input
          value={texto}
          required={obrigatorio}
          onChange={(e) => digitar(e.target.value)}
          onFocus={() => definirAberto(true)}
          onBlur={() => window.setTimeout(() => definirAberto(false), 150)}
          placeholder="Digite o nome (ex.: Atraxa)"
          autoComplete="off"
          role="combobox"
          aria-expanded={aberto}
          aria-controls={idLista}
          className={`w-full pr-9 ${valor?.urlArte ? 'pl-14' : 'pl-9'}`}
        />
        {texto && (
          <button type="button" className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-zinc-400 hover:text-zinc-100"
            onClick={() => { definirTexto(''); aoMudar(null) }} aria-label="Limpar">
            <X className="size-4" />
          </button>
        )}
      </div>
      <p className="mt-1 text-xs text-zinc-500">
        {valor?.idScryfall
          ? 'Encontrado no Scryfall.'
          : scryfallForaDoAr
            ? 'Scryfall indisponível agora — o nome digitado será usado e a arte é buscada depois.'
            : texto.trim().length >= 2 ? 'Escolha um resultado para trazer a arte e as cores.' : 'Busca no Scryfall.'}
      </p>

      {aberto && textoAtrasado.trim().length >= 2 && !scryfallForaDoAr && (
        <ul id={idLista} role="listbox" className="absolute z-20 mt-1 max-h-72 w-full overflow-y-auto rounded-xl border border-zinc-700 bg-zinc-900 p-1 shadow-xl">
          {busca.isFetching && (
            <li className="flex items-center gap-2 px-3 py-2 text-sm text-zinc-400"><Loader2 className="size-4 animate-spin" aria-hidden />Buscando…</li>
          )}
          {!busca.isFetching && busca.data?.length === 0 && (
            <li className="px-3 py-2 text-sm text-zinc-400">Nenhum comandante encontrado. O nome digitado será usado.</li>
          )}
          {busca.data?.map((carta) => (
            <li key={carta.id} role="option" aria-selected={valor?.idScryfall === carta.id}>
              <button type="button" onMouseDown={(e) => e.preventDefault()} onClick={() => escolher(carta)}
                className="flex w-full items-center gap-3 rounded-lg p-1.5 text-left hover:bg-zinc-800">
                {carta.urlArte
                  ? <img src={carta.urlArte} alt="" loading="lazy" className="h-9 w-12 shrink-0 rounded object-cover" />
                  : <span className="h-9 w-12 shrink-0 rounded bg-zinc-800" />}
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-medium text-zinc-100">{carta.nome}</span>
                  <span className="block truncate text-xs text-zinc-400">{carta.tipo}</span>
                </span>
                <CoresMana identidadeCor={carta.identidadeCor} />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
