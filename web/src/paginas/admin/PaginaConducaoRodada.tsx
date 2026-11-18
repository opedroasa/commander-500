import { AlertTriangle, Check, ExternalLink, Flag, Handshake, Pencil, Play, Search, Shuffle, Trash2, Users } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import {
  useDecks, useDefinirParticipantes, useEncerrarRodada, useExcluirRodada, useIniciarRodada, useJogadores,
  useRegistrarResultadoMesa, useRodada, useSortearRodada,
} from '../../api/consultas'
import type { Deck, Mesa, Rodada } from '../../api/tipos'
import { CaixaErro, Carregando, ErroEmLinha, SeloSituacaoDeck, SeloSituacaoRodada, Vazio } from '../../componentes/comum'
import { Cronometro } from '../../componentes/Cronometro'
import { DesenhoMesa } from '../../componentes/DesenhoMesa'
import { ListaByes } from '../../componentes/QuadroRodada'
import { chaveBusca, formatarData, formatarDataHora, nomeComApelido, rotuloSituacaoDeck } from '../../utilitarios/formatacao'

const passos = [
  { situacao: 'Rascunho', rotulo: 'Presentes' },
  { situacao: 'Sorteada', rotulo: 'Sorteio' },
  { situacao: 'EmAndamento', rotulo: 'Jogo' },
  { situacao: 'Encerrada', rotulo: 'Encerrada' },
] as const

/** Condução da rodada pelo lojista (spec 6.4): presentes → sorteio → timer → resultados → encerrar. */
export function PaginaConducaoRodada() {
  const id = Number(useParams().id)
  const rodada = useRodada(id)
  const navegar = useNavigate()
  const excluir = useExcluirRodada()
  const [editandoPresentes, definirEditandoPresentes] = useState(false)

  if (rodada.isPending) return <Carregando />
  if (rodada.error) return <CaixaErro erro={rodada.error} aoTentarDeNovo={() => rodada.refetch()} />
  const r = rodada.data
  const passoAtual = passos.findIndex((p) => p.situacao === r.situacao)
  const podeExcluir = r.situacao === 'Rascunho' || r.situacao === 'Sorteada'

  async function confirmarExclusao() {
    if (!confirm(`Excluir a rodada ${r.numeroNaEtapa} da etapa ${r.numeroEtapa}? Os presentes e o sorteio serão perdidos.`)) return
    await excluir.mutateAsync(r.id)
    navegar('/admin')
  }

  return (
    <>
      <div className="mb-4 flex flex-wrap items-center gap-x-3 gap-y-2">
        <h1 className="font-display text-2xl font-bold text-amber-100">Etapa {r.numeroEtapa} · Rodada {r.numeroNaEtapa}</h1>
        <SeloSituacaoRodada situacao={r.situacao} />
        <span className="text-sm text-zinc-400">{r.nomeTemporada} · {formatarData(r.data)}</span>
        <div className="ml-auto flex gap-2">
          <Link to={`/rodadas/${r.id}`} target="_blank" className="botao-secundario botao-pequeno">
            <ExternalLink className="size-4" aria-hidden /> Tela pública
          </Link>
          {podeExcluir && (
            <button className="botao-perigo botao-pequeno" onClick={confirmarExclusao} disabled={excluir.isPending}>
              <Trash2 className="size-4" aria-hidden /> Excluir
            </button>
          )}
        </div>
      </div>
      <ErroEmLinha erro={excluir.error} />

      <ol className="mb-6 grid grid-cols-4 gap-1 text-center text-xs">
        {passos.map((passo, indice) => (
          <li
            key={passo.situacao}
            className={`rounded-lg px-1 py-2 font-medium ${
              indice < passoAtual ? 'bg-zinc-800 text-zinc-300' : indice === passoAtual ? 'bg-amber-500 text-zinc-950' : 'bg-zinc-900 text-zinc-500'
            }`}
          >
            {indice < passoAtual && <Check className="mr-1 inline size-3" aria-hidden />}{passo.rotulo}
          </li>
        ))}
      </ol>

      {r.situacao === 'Rascunho' && <EditorPresentes rodada={r} aoConcluir={() => definirEditandoPresentes(false)} />}

      {r.situacao === 'Sorteada' && (editandoPresentes
        ? <EditorPresentes rodada={r} aoConcluir={() => definirEditandoPresentes(false)} aoCancelar={() => definirEditandoPresentes(false)} />
        : <PassoSorteio rodada={r} aoEditarPresentes={() => definirEditandoPresentes(true)} />)}

      {(r.situacao === 'EmAndamento' || r.situacao === 'Encerrada') && <PassoJogo rodada={r} />}
    </>
  )
}

