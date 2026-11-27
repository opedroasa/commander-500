import { Search } from 'lucide-react'
import { useSearchParams } from 'react-router'
import { useEstatisticasComandantes, useTemporadas } from '../../api/consultas'
import { CabecalhoPagina, CaixaErro, Carregando, Vazio } from '../../componentes/comum'
import { FiltroCores } from '../../componentes/FiltroCores'
import { Paginacao } from '../../componentes/Paginacao'
import { usePaginacao } from '../../utilitarios/paginacao'
import { CartoesArteComandante, ListaComandantes } from '../../componentes/RankingComandantes'
import {
  ordenarComandantes, rotuloOrdemComandantes, temExatamenteAsCores, type CorFiltro, type OrdemComandantes,
} from '../../utilitarios/comandantes'
import { chaveBusca } from '../../utilitarios/formatacao'

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

  // Filtros por nome e cor ficam na URL (dá para mandar o link já filtrado).
  const busca = parametros.get('busca') ?? ''
  const cores = (parametros.get('cores') ?? '').split('').filter((c): c is CorFiltro => 'WUBRGC'.includes(c))
  const termo = chaveBusca(busca)
  const filtradas = ordenarComandantes(estatisticas.data ?? [], ordem)
    .filter((l) => !termo || chaveBusca(l.nome).includes(termo))
    .filter((l) => temExatamenteAsCores(l.comandantes, cores))
  const { itensDaPagina, deslocamento, estado } = usePaginacao(filtradas, `${idTemporada}|${ordem}|${termo}|${cores.join('')}`)
  const temFiltro = termo !== '' || cores.length > 0

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

      <div className="mb-5 flex flex-wrap items-center gap-3">
        <div className="relative min-w-0 flex-1 sm:max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
          <input type="search" placeholder="Buscar comandante ou parceiro" value={busca} className="w-full pl-9"
            onChange={(e) => definirFiltro('busca', e.target.value || null)} />
        </div>
        <FiltroCores marcadas={cores} aoMudar={(novas) => definirFiltro('cores', novas.join('') || null)} />
      </div>

      {estatisticas.isPending && <Carregando />}
      {estatisticas.error && <CaixaErro erro={estatisticas.error} aoTentarDeNovo={() => estatisticas.refetch()} />}
      {estatisticas.data && (
        <div className="space-y-6">
          {/* Top 4 em cartões só sem filtro (com filtro, a lista abaixo já mostra o resultado) */}
          {!temFiltro && <CartoesArteComandante linhas={estatisticas.data} ordem={ordem} />}
          {filtradas.length === 0 ? (
            <Vazio>{temFiltro ? 'Nenhum comandante com esse filtro.' : 'Nenhum dado ainda.'}</Vazio>
          ) : (
            <div className="cartao px-4 py-1">
              <ListaComandantes linhas={itensDaPagina} ordem={ordem} minimoPartidas={minimoPartidas} jaOrdenada deslocamento={deslocamento} />
            </div>
          )}
          <Paginacao estado={estado} rotulo="comandantes" />
          <p className="text-xs text-zinc-500">
            Duplas de parceiros/background contam como uma linha. Bye não conta como partida nem vitória.
            % de vitória a partir de {minimoPartidas} partidas. "Mais decks" conta os decks cadastrados na loja.
            Filtro de cor: mostra os comandantes com exatamente as cores marcadas. Toque num comandante para ver a página dele.
          </p>
        </div>
      )}
    </>
  )
}
