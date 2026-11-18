import { ChevronDown, Crown, Handshake, Medal } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { useEstatisticasComandantes, useEtapa, useEtapasTemporada, useRanking, useTemporada } from '../../api/consultas'
import type { Etapa, Rodada, Temporada } from '../../api/tipos'
import { Abas, CabecalhoPagina, CaixaErro, Carregando, SeloSituacaoRodada, Vazio } from '../../componentes/comum'
import { JanelaPerfilJogador } from '../../componentes/JanelaPerfilJogador'
import { CartoesArteComandante, ListaComandantes } from '../../componentes/RankingComandantes'
import { rotuloOrdemComandantes, type OrdemComandantes } from '../../utilitarios/comandantes'
import { formatarData, formatarPercentual, rotuloCriterioDesempate } from '../../utilitarios/formatacao'

type Aba = 'ranking' | 'etapas' | 'comandantes'
const abas: Aba[] = ['ranking', 'etapas', 'comandantes']

/** Detalhe da temporada: ranking, etapas (com ranking do dia) e comandantes. */
export function PaginaTemporada() {
  const id = Number(useParams().id)
  const [parametros, definirParametros] = useSearchParams()
  const aba = (abas.includes(parametros.get('aba') as Aba) ? parametros.get('aba') : 'ranking') as Aba
  const temporada = useTemporada(id)
  const [idPerfil, definirIdPerfil] = useState<number | null>(null)

  if (temporada.isPending) return <Carregando />
  if (temporada.error) return <CaixaErro erro={temporada.error} aoTentarDeNovo={() => temporada.refetch()} />
  const t = temporada.data

  return (
    <>
      <CabecalhoPagina
        titulo={t.nome}
        subtitulo={
          <>
            {formatarData(t.dataInicio)}{t.dataFim ? ` – ${formatarData(t.dataFim)}` : ' – em aberto'}
            {t.ativa && <span className="ml-2 rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">em andamento</span>}
          </>
        }
      />
      <Abas
        valor={aba}
        aoMudar={(nova) => definirParametros({ aba: nova }, { replace: true })}
        opcoes={[
          { valor: 'ranking', rotulo: 'Ranking' },
          { valor: 'etapas', rotulo: 'Etapas' },
          { valor: 'comandantes', rotulo: 'Comandantes' },
        ]}
      />
      {aba === 'ranking' && <AbaRanking temporada={t} aoAbrirJogador={definirIdPerfil} />}
      {aba === 'etapas' && <AbaEtapas idTemporada={t.id} aoAbrirJogador={definirIdPerfil} />}
      {aba === 'comandantes' && <AbaComandantes temporada={t} />}

      <JanelaPerfilJogador idJogador={idPerfil} idTemporada={t.id} aoFechar={() => definirIdPerfil(null)} />
    </>
  )
}

/** Nome do jogador que abre o mini perfil. */
function BotaoJogador({ id, nome, aoAbrir }: { id: number; nome: string; aoAbrir: (id: number) => void }) {
  return (
    <button type="button" onClick={() => aoAbrir(id)} className="max-w-full truncate text-left font-medium text-zinc-100 underline-offset-2 hover:text-amber-300 hover:underline">
      {nome}
    </button>
  )
}