// ---------------------------------------------------------------- Passo 1: presentes + decks

function EditorPresentes({ rodada, aoConcluir, aoCancelar }: { rodada: Rodada; aoConcluir: () => void; aoCancelar?: () => void }) {
  const jogadores = useJogadores()
  const decks = useDecks()
  const definirParticipantes = useDefinirParticipantes()
  const sortear = useSortearRodada()
  const [filtro, definirFiltro] = useState('')
  // Jogador presente → deck escolhido
  const [presentes, definirPresentes] = useState<Map<number, number>>(
    () => new Map(rodada.participacoes.map((p) => [p.idJogador, p.idDeck])),
  )

  const decksPorJogador = useMemo(() => {
    const mapa = new Map<number, Deck[]>()
    for (const d of decks.data ?? []) mapa.set(d.idJogador, [...(mapa.get(d.idJogador) ?? []), d])
    return mapa
  }, [decks.data])

  if (jogadores.isPending || decks.isPending) return <Carregando />
  if (jogadores.error) return <CaixaErro erro={jogadores.error} />
  if (decks.error) return <CaixaErro erro={decks.error} />

  const busca = chaveBusca(filtro)
  const lista = jogadores.data
    .filter((j) => j.ativo || presentes.has(j.id))
    .filter((j) => !busca || chaveBusca(`${j.nome} ${j.apelido ?? ''}`).includes(busca))

  const quantidade = presentes.size
  const mesas = Math.floor(quantidade / 4)
  const byes = quantidade % 4

  function marcarOuDesmarcar(idJogador: number) {
    const novos = new Map(presentes)
    if (novos.has(idJogador)) novos.delete(idJogador)
    else {
      const decksDoJogador = decksPorJogador.get(idJogador) ?? []
      const preferido = decksDoJogador.find((d) => d.situacao === 'Aprovado') ?? decksDoJogador[0]
      if (!preferido) return
      novos.set(idJogador, preferido.id)
    }
    definirPresentes(novos)
  }

  function escolherDeck(idJogador: number, idDeck: number) {
    definirPresentes(new Map(presentes).set(idJogador, idDeck))
  }

  async function salvar(eSortear: boolean) {
    const participantes = [...presentes].map(([idJogador, idDeck]) => ({ idJogador, idDeck }))
    await definirParticipantes.mutateAsync({ idRodada: rodada.id, participantes })
    if (eSortear) await sortear.mutateAsync(rodada.id)
    aoConcluir()
  }

  const ocupado = definirParticipantes.isPending || sortear.isPending

  return (
    <section>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <h2 className="flex items-center gap-2 font-semibold"><Users className="size-4 text-amber-400" aria-hidden />Quem está presente?</h2>
        {rodada.situacao === 'Sorteada' && (
          <p className="w-full rounded-lg border border-amber-900 bg-amber-950/40 p-2 text-xs text-amber-200">
            Salvar alterações desfaz o sorteio atual — será preciso sortear de novo.
          </p>
        )}
        {rodada.situacao === 'Rascunho' && rodada.numeroNaEtapa > 1 && (
          <p className="w-full rounded-lg border border-sky-900 bg-sky-950/40 p-2 text-xs text-sky-200">
            Presentes e decks copiados da rodada anterior desta etapa. Desmarque quem foi embora — os resultados
            dele nas rodadas anteriores continuam valendo para a etapa. O sorteio evita repetir adversários e cadeiras.
          </p>
        )}
      </div>
      <div className="relative mb-3">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
        <input type="search" placeholder="Filtrar jogadores" value={filtro} onChange={(e) => definirFiltro(e.target.value)} className="w-full pl-9" />
      </div>

      {lista.length === 0 && <Vazio>Nenhum jogador encontrado. <Link className="text-amber-300 hover:underline" to="/admin/jogadores">Cadastrar jogadores</Link></Vazio>}

      <ul className="space-y-1.5 pb-28">
        {lista.map((j) => {
          const decksDoJogador = decksPorJogador.get(j.id) ?? []
          const idDeck = presentes.get(j.id)
          const deck = decksDoJogador.find((d) => d.id === idDeck)
          const presente = presentes.has(j.id)
          return (
            <li key={j.id} className={`rounded-xl border p-2.5 transition ${presente ? 'border-amber-700/60 bg-amber-950/20' : 'border-zinc-800 bg-zinc-900/40'}`}>
              <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
                <label className="flex min-w-0 flex-1 cursor-pointer items-center gap-3">
                  <input type="checkbox" checked={presente} disabled={decksDoJogador.length === 0} onChange={() => marcarOuDesmarcar(j.id)} />
                  <span className="min-w-0 truncate font-medium">{nomeComApelido(j.nome, j.apelido)}</span>
                </label>
                {decksDoJogador.length === 0 ? (
                  <Link to="/admin/jogadores" className="text-xs text-zinc-500 hover:text-amber-300">sem deck — cadastrar</Link>
                ) : presente && (
                  <div className="flex w-full items-center gap-2 sm:w-auto">
                    <select value={idDeck} onChange={(e) => escolherDeck(j.id, Number(e.target.value))} className="min-w-0 flex-1 sm:w-72" aria-label={`Deck de ${j.nome}`}>
                      {decksDoJogador.map((d) => (
                        <option key={d.id} value={d.id}>
                          {d.nome} — {d.comandantes.map((c) => c.nome).join(' + ')}{d.situacao !== 'Aprovado' ? ` (${rotuloSituacaoDeck[d.situacao]})` : ''}
                        </option>
                      ))}
                    </select>
                    {deck && deck.situacao !== 'Aprovado' && <SeloSituacaoDeck situacao={deck.situacao} />}
                  </div>
                )}
              </div>
            </li>
          )
        })}
      </ul>

      {/* barra de ações fixa no rodapé */}
      <div className="fixed inset-x-0 bottom-0 z-10 border-t border-zinc-800 bg-zinc-950/95 px-4 py-3 backdrop-blur" style={{ paddingBottom: 'max(0.75rem, env(safe-area-inset-bottom))' }}>
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-2">
          <div className="mr-auto text-sm">
            <b className="text-zinc-100">{quantidade}</b> presente{quantidade === 1 ? '' : 's'}
            {quantidade >= 4
              ? <span className="text-zinc-400"> · {mesas} mesa{mesas > 1 ? 's' : ''}{byes ? ` · ${byes} bye${byes > 1 ? 's' : ''}` : ''}</span>
              : <span className="text-amber-300"> · mínimo de 4 para sortear</span>}
            <ErroEmLinha erro={definirParticipantes.error ?? sortear.error} />
          </div>
          {aoCancelar && <button className="botao-discreto" onClick={aoCancelar} disabled={ocupado}>Cancelar</button>}
          <button className="botao-secundario" onClick={() => salvar(false)} disabled={ocupado}>Salvar</button>
          <button className="botao-primario" onClick={() => salvar(true)} disabled={ocupado || quantidade < 4}>
            <Shuffle className="size-4" aria-hidden /> {ocupado ? 'Salvando…' : 'Salvar e sortear'}
          </button>
        </div>
      </div>
    </section>
  )
}

