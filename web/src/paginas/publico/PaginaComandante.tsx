import { ArrowLeft, Crown, ExternalLink, Trophy } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { usePaginaComandante } from '../../api/consultas'
import { CoresMana } from '../../componentes/ArteComandante'
import { CaixaErro, Carregando, LegendaSiglas, Vazio } from '../../componentes/comum'
import { GraficoEvolucao } from '../../componentes/GraficoEvolucao'
import { Numero } from '../../componentes/Numeros'
import { Paginacao } from '../../componentes/Paginacao'
import { usePaginacao } from '../../utilitarios/paginacao'
import { formatarPercentual } from '../../utilitarios/formatacao'

/** "12 jogadores únicos" (como no c500.fun): quantas pessoas diferentes estão por trás do número. */
const unicos = (n: number) => `${n} jogador${n === 1 ? '' : 'es'} único${n === 1 ? '' : 's'}`

/** Página do comandante (ou dupla de parceiros): carta, números, títulos, evolução mês a mês e todas as listas cadastradas. */
export function PaginaComandante() {
  const chave = useParams().chave ?? ''
  const pagina = usePaginaComandante(chave)
  const { itensDaPagina, estado } = usePaginacao(pagina.data?.listas ?? [], chave)
  if (pagina.isPending) return <Carregando />
  if (pagina.error) return <CaixaErro erro={pagina.error} aoTentarDeNovo={() => pagina.refetch()} />
  const c = pagina.data

  return (
    <div className="space-y-8">
      <div>
        <Link to="/comandantes" className="mb-3 inline-flex items-center gap-1 text-sm text-zinc-400 hover:text-amber-300"><ArrowLeft className="size-4" aria-hidden />Comandantes</Link>
        <div className="flex flex-col gap-5 sm:flex-row sm:items-start">
          {/* Carta inteira do Scryfall (uma ou duas, para parceiros/background) */}
          <div className="flex shrink-0 justify-center gap-2">
            {c.comandantes.map((cmd) => cmd.urlImagem
              ? <img key={cmd.id} src={cmd.urlImagem} alt={cmd.nome} className="w-40 rounded-[4.5%] shadow-2xl ring-1 ring-black/40 sm:w-48" />
              : <div key={cmd.id} className="grid aspect-[5/7] w-40 place-items-center rounded-xl bg-zinc-800 p-3 text-center text-sm text-zinc-300 sm:w-48">{cmd.nome}</div>)}
          </div>
          {/* Ao lado da carta (no celular, abaixo dela): nome, cores e os números, para o espaço não ficar vazio. */}
          <div className="min-w-0 flex-1 space-y-3">
            <div className="space-y-2">
              <h1 className="font-display text-3xl font-bold leading-tight text-amber-100 break-words">{c.nome}</h1>
              <div className="flex flex-wrap items-center gap-2">
                <CoresMana identidadeCor={c.identidadeCor} />
                {c.titulos.length > 0 && (
                  <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-500/15 px-3 py-1 text-sm font-semibold text-amber-200 ring-1 ring-amber-500/40">
                    <Trophy className="size-4" aria-hidden />{c.titulos.length} título{c.titulos.length === 1 ? '' : 's'} de temporada
                  </span>
                )}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
              <Numero rotulo="% de vitória" valor={formatarPercentual(c.percentualVitoria)} destaque />
              <Numero rotulo="Partidas" valor={c.partidas} detalhe={unicos(c.jogadores)} />
              <Numero rotulo="Vitórias" valor={c.vitorias} detalhe={unicos(c.jogadoresComVitoria)} />
              <Numero rotulo="Empates" valor={c.empates} />
              <Numero rotulo="Derrotas" valor={c.derrotas} />
              <Numero rotulo="Títulos" valor={c.titulos.length} detalhe={unicos(new Set(c.titulos.map((t) => t.idJogador)).size)} />
              <Numero rotulo="Etapas vencidas" valor={c.etapasVencidas} />
              <Numero rotulo="Listas" valor={c.listas.length} />
            </div>
          </div>
        </div>
      </div>

      <section className="cartao p-4"><GraficoEvolucao pontos={c.evolucao} /></section>

      {c.titulos.length > 0 && (
        <section>
          <h2 className="mb-3 flex items-center gap-2 text-lg font-bold text-zinc-50"><Crown className="size-5 text-amber-400" aria-hidden />Títulos</h2>
          <ul className="flex flex-wrap gap-2">
            {c.titulos.map((t) => (
              <li key={`${t.idTemporada}-${t.idJogador}`} className="inline-flex items-center gap-1.5 rounded-full bg-amber-500/15 px-3 py-1 text-sm text-amber-100 ring-1 ring-amber-500/40">
                <Trophy className="size-4 text-amber-300" aria-hidden />
                <Link to={`/temporadas/${t.idTemporada}`} className="hover:underline">{t.nomeTemporada}</Link> ·
                <Link to={`/jogadores/${t.idJogador}`} className="font-semibold hover:underline">{t.nomeJogador}</Link>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section>
        <h2 className="mb-3 text-lg font-bold text-zinc-50">Listas cadastradas</h2>
        {c.listas.length === 0 ? <Vazio>Nenhuma lista cadastrada.</Vazio> : (
          <>
            <div className="cartao overflow-x-auto">
              <table className="w-full text-sm tabular-nums">
                <thead className="text-xs uppercase tracking-wide text-zinc-400">
                  <tr className="border-b border-zinc-800">
                    <th className="px-3 py-2 text-left">Jogador</th><th className="px-2 py-2 text-left">Deck</th>
                    <th className="px-2 py-2 text-right">P</th><th className="px-2 py-2 text-right">V</th>
                    <th className="px-2 py-2 text-right">%V</th><th className="px-3 py-2 text-right">Lista</th>
                  </tr>
                </thead>
                <tbody>
                  {itensDaPagina.map((l) => (
                    <tr key={l.idDeck} className="border-b border-zinc-800/60 last:border-0">
                      <td className="px-3 py-2"><Link to={`/jogadores/${l.idJogador}`} className="font-medium text-zinc-100 hover:text-amber-300 hover:underline">{l.nomeJogador}</Link></td>
                      <td className="px-2 py-2 text-zinc-300 break-words">{l.nomeDeck}</td>
                      <td className="px-2 py-2 text-right">{l.partidas}</td>
                      <td className="px-2 py-2 text-right">{l.vitorias}</td>
                      <td className="px-2 py-2 text-right">{formatarPercentual(l.percentualVitoria)}</td>
                      <td className="px-3 py-2 text-right">
                        {l.urlLista
                          ? <a href={l.urlLista} target="_blank" rel="noreferrer noopener" className="inline-flex items-center gap-1 text-amber-300 hover:underline">Ver <ExternalLink className="size-3" aria-hidden /></a>
                          : <span className="text-zinc-500">sem lista</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Paginacao estado={estado} rotulo="listas" />
          </>
        )}
      </section>

      {c.temporadas.length > 0 && (
        <section>
          <h2 className="mb-3 text-lg font-bold text-zinc-50">Por temporada</h2>
          <div className="cartao overflow-x-auto">
            <table className="w-full text-sm tabular-nums">
              <thead className="text-xs uppercase tracking-wide text-zinc-400">
                <tr className="border-b border-zinc-800">
                  <th className="px-3 py-2 text-left">Temporada</th><th className="px-2 py-2 text-right">P</th>
                  <th className="px-2 py-2 text-right">V</th><th className="px-3 py-2 text-right">%V</th>
                </tr>
              </thead>
              <tbody>
                {c.temporadas.map((t) => (
                  <tr key={t.idTemporada} className="border-b border-zinc-800/60 last:border-0">
                    <td className="px-3 py-2"><Link to={`/temporadas/${t.idTemporada}?aba=comandantes`} className="font-medium text-zinc-100 hover:text-amber-300 hover:underline">{t.nome}</Link></td>
                    <td className="px-2 py-2 text-right">{t.partidas}</td>
                    <td className="px-2 py-2 text-right">{t.vitorias}</td>
                    <td className="px-3 py-2 text-right">{formatarPercentual(t.percentualVitoria)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      <LegendaSiglas itens={[
        ['P', 'partidas'], ['V', 'vitórias'], ['%V', 'vitórias ÷ partidas'],
        ['Jogadores únicos', 'quantas pessoas diferentes estão por trás do número'],
        ['Título', 'temporada encerrada em que o campeão jogou mais com esse comandante'],
        ['Etapas vencidas', 'etapas em que um jogador com esse comandante terminou em 1º'],
      ]} />
    </div>
  )
}