function AbaRanking({ temporada, aoAbrirJogador }: { temporada: Temporada; aoAbrirJogador: (id: number) => void }) {
  const ranking = useRanking(temporada.id)
  if (ranking.isPending) return <Carregando />
  if (ranking.error) return <CaixaErro erro={ranking.error} aoTentarDeNovo={() => ranking.refetch()} />
  const c = temporada.configuracao

  return (
    <>
      {ranking.data.length === 0 ? (
        <Vazio>Nenhuma rodada encerrada ainda. O ranking aparece depois da primeira rodada.</Vazio>
      ) : (
        <div className="cartao overflow-x-auto">
          <table className="w-full text-[13px] tabular-nums sm:text-sm">
            <thead className="text-xs uppercase tracking-wide text-zinc-400">
              <tr className="border-b border-zinc-800">
                <th className="px-2 py-2.5 text-left sm:px-3">#</th>
                <th className="px-1.5 py-2.5 text-left">Jogador</th>
                <th className="px-1.5 py-2.5 text-right" title="Pontos">Pts</th>
                <th className="px-1.5 py-2.5 text-right" title="Vitórias">V</th>
                <th className="px-1.5 py-2.5 text-right" title="Empates">E</th>
                <th className="px-1.5 py-2.5 text-right" title="Byes">B</th>
                <th className="px-1.5 py-2.5 text-right" title="Partidas jogadas">P</th>
                <th className="px-2 py-2.5 text-right sm:px-3" title="% de vitória">%V</th>
              </tr>
            </thead>
            <tbody>
              {ranking.data.map((l) => (
                <tr key={l.idJogador} className="border-b border-zinc-800/60 last:border-0">
                  <td className="px-2 py-2.5 sm:px-3"><SeloPosicao posicao={l.posicao} /></td>
                  <td className="max-w-[7.5rem] px-1.5 py-2.5 sm:max-w-none">
                    <BotaoJogador id={l.idJogador} nome={l.nomeJogador} aoAbrir={aoAbrirJogador} />
                  </td>
                  <td className="px-1.5 py-2.5 text-right text-base font-bold text-amber-300">{l.pontos}</td>
                  <td className="px-1.5 py-2.5 text-right">{l.vitorias}</td>
                  <td className="px-1.5 py-2.5 text-right">{l.empates}</td>
                  <td className="px-1.5 py-2.5 text-right">{l.byes}</td>
                  <td className="px-1.5 py-2.5 text-right">{l.partidas}</td>
                  <td className="px-2 py-2.5 text-right text-zinc-300 sm:px-3">{formatarPercentual(l.percentualVitoria)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <p className="mt-2 text-xs text-zinc-500">Toque no nome do jogador para ver o perfil.</p>
      <details className="mt-3 text-sm text-zinc-400">
        <summary className="cursor-pointer select-none text-zinc-300">Como funciona a pontuação</summary>
        <ul className="mt-2 list-disc space-y-1 pl-5">
          <li>Vitória: <b>{c.pontosVitoria}</b> pts (só o vencedor da mesa pontua).</li>
          <li>Empate / tempo esgotado: <b>{c.pontosEmpate}</b> pt para cada jogador da mesa.</li>
          <li>Bye: <b>{c.pontosBye1}</b> pts (1 de fora), <b>{c.pontosBye2}</b> (2 de fora), <b>{c.pontosBye3}</b> (3 de fora). Bye não conta como partida.</li>
          <li>Desempate: pontos → {c.criteriosDesempate.map((criterio) => rotuloCriterioDesempate[criterio].toLowerCase()).join(' → ')}{c.criteriosDesempate.length ? ' → ' : ''}nome.</li>
          <li>V = vitórias, E = empates, B = byes, P = partidas jogadas, %V = vitórias ÷ partidas.</li>
        </ul>
      </details>
    </>
  )
}

function SeloPosicao({ posicao, empatado = false }: { posicao: number; empatado?: boolean }) {
  return (
    <span
      className={`inline-flex size-6 items-center justify-center rounded-full text-xs font-bold ${
        posicao === 1 ? 'bg-brand-orange text-white'
          : posicao === 2 ? 'bg-zinc-300 text-zinc-900'
            : posicao === 3 ? 'bg-amber-800 text-amber-50' : 'text-zinc-400'
      }`}
      title={empatado ? 'Empatado' : undefined}
    >
      {posicao}{empatado && <span className="sr-only"> (empatado)</span>}
    </span>
  )
}

// ---------------------------------------------------------------- Etapas

function AbaEtapas({ idTemporada, aoAbrirJogador }: { idTemporada: number; aoAbrirJogador: (id: number) => void }) {
  const etapas = useEtapasTemporada(idTemporada)
  if (etapas.isPending) return <Carregando />
  if (etapas.error) return <CaixaErro erro={etapas.error} aoTentarDeNovo={() => etapas.refetch()} />
  if (etapas.data.length === 0) return <Vazio>Nenhuma etapa ainda.</Vazio>
  return (
    <div className="space-y-2">
      {etapas.data.map((e, indice) => <CartaoEtapa key={e.id} etapa={e} abertaInicialmente={indice === 0} aoAbrirJogador={aoAbrirJogador} />)}
      <p className="pt-2 text-xs text-zinc-500">
        Ranking da etapa: mais vitórias (bye conta como vitória) → quem teve bye fica abaixo → confronto direto entre os empatados → persistindo, dividem a posição.
      </p>
    </div>
  )
}

function CartaoEtapa({ etapa, abertaInicialmente, aoAbrirJogador }: { etapa: Etapa; abertaInicialmente: boolean; aoAbrirJogador: (id: number) => void }) {
  const [aberta, definirAberta] = useState(abertaInicialmente)
  const detalhe = useEtapa(etapa.id, aberta)

  return (
    <div className="cartao">
      <button className="flex w-full flex-wrap items-center gap-x-3 gap-y-1 p-4 text-left" onClick={() => definirAberta(!aberta)} aria-expanded={aberta}>
        <span className="font-display text-lg font-bold text-zinc-100">Etapa {etapa.numero}</span>
        <span className="text-sm text-zinc-400">{formatarData(etapa.data)} · {etapa.quantidadeRodadas} rodada{etapa.quantidadeRodadas === 1 ? '' : 's'}</span>
        <span className="ml-auto flex items-center gap-2">
          {etapa.lideres.length > 0 && (
            <span className="inline-flex items-center gap-1 text-sm text-amber-300">
              <Medal className="size-4" aria-hidden />
              {etapa.concluida ? '' : 'Líder: '}{etapa.lideres.map((l) => l.nome).join(', ')}
            </span>
          )}
          {!etapa.concluida && <span className="rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">em andamento</span>}
          <ChevronDown className={`size-4 text-zinc-500 transition ${aberta ? 'rotate-180' : ''}`} aria-hidden />
        </span>
      </button>
      {aberta && (
        <div className="space-y-4 border-t border-zinc-800 p-4">
          {detalhe.isPending && <Carregando />}
          {detalhe.error && <CaixaErro erro={detalhe.error} />}
          {detalhe.data && (
            <>
              {detalhe.data.ranking.length === 0 ? (
                <p className="text-sm text-zinc-400">Nenhuma rodada encerrada nesta etapa ainda.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-[13px] tabular-nums sm:text-sm">
                    <thead className="text-xs uppercase tracking-wide text-zinc-400">
                      <tr className="border-b border-zinc-800">
                        <th className="px-1.5 py-2 text-left">#</th>
                        <th className="px-1.5 py-2 text-left">Jogador</th>
                        <th className="px-1.5 py-2 text-right" title="Vitórias na etapa (bye conta)">V</th>
                        <th className="px-1.5 py-2 text-right" title="Byes">B</th>
                        <th className="px-1.5 py-2 text-right" title="Empates">E</th>
                        <th className="px-1.5 py-2 text-right" title="Vitórias em confronto direto entre empatados">CD</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detalhe.data.ranking.map((l) => (
                        <tr key={l.idJogador} className="border-b border-zinc-800/60 last:border-0">
                          <td className="px-1.5 py-2"><SeloPosicao posicao={l.posicao} empatado={l.empatado} /></td>
                          <td className="max-w-[9rem] px-1.5 py-2 sm:max-w-none">
                            <span className="flex items-center gap-1.5">
                              <BotaoJogador id={l.idJogador} nome={l.nomeJogador} aoAbrir={aoAbrirJogador} />
                              {l.empatado && l.vitorias > 0 && <span className="shrink-0 rounded bg-zinc-800 px-1 text-[0.6rem] uppercase text-zinc-400">empate</span>}
                            </span>
                          </td>
                          <td className="px-1.5 py-2 text-right text-base font-bold text-amber-300">{l.vitorias}</td>
                          <td className="px-1.5 py-2 text-right">{l.byes}</td>
                          <td className="px-1.5 py-2 text-right">{l.empates}</td>
                          <td className="px-1.5 py-2 text-right text-zinc-400">{l.vitoriasConfrontoDireto || '–'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              <div className="space-y-2">
                {detalhe.data.rodadas.filter((r) => r.situacao !== 'Rascunho').map((r) => <ResumoRodada key={r.id} rodada={r} aoAbrirJogador={aoAbrirJogador} />)}
              </div>
            </>
          )}
        </div>
      )}
    </div>
  )
}

function ResumoRodada({ rodada, aoAbrirJogador }: { rodada: Rodada; aoAbrirJogador: (id: number) => void }) {
  const [aberta, definirAberta] = useState(false)
  return (
    <div className="rounded-xl border border-zinc-800">
      <button className="flex w-full items-center gap-3 px-3 py-2.5 text-left" onClick={() => definirAberta(!aberta)} aria-expanded={aberta}>
        <span className="font-semibold text-zinc-100">Rodada {rodada.numeroNaEtapa}</span>
        <span className="ml-auto"><SeloSituacaoRodada situacao={rodada.situacao} /></span>
        <ChevronDown className={`size-4 text-zinc-500 transition ${aberta ? 'rotate-180' : ''}`} aria-hidden />
      </button>
      {aberta && (
        <div className="space-y-3 border-t border-zinc-800 p-3">
          {rodada.mesas.map((mesa) => (
            <div key={mesa.id}>
              <div className="mb-1 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-zinc-400">
                Mesa {mesa.numero}
                {mesa.resultado === 'Empate' && <span className="inline-flex items-center gap-1 normal-case text-zinc-300"><Handshake className="size-3.5" aria-hidden />empate</span>}
                {mesa.resultado === 'Pendente' && <span className="normal-case text-zinc-500">sem resultado</span>}
              </div>
              <ul className="grid gap-1 sm:grid-cols-2">
                {mesa.cadeiras.map((cadeira) => {
                  const venceu = mesa.resultado === 'Vitoria' && mesa.idParticipacaoVencedora === cadeira.participacao.id
                  return (
                    <li key={cadeira.numero} className={`flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm ${venceu ? 'bg-amber-500/15' : 'bg-zinc-900'}`}>
                      <span className="w-4 text-xs text-zinc-500">{cadeira.numero}</span>
                      <span className="min-w-0 flex-1 truncate">
                        <button type="button" onClick={() => aoAbrirJogador(cadeira.participacao.idJogador)} className="font-medium hover:text-amber-300 hover:underline">
                          {cadeira.participacao.nomeJogador}
                        </button>
                        <span className="text-zinc-500"> · {cadeira.participacao.comandantes}</span>
                      </span>
                      {venceu && <Crown className="size-4 shrink-0 text-amber-400" aria-label="Vencedor" />}
                    </li>
                  )
                })}
              </ul>
            </div>
          ))}
          {rodada.byes.length > 0 && <p className="text-sm text-zinc-400">Bye: {rodada.byes.map((b) => b.nomeJogador).join(', ')}</p>}
          <Link to={`/rodadas/${rodada.id}`} className="inline-block text-sm font-medium text-amber-300 hover:underline">Ver mesas desenhadas →</Link>
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------- Comandantes

function AbaComandantes({ temporada }: { temporada: Temporada }) {
  const estatisticas = useEstatisticasComandantes(temporada.id)
  const [ordem, definirOrdem] = useState<OrdemComandantes>('jogados')
  if (estatisticas.isPending) return <Carregando />
  if (estatisticas.error) return <CaixaErro erro={estatisticas.error} aoTentarDeNovo={() => estatisticas.refetch()} />
  const minimo = temporada.configuracao.minimoPartidasPercentualComandante

  if (!estatisticas.data.some((l) => l.partidas > 0)) return <Vazio>Nenhuma partida registrada nesta temporada ainda.</Vazio>

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap gap-1.5">
        {(['jogados', 'vitorias', 'percentual'] as const).map((o) => (
          <button key={o} onClick={() => definirOrdem(o)}
            className={`botao botao-pequeno ${ordem === o ? 'bg-brand-orange text-white' : 'border border-zinc-700 text-zinc-300 hover:bg-zinc-800'}`}>
            {rotuloOrdemComandantes[o]}
          </button>
        ))}
      </div>
      {ordem !== 'percentual' && <CartoesArteComandante linhas={estatisticas.data} ordem={ordem} />}
      <div className="cartao px-4 py-1"><ListaComandantes linhas={estatisticas.data} ordem={ordem} minimoPartidas={minimo} /></div>
      <p className="text-xs text-zinc-500">
        Duplas de parceiros/background aparecem como uma linha. Bye não conta como partida nem vitória. % de vitória a partir de {minimo} partidas.
      </p>
    </div>
  )
}