// ---------------------------------------------------------------- Passo 2: sorteio

function PassoSorteio({ rodada, aoEditarPresentes }: { rodada: Rodada; aoEditarPresentes: () => void }) {
  const sortear = useSortearRodada()
  const iniciar = useIniciarRodada()
  const [minutos, definirMinutos] = useState(rodada.duracaoMinutos)
  const decksNaoAprovados = rodada.participacoes.filter((p) => !p.bye && p.situacaoDeck !== 'Aprovado')

  async function sortearDeNovo() {
    if (confirm('Sortear as mesas novamente? O sorteio atual será substituído.')) await sortear.mutateAsync(rodada.id)
  }

  return (
    <section className="space-y-5">
      <div className="cartao flex flex-wrap items-end gap-3 p-4">
        <label className="block">
          <span className="rotulo">Duração (min)</span>
          <input type="number" min={1} max={600} value={minutos} onChange={(e) => definirMinutos(Number(e.target.value))} className="w-28" />
        </label>
        <button className="botao-primario" disabled={iniciar.isPending || minutos < 1}
          onClick={() => iniciar.mutate({ idRodada: rodada.id, duracaoMinutos: minutos })}>
          <Play className="size-4" aria-hidden /> Iniciar rodada
        </button>
        <div className="ml-auto flex flex-wrap gap-2">
          <button className="botao-secundario" onClick={sortearDeNovo} disabled={sortear.isPending}>
            <Shuffle className="size-4" aria-hidden /> Sortear novamente
          </button>
          <button className="botao-secundario" onClick={aoEditarPresentes}>
            <Pencil className="size-4" aria-hidden /> Editar presentes
          </button>
        </div>
        <div className="w-full"><ErroEmLinha erro={iniciar.error ?? sortear.error} /></div>
      </div>

      {decksNaoAprovados.length > 0 && (
        <AvisoDecks nomes={decksNaoAprovados.map((p) => `${p.nomeJogador} (${rotuloSituacaoDeck[p.situacaoDeck].toLowerCase()})`)} />
      )}

      {rodada.numeroNaEtapa > 1 && (
        <p className="text-xs text-zinc-400">
          Sorteio montado para evitar adversários e cadeiras das rodadas anteriores desta etapa (quando o número de jogadores permite).
        </p>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {rodada.mesas.map((mesa) => (
          <section key={mesa.id} className="cartao p-3" aria-label={`Mesa ${mesa.numero}`}><DesenhoMesa mesa={mesa} /></section>
        ))}
      </div>
      <ListaByes rodada={rodada} />
    </section>
  )
}

function AvisoDecks({ nomes }: { nomes: string[] }) {
  return (
    <div className="flex items-start gap-2 rounded-xl border border-amber-900 bg-amber-950/30 p-3 text-sm text-amber-200">
      <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
      <span>Deck não aprovado: {nomes.join(', ')}. Não bloqueia a rodada.</span>
    </div>
  )
}

// ---------------------------------------------------------------- Passos 3/4: jogo, resultados, encerrar

function PassoJogo({ rodada }: { rodada: Rodada }) {
  const encerrar = useEncerrarRodada()
  const pendentes = rodada.mesas.filter((m) => m.resultado === 'Pendente')
  const encerrada = rodada.situacao === 'Encerrada'

  async function confirmarEncerramento() {
    if (confirm(`Encerrar a rodada ${rodada.numeroNaEtapa} da etapa ${rodada.numeroEtapa}? Os pontos entram no ranking.`)) await encerrar.mutateAsync(rodada.id)
  }

  return (
    <section className="space-y-5">
      {!encerrada && rodada.cronometro && (
        <Cronometro cronometro={rodada.cronometro} agoraServidor={rodada.agoraServidor} turnosExtras={rodada.turnosExtrasAposTempo} />
      )}

      {encerrada ? (
        <p className="rounded-xl border border-zinc-800 bg-zinc-900/60 p-3 text-sm text-zinc-300">
          Rodada encerrada em {formatarDataHora(rodada.encerradaEm)}. Resultados ainda podem ser corrigidos — o ranking é recalculado.
        </p>
      ) : (
        <p className="text-sm text-zinc-400">Toque no jogador vencedor de cada mesa e confirme, ou marque empate.</p>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {rodada.mesas.map((mesa) => <CartaoResultadoMesa key={mesa.id} rodada={rodada} mesa={mesa} />)}
      </div>
      <ListaByes rodada={rodada} />

      {!encerrada && (
        <div className="cartao flex flex-wrap items-center gap-3 p-4">
          <span className="mr-auto text-sm text-zinc-300">
            {pendentes.length === 0 ? 'Todas as mesas têm resultado.' : `Faltam ${pendentes.length} mesa${pendentes.length > 1 ? 's' : ''}: ${pendentes.map((m) => m.numero).join(', ')}.`}
          </span>
          <button className="botao-primario" onClick={confirmarEncerramento} disabled={pendentes.length > 0 || encerrar.isPending}>
            <Flag className="size-4" aria-hidden /> Encerrar rodada
          </button>
          <div className="w-full"><ErroEmLinha erro={encerrar.error} /></div>
        </div>
      )}
    </section>
  )
}

function CartaoResultadoMesa({ rodada, mesa }: { rodada: Rodada; mesa: Mesa }) {
  const registrar = useRegistrarResultadoMesa()
  const encerrada = rodada.situacao === 'Encerrada'
  const [editando, definirEditando] = useState(mesa.resultado === 'Pendente' && !encerrada)
  const [idVencedor, definirIdVencedor] = useState<number | null>(null)
  const nomeVencedor = mesa.cadeiras.find((c) => c.participacao.id === idVencedor)?.participacao.nomeJogador

  async function enviar(resultado: 'Vitoria' | 'Empate') {
    if (encerrada && !confirm('Corrigir o resultado de uma rodada encerrada? O ranking será recalculado.')) return
    await registrar.mutateAsync({ idMesa: mesa.id, resultado, idParticipacaoVencedora: resultado === 'Vitoria' ? idVencedor : null })
    definirIdVencedor(null)
    definirEditando(false)
  }

  return (
    <section className={`cartao p-3 ${editando ? 'ring-1 ring-amber-700/60' : ''}`} aria-label={`Mesa ${mesa.numero}`}>
      <DesenhoMesa
        mesa={mesa}
        aoClicarCadeira={editando ? (c) => definirIdVencedor(c.participacao.id === idVencedor ? null : c.participacao.id) : undefined}
        idParticipacaoEscolhida={editando ? idVencedor : undefined}
      />
      <div className="mt-3 flex flex-wrap items-center gap-2">
        {editando ? (
          <>
            <button className="botao-primario botao-pequeno flex-1" disabled={idVencedor == null || registrar.isPending} onClick={() => enviar('Vitoria')}>
              <Check className="size-4" aria-hidden /> {nomeVencedor ? `Vitória: ${nomeVencedor}` : 'Selecione o vencedor'}
            </button>
            <button className="botao-secundario botao-pequeno" disabled={registrar.isPending} onClick={() => enviar('Empate')}>
              <Handshake className="size-4" aria-hidden /> Empate
            </button>
            {mesa.resultado !== 'Pendente' && (
              <button className="botao-discreto botao-pequeno" onClick={() => { definirEditando(false); definirIdVencedor(null) }}>Cancelar</button>
            )}
          </>
        ) : (
          <>
            <span className="mr-auto text-sm text-zinc-300">
              {mesa.resultado === 'Empate' ? 'Empate' : mesa.resultado === 'Vitoria'
                ? <>Vitória: <b>{mesa.cadeiras.find((c) => c.participacao.id === mesa.idParticipacaoVencedora)?.participacao.nomeJogador}</b></>
                : 'Sem resultado'}
            </span>
            <button className="botao-discreto botao-pequeno" onClick={() => definirEditando(true)}>
              <Pencil className="size-3.5" aria-hidden /> {encerrada ? 'Corrigir' : 'Alterar'}
            </button>
          </>
        )}
      </div>
      {mesa.alteradaPor && !editando && (
        <p className="mt-1 text-[0.7rem] text-zinc-500">Registrado por {mesa.alteradaPor} em {formatarDataHora(mesa.alteradaEm)}</p>
      )}
      <ErroEmLinha erro={registrar.error} />
    </section>
  )
}
