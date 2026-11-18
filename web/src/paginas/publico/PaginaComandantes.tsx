import { useSearchParams } from 'react-router'
import { useEstatisticasComandantes, useTemporadas } from '../../api/consultas'
import { CabecalhoPagina, CaixaErro, Carregando } from '../../componentes/comum'
import { CartoesArteComandante, ListaComandantes } from '../../componentes/RankingComandantes'
import { rotuloOrdemComandantes, type OrdemComandantes } from '../../utilitarios/comandantes'

const ordens = Object.keys(rotuloOrdemComandantes) as OrdemComandantes[]

/** Ranking público de comandantes: todas as temporadas ou uma, com ordenação. Duplas de parceiros = uma linha. */
export function PaginaComandantes() {
  const [parametros, definirParametros] = useSearchParams()
  const idTemporada = Number(parametros.get('temporada')) || null
  const ordem = (ordens.includes(parametros.get('ordem') as OrdemComandantes) ? parametros.get('ordem') : 'jogados') as OrdemComandantes
  const temporadas = useTemporadas()
  const estatisticas = useEstatisticasComandantes(idTemporada)
  const minimoPartidas = idTemporada
    ? temporadas.data?.find((t) => t.id === idTemporada)?.configuracao.minimoPartidasPercentualComandante ?? 3
    : 3

  function definirFiltro(chave: string, valor: string | null) {
    const novos = new URLSearchParams(parametros)
    if (valor) novos.set(chave, valor)
    else novos.delete(chave)
    definirParametros(novos, { replace: true })
  }

  return (
    <>
      <CabecalhoPagina titulo="Comandantes" subtitulo="O que está sendo jogado e vencendo na liga da Fantasy Store." />

      <div className="mb-5 flex flex-wrap items-center gap-2">
        <select value={idTemporada ?? ''} onChange={(e) => definirFiltro('temporada', e.target.value || null)} aria-label="Temporada" className="min-w-0">
          <option value="">Todas as temporadas</option>
          {temporadas.data?.map((t) => <option key={t.id} value={t.id}>{t.nome}</option>)}
        </select>
        <div className="flex flex-wrap gap-1.5">
          {ordens.map((o) => (
            <button key={o} onClick={() => definirFiltro('ordem', o === 'jogados' ? null : o)}
              className={`botao botao-pequeno ${ordem === o ? 'bg-brand-orange text-white' : 'border border-zinc-700 text-zinc-300 hover:bg-zinc-800'}`}>
              {rotuloOrdemComandantes[o]}
            </button>
          ))}
        </div>
      </div>

      {estatisticas.isPending && <Carregando />}
      {estatisticas.error && <CaixaErro erro={estatisticas.error} aoTentarDeNovo={() => estatisticas.refetch()} />}
      {estatisticas.data && (
        <div className="space-y-6">
          {ordem !== 'percentual' && <CartoesArteComandante linhas={estatisticas.data} ordem={ordem} />}
          <div className="cartao px-4 py-1">
            <ListaComandantes linhas={estatisticas.data} ordem={ordem} minimoPartidas={minimoPartidas} />
          </div>
          <p className="text-xs text-zinc-500">
            Duplas de parceiros/background contam como uma linha. Bye não conta como partida nem vitória.
            % de vitória a partir de {minimoPartidas} partidas. "Mais decks" conta os decks cadastrados na loja.
          </p>
        </div>
      )}
    </>
  )
}
