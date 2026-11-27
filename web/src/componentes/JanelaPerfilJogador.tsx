import { ExternalLink, Flag, Medal, Pencil, Plus, Trophy } from 'lucide-react'
import { useState } from 'react'
import { usePerfilJogador } from '../api/consultas'
import type { Deck, EstatisticasJogador } from '../api/tipos'
import { identidadeCombinada } from '../utilitarios/comandantes'
import { formatarData, formatarPercentual } from '../utilitarios/formatacao'
import { ArteComandante, CoresMana } from './ArteComandante'
import { CaixaErro, Carregando, Janela, SeloSituacaoDeck } from './comum'
import { JanelaDeck } from './JanelaDeck'

/**
 * Mini perfil do jogador: estatísticas gerais, da temporada (quando aberto a partir de uma), conquistas e decks.
 * No modo admin mostra também a situação/observação dos decks e permite cadastrar e editar.
 */
export function JanelaPerfilJogador({ idJogador, idTemporada = null, admin = false, aoFechar }: {
  idJogador: number | null
  idTemporada?: number | null
  admin?: boolean
  aoFechar: () => void
}) {
  const perfil = usePerfilJogador(idJogador, idTemporada)
  const [deckEmEdicao, definirDeckEmEdicao] = useState<Deck | 'novo' | null>(null)
  const p = perfil.data

  return (
    <>
      {/* O perfil continua aberto embaixo da janela do deck (os <dialog> se empilham; Esc fecha o de cima). */}
      <Janela aberta={idJogador != null} aoFechar={aoFechar} titulo={p ? p.nome + (p.apelido ? ` (${p.apelido})` : '') : 'Jogador'} larga>
        {perfil.isPending && <Carregando />}
        {perfil.error && <CaixaErro erro={perfil.error} aoTentarDeNovo={() => perfil.refetch()} />}
        {p && (
          <div className="space-y-5">
            {admin && !p.ativo && <p className="rounded-lg bg-zinc-800 px-3 py-2 text-sm text-zinc-300">Jogador inativo.</p>}

            {/* Abre em outra aba para quem está vendo um ranking não perder a tela em que estava. */}
            <a href={`/jogadores/${p.id}`} target="_blank" rel="noopener" className="botao-secundario botao-pequeno w-fit">
              <ExternalLink className="size-4" aria-hidden /> Ver perfil completo
            </a>

            {p.temporada && (
              <section>
                <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-zinc-400">{p.temporada.nomeTemporada}</h3>
                <div className="mb-2 flex flex-wrap items-baseline gap-x-4 gap-y-1">
                  <span className="font-display text-3xl font-bold text-amber-100">{p.temporada.posicao ? `${p.temporada.posicao}º` : '–'}</span>
                  <span className="text-lg font-semibold text-amber-300">{p.temporada.pontos} pts</span>
                </div>
                <GradeEstatisticas estatisticas={p.temporada.estatisticas} />
              </section>
            )}

            <section>
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-zinc-400">
                Geral · {p.temporadasJogadas} temporada{p.temporadasJogadas === 1 ? '' : 's'} jogada{p.temporadasJogadas === 1 ? '' : 's'}
              </h3>
              <GradeEstatisticas estatisticas={p.geral} />
            </section>

            {(p.temporadasVencidas.length > 0 || p.etapasVencidas.length > 0) && (
              <section>
                <h3 className="mb-2 text-xs font-semibold uppercase tracking-widest text-zinc-400">Conquistas</h3>
                <ul className="flex flex-wrap gap-2">
                  {p.temporadasVencidas.map((t) => (
                    <li key={`t${t.id}`} className="inline-flex items-center gap-1.5 rounded-full bg-amber-500/15 px-3 py-1 text-sm font-medium text-amber-200 ring-1 ring-amber-500/40">
                      <Trophy className="size-4" aria-hidden /> Campeão · {t.nome}
                    </li>
                  ))}
                  {p.etapasVencidas.map((e) => (
                    <li key={`e${e.id}`} className="inline-flex items-center gap-1.5 rounded-full bg-zinc-800 px-3 py-1 text-sm text-zinc-200">
                      <Medal className="size-4 text-amber-400" aria-hidden /> Etapa {e.numero} · {formatarData(e.data)}
                    </li>
                  ))}
                </ul>
              </section>
            )}

            <section>
              <div className="mb-2 flex items-center gap-2">
                <h3 className="flex-1 text-xs font-semibold uppercase tracking-widest text-zinc-400">Decks ({p.decks.length})</h3>
                {admin && (
                  <button className="botao-primario botao-pequeno" onClick={() => definirDeckEmEdicao('novo')}>
                    <Plus className="size-4" aria-hidden /> Novo deck
                  </button>
                )}
              </div>
              {p.decks.length === 0 && <p className="text-sm text-zinc-400">Nenhum deck cadastrado.</p>}
              <ul className="grid gap-2 sm:grid-cols-2">
                {p.decks.map((d) => (
                  <li key={d.id} className="flex gap-3 rounded-xl border border-zinc-800 bg-zinc-900 p-2">
                    <ArteComandante comandantes={d.comandantes} className="h-16 w-20 shrink-0" arredondamento="rounded-lg" />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-start gap-1">
                        <span className="min-w-0 flex-1 truncate text-sm font-semibold">{d.nome}</span>
                        {admin && (
                          <button className="rounded p-0.5 text-zinc-400 hover:text-zinc-100" onClick={() => definirDeckEmEdicao(d)} aria-label={`Editar ${d.nome}`}>
                            <Pencil className="size-3.5" />
                          </button>
                        )}
                      </div>
                      <div className="truncate text-xs text-zinc-400">{d.comandantes.map((c) => c.nome).join(' + ')}</div>
                      <div className="mt-1 flex flex-wrap items-center gap-2">
                        <CoresMana identidadeCor={identidadeCombinada(d.comandantes)} />
                        {admin && <SeloSituacaoDeck situacao={d.situacao} />}
                        {d.urlLista && (
                          <a href={d.urlLista} target="_blank" rel="noreferrer noopener" className="inline-flex items-center gap-1 text-xs text-amber-300 hover:underline">
                            Lista <ExternalLink className="size-3" aria-hidden />
                          </a>
                        )}
                      </div>
                      {admin && d.observacaoRevisao && <p className="mt-1 text-xs text-zinc-500"><Flag className="mr-1 inline size-3" aria-hidden />{d.observacaoRevisao}</p>}
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          </div>
        )}
      </Janela>

      {deckEmEdicao !== null && idJogador != null && (
        <JanelaDeck
          aberta
          deck={deckEmEdicao === 'novo' ? null : deckEmEdicao}
          idJogador={idJogador}
          aoFechar={() => definirDeckEmEdicao(null)}
        />
      )}
    </>
  )
}

function GradeEstatisticas({ estatisticas }: { estatisticas: EstatisticasJogador }) {
  const itens = [
    { rotulo: 'Participações', valor: estatisticas.participacoes, dica: 'rodadas (inclui byes)' },
    { rotulo: 'Vitórias', valor: estatisticas.vitorias },
    { rotulo: 'Derrotas', valor: estatisticas.derrotas },
    { rotulo: 'Empates', valor: estatisticas.empates },
    { rotulo: 'Byes', valor: estatisticas.byes },
    { rotulo: '% Vitória', valor: formatarPercentual(estatisticas.percentualVitoria), dica: 'vitórias ÷ partidas jogadas' },
  ]
  return (
    <dl className="grid grid-cols-3 gap-2 sm:grid-cols-6">
      {itens.map((item) => (
        <div key={item.rotulo} className="flex flex-col-reverse rounded-xl border border-zinc-800 bg-zinc-900 px-2 py-2 text-center" title={item.dica}>
          <dt className="text-[0.65rem] uppercase tracking-wide text-zinc-400">{item.rotulo}</dt>
          <dd className="text-xl font-bold tabular-nums text-zinc-50">{item.valor}</dd>
        </div>
      ))}
    </dl>
  )
}
