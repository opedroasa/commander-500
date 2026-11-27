import type { CorFiltro } from '../utilitarios/comandantes'

const cores: { cor: CorFiltro; nome: string }[] = [
  { cor: 'W', nome: 'Branco' },
  { cor: 'U', nome: 'Azul' },
  { cor: 'B', nome: 'Preto' },
  { cor: 'R', nome: 'Vermelho' },
  { cor: 'G', nome: 'Verde' },
  { cor: 'C', nome: 'Incolor' },
]

/**
 * Botões dos símbolos de mana para filtrar por identidade de cor (pode marcar várias). Os símbolos vêm do Scryfall.
 * Marcar "Incolor" desmarca as cores (e vice-versa): incolor é a ausência de cor.
 */
export function FiltroCores({ marcadas, aoMudar }: { marcadas: CorFiltro[]; aoMudar: (cores: CorFiltro[]) => void }) {
  function alternar(cor: CorFiltro) {
    if (marcadas.includes(cor)) return aoMudar(marcadas.filter((c) => c !== cor))
    if (cor === 'C') return aoMudar(['C'])
    aoMudar([...marcadas.filter((c) => c !== 'C'), cor])
  }
  return (
    <div className="flex flex-wrap items-center gap-1.5" role="group" aria-label="Identidade de cor">
      {cores.map(({ cor, nome }) => {
        const ativa = marcadas.includes(cor)
        return (
          <button key={cor} type="button" onClick={() => alternar(cor)} aria-pressed={ativa} title={nome}
            className={`rounded-full p-1 transition ${ativa ? 'bg-amber-500/25 ring-2 ring-amber-400' : 'opacity-50 hover:opacity-90'}`}>
            <img src={`https://svgs.scryfall.io/card-symbols/${cor}.svg`} alt={nome} className="size-6" />
          </button>
        )
      })}
      {marcadas.length > 0 && (
        <button type="button" className="botao-discreto botao-pequeno" onClick={() => aoMudar([])}>Limpar cores</button>
      )}
    </div>
  )
}
