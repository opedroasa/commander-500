import { ArrowLeft, CircleCheck, CircleX, Clock, Crown, ExternalLink, Medal, Skull, Swords, Trophy, type LucideIcon } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { usePaginaJogador } from '../../api/consultas'
import type { Confronto, SituacaoDeck } from '../../api/tipos'
import { ArteComandante, CoresMana } from '../../componentes/ArteComandante'
import { CaixaErro, Carregando, LegendaSiglas, Vazio } from '../../componentes/comum'
import { GraficoEvolucao } from '../../componentes/GraficoEvolucao'
import { GradeNumeros, Numero } from '../../componentes/Numeros'
import { identidadeCombinada } from '../../utilitarios/comandantes'
import { estiloSituacaoDeck, formatarData, formatarPercentual, nomeComApelido } from '../../utilitarios/formatacao'

/** Página completa do jogador: números, evolução mês a mês, temporadas, decks, rivalidades e etapas vencidas. */
export function PaginaJogador() {
  const id = Number(useParams().id)
  const pagina = usePaginaJogador(id)
  if (pagina.isPending) return <Carregando />
  if (pagina.error) return <CaixaErro erro={pagina.error} aoTentarDeNovo={() => pagina.refetch()} />
  const j = pagina.data
  const melhor = j.decks.find((d) => d.id === j.idMelhorDeck)

  return (
    <div className="space-y-8">
      <div>
        <Link to="/jogadores" className="mb-3 inline-flex items-center gap-1 text-sm text-zinc-400 hover:text-amber-300"><ArrowLeft className="size-4" aria-hidden />Jogadores</Link>
        <div className="flex flex-wrap items-center gap-4">
          {melhor && <ArteComandante comandantes={melhor.comandantes} className="h-20 w-28 shrink-0" arredondamento="rounded-xl" />}
          <div className="min-w-0">
            <h1 className="font-display text-3xl font-bold text-amber-100 break-words">{nomeComApelido(j.nome, j.apelido)}</h1>
            <p className="text-sm text-zinc-400">
              Na liga desde {formatarData(j.dataCadastro)}
              {!j.ativo && <span className="ml-2 rounded-full bg-zinc-800 px-2 py-0.5 text-xs">inativo</span>}
            </p>
            {j.titulos > 0 && (
              <p className="mt-1 inline-flex items-center gap-1.5 rounded-full bg-amber-500/15 px-3 py-1 text-sm font-semibold text-amber-200 ring-1 ring-amber-500/40">
                <Trophy className="size-4" aria-hidden />{j.titulos} título{j.titulos === 1 ? '' : 's'} de temporada
              </p>
            )}
          </div>
        </div>
      </div>

      <GradeNumeros>
        <Numero rotulo="Partidas" valor={j.geral.partidas} detalhe={`${j.geral.participacoes} presenças`} />
        <Numero rotulo="Vitórias" valor={j.geral.vitorias} destaque />
        <Numero rotulo="% de vitória" valor={formatarPercentual(j.geral.percentualVitoria)} />
        <Numero rotulo="Empates" valor={j.geral.empates} />
        <Numero rotulo="Byes" valor={j.geral.byes} />
        <Numero rotulo="Etapas vencidas" valor={j.etapasVencidas.length} />
      </GradeNumeros>

      <section className="cartao p-4"><GraficoEvolucao pontos={j.evolucao} /></section>

      {(j.nemesis || j.vitimaFavorita) && (
        <section>
          <h2 className="mb-3 flex items-center gap-2 text-lg font-bold text-zinc-50"><Swords className="size-5 text-amber-400" aria-hidden />Rivalidades</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            {j.nemesis && <CartaoRival tipo="nemesis" confronto={j.nemesis} />}
            {j.vitimaFavorita && <CartaoRival tipo="vitima" confronto={j.vitimaFavorita} />}
          </div>
          {j.confrontos.length > 0 && (
            <div className="cartao mt-3 overflow-x-auto">
              <table className="w-full text-sm tabular-nums">
                <thead className="text-xs uppercase tracking-wide text-zinc-400">
                  <tr className="border-b border-zinc-800">
                    <th className="px-3 py-2 text-left">Adversário</th>
                    <th className="px-2 py-2 text-right" title="Mesas jogadas juntos">Mesas</th>
                    <th className="px-2 py-2 text-right" title="Mesas que ele venceu">Ele venceu</th>
                    <th className="px-3 py-2 text-right" title="Mesas que o adversário venceu">Adv. venceu</th>
                  </tr>
                </thead>
                <tbody>
                  {j.confrontos.map((c) => (
                    <tr key={c.idJogador} className="border-b border-zinc-800/60 last:border-0">
                      <td className="px-3 py-2"><Link to={`/jogadores/${c.idJogador}`} className="font-medium text-zinc-100 hover:text-amber-300 hover:underline">{c.nome}</Link></td>
                      <td className="px-2 py-2 text-right">{c.mesas}</td>
                      <td className="px-2 py-2 text-right">{c.vitoriasDoJogador}</td>
                      <td className="px-3 py-2 text-right">{c.vitoriasDoAdversario}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      <section>
        <h2 className="mb-3 text-lg font-bold text-zinc-50">Temporadas</h2>
        {j.temporadas.length === 0 ? <Vazio>Ainda não jogou nenhuma temporada.</Vazio> : (
          <div className="cartao overflow-x-auto">
            <table className="w-full text-sm tabular-nums">
              <thead className="text-xs uppercase tracking-wide text-zinc-400">
                <tr className="border-b border-zinc-800">
                  <th className="px-3 py-2 text-left">Temporada</th><th className="px-2 py-2 text-right">#</th>
                  <th className="px-2 py-2 text-right">Pts</th><th className="px-2 py-2 text-right">P</th><th className="px-3 py-2 text-right">V</th>
                </tr>
              </thead>
              <tbody>
                {j.temporadas.map((t) => (
                  <tr key={t.idTemporada} className="border-b border-zinc-800/60 last:border-0">
                    <td className="px-3 py-2">
                      <Link to={`/temporadas/${t.idTemporada}`} className="font-medium text-zinc-100 hover:text-amber-300 hover:underline">{t.nome}</Link>
                      {t.campeao && <span className="ml-2 inline-flex items-center gap-1 text-xs font-semibold text-amber-300"><Crown className="size-3.5" aria-hidden />campeão</span>}
                      {t.ativa && <span className="ml-2 rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">em andamento</span>}
                    </td>
                    <td className="px-2 py-2 text-right">{t.posicao ? `${t.posicao}º` : '–'}</td>
                    <td className="px-2 py-2 text-right font-bold text-amber-300">{t.pontos}</td>
                    <td className="px-2 py-2 text-right">{t.partidas}</td>
                    <td className="px-3 py-2 text-right">{t.vitorias}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section>
        <h2 className="mb-3 text-lg font-bold text-zinc-50">Decks</h2>
        {j.decks.length === 0 ? <Vazio>Nenhum deck cadastrado.</Vazio> : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {j.decks.map((d) => (
              <li key={d.id} className={`cartao flex gap-3 p-3 ${d.id === j.idMelhorDeck ? 'ring-1 ring-amber-500/60' : ''}`}>
                <Link to={`/comandantes/${d.chave}`} className="shrink-0" title="Ver página do comandante">
                  <ArteComandante comandantes={d.comandantes} className="h-16 w-24" arredondamento="rounded-lg" />
                </Link>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-zinc-100 break-words">{d.nome}</span>
                    {d.id === j.idMelhorDeck && <span className="shrink-0 rounded-full bg-amber-500/15 px-2 py-0.5 text-[0.7rem] font-semibold text-amber-200">melhor deck</span>}
                  </div>
                  <Link to={`/comandantes/${d.chave}`} className="block text-xs leading-snug text-zinc-400 break-words hover:text-amber-300 hover:underline">
                    {d.comandantes.map((c) => c.nome).join(' + ')}
                  </Link>
                  <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-zinc-400">
                    <CoresMana identidadeCor={identidadeCombinada(d.comandantes)} />
                    <span>{d.partidas} P · {d.vitorias} V · {formatarPercentual(d.percentualVitoria)}</span>
                    <SeloLista urlLista={d.urlLista} situacao={d.situacao} />
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      {j.etapasVencidas.length > 0 && (
        <section>
          <h2 className="mb-3 flex items-center gap-2 text-lg font-bold text-zinc-50"><Medal className="size-5 text-amber-400" aria-hidden />Etapas vencidas</h2>
          <ul className="flex flex-wrap gap-2">
            {j.etapasVencidas.map((e) => (
              <li key={e.id}>
                <Link to={`/temporadas/${e.idTemporada}?aba=etapas`} className="inline-flex items-center gap-1.5 rounded-full bg-zinc-800 px-3 py-1 text-sm text-zinc-200 hover:bg-zinc-700">
                  Etapa {e.numero} · {e.nomeTemporada} · {formatarData(e.data)}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}

      <LegendaSiglas itens={[
        ['#', 'posição na temporada'], ['Pts', 'pontos'], ['P', 'partidas'], ['V', 'vitórias'],
        ['Melhor deck', 'maior % de vitória com 3 ou mais partidas'],
        ['Nêmesis', 'quem mais venceu mesas com ele junto'], ['Vítima favorita', 'de quem ele mais venceu'],
      ]} />
    </div>
  )
}

function CartaoRival({ tipo, confronto }: { tipo: 'nemesis' | 'vitima'; confronto: Confronto }) {
  const nemesis = tipo === 'nemesis'
  return (
    <Link to={`/jogadores/${confronto.idJogador}`} className="cartao flex items-center gap-3 p-4 transition hover:border-zinc-600">
      <span className={`grid size-11 shrink-0 place-items-center rounded-full ${nemesis ? 'bg-red-950 text-red-300' : 'bg-emerald-950 text-emerald-300'}`}>
        {nemesis ? <Skull className="size-5" aria-hidden /> : <Crown className="size-5" aria-hidden />}
      </span>
      <span className="min-w-0">
        <span className="block text-xs font-medium uppercase tracking-wide text-zinc-400">{nemesis ? 'Nêmesis' : 'Vítima favorita'}</span>
        <span className="block font-semibold text-zinc-100 break-words">{confronto.nome}</span>
        <span className="block text-xs text-zinc-400">
          {nemesis ? `venceu ${confronto.vitoriasDoAdversario}` : `perdeu ${confronto.vitoriasDoJogador}`} de {confronto.mesas} mesa{confronto.mesas === 1 ? '' : 's'} juntos
        </span>
      </span>
    </Link>
  )
}

/** Texto e ícone da lista conforme a revisão do lojista. */
const aparenciaLista: Record<SituacaoDeck, { rotulo: string; Icone: LucideIcon }> = {
  Aprovado: { rotulo: 'Lista aprovada', Icone: CircleCheck },
  Pendente: { rotulo: 'Lista pendente', Icone: Clock },
  Reprovado: { rotulo: 'Lista recusada', Icone: CircleX },
}

/**
 * Selo da lista do deck. Com lista: o selo inteiro é o link (abre em outra aba) e a cor mostra a revisão do lojista.
 * Sem lista: só um "Sem lista" discreto.
 */
function SeloLista({ urlLista, situacao }: { urlLista: string | null; situacao: SituacaoDeck }) {
  if (!urlLista) return <span className="text-zinc-500">Sem lista</span>
  const { rotulo, Icone } = aparenciaLista[situacao]
  return (
    <a href={urlLista} target="_blank" rel="noreferrer noopener" title={`${rotulo}. Abrir a lista em outra aba`}
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 font-medium transition hover:brightness-125 ${estiloSituacaoDeck[situacao]}`}>
      <Icone className="size-3" aria-hidden />
      {rotulo}
      <ExternalLink className="size-3 opacity-70" aria-hidden />
    </a>
  )
}
