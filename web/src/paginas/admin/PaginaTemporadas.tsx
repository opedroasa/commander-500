import { ArrowDown, ArrowUp, CalendarClock, Crown, Flag, Lock, Minus, Pencil, Plus, Settings, Trash2, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import {
  useCriarMesaFinal, useDecks, useDefinirProximaEtapa, useEncerrarTemporada, useExcluirTemporada, useFinalistasSugeridos, useJogadores,
  useSalvarConfiguracaoTemporada, useSalvarTemporada, useTemporadas,
} from '../../api/consultas'
import type { ConfiguracaoTemporada, CriterioDesempate, FinalistaSugerido, ModoPontuacao, Temporada } from '../../api/tipos'
import { CabecalhoPagina, CaixaErro, Campo, Carregando, ErroEmLinha, Janela, LegendaSiglas, Vazio } from '../../componentes/comum'
import {
  deCampoDataHora, explicacaoModoPontuacao, formatarData, formatarDiaEHora, hojeIso, paraCampoDataHora, rotuloCriterioDesempate,
  rotuloModoPontuacao,
} from '../../utilitarios/formatacao'

/** Temporadas: criar, editar, encerrar, excluir, configurar a pontuação, marcar a próxima etapa e formar a mesa final. */
export function PaginaTemporadas() {
  const temporadas = useTemporadas()
  const encerrar = useEncerrarTemporada()
  const excluir = useExcluirTemporada()
  const [emEdicao, definirEmEdicao] = useState<Temporada | 'nova' | null>(null)
  const [emConfiguracao, definirEmConfiguracao] = useState<Temporada | null>(null)
  const [emProximaEtapa, definirEmProximaEtapa] = useState<Temporada | null>(null)
  const [emMesaFinal, definirEmMesaFinal] = useState<Temporada | null>(null)

  if (temporadas.isPending) return <Carregando />
  if (temporadas.error) return <CaixaErro erro={temporadas.error} aoTentarDeNovo={() => temporadas.refetch()} />

  async function confirmarEncerramento(t: Temporada) {
    if (confirm(`Encerrar "${t.nome}"? Ela deixa de estar ativa e não recebe novas rodadas.`)) await encerrar.mutateAsync(t.id)
  }
  async function confirmarExclusao(t: Temporada) {
    if (confirm(`Excluir "${t.nome}"?`)) await excluir.mutateAsync(t.id)
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Temporadas"
        acoes={<button className="botao-primario" onClick={() => definirEmEdicao('nova')}><Plus className="size-4" aria-hidden />Nova temporada</button>}
      />
      <ErroEmLinha erro={encerrar.error ?? excluir.error} />
      {temporadas.data.length === 0 && <Vazio>Nenhuma temporada.</Vazio>}
      <ul className="space-y-2">
        {temporadas.data.map((t) => {
          const c = t.configuracao
          const porColocacao = c.modoPontuacao === 'ColocacaoNaEtapa'
          return (
            <li key={t.id} className="cartao p-4">
              <div className="flex flex-wrap items-start gap-3">
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-display text-lg font-bold">{t.nome}</span>
                    {t.ativa
                      ? <span className="rounded-full bg-emerald-950 px-2 py-0.5 text-xs text-emerald-300">ativa</span>
                      : <span className="rounded-full bg-zinc-800 px-2 py-0.5 text-xs text-zinc-400">encerrada</span>}
                    {t.configuracaoTravada && <span className="inline-flex items-center gap-1 text-xs text-zinc-400"><Lock className="size-3" aria-hidden />pontuação travada</span>}
                  </div>
                  <div className="text-sm text-zinc-400">
                    {formatarData(t.dataInicio)}{t.dataFim ? ` – ${formatarData(t.dataFim)}` : ''} · {t.quantidadeEtapas} etapa{t.quantidadeEtapas === 1 ? '' : 's'} · {t.quantidadeRodadas} rodada{t.quantidadeRodadas === 1 ? '' : 's'}
                    {t.idEtapaFinal && ' · mesa final formada'}
                  </div>
                  <div className="mt-1 text-xs text-zinc-500">
                    {rotuloModoPontuacao[c.modoPontuacao]}
                    {porColocacao && ` (${c.pontosPorColocacao.join('-')}, demais ${c.pontosDemaisColocados})`}
                    {' · '}rodada: V {c.pontosVitoria} · E {c.pontosEmpate} · D {c.pontosDerrota} · Bye {c.pontosBye1}/{c.pontosBye2}/{c.pontosBye3} · {c.minutosPadraoRodada} min
                  </div>
                  {t.proximaEtapaEm && (
                    <div className="mt-1 text-xs text-amber-300">
                      Próxima etapa: {formatarDiaEHora(t.proximaEtapaEm)}{t.prazoListas && ` · listas até ${formatarData(t.prazoListas)}`}
                    </div>
                  )}
                </div>
                <div className="flex flex-wrap gap-1">
                  <button className="botao-secundario botao-pequeno" onClick={() => definirEmConfiguracao(t)}><Settings className="size-4" aria-hidden />Pontuação</button>
                  {t.ativa && (
                    <button className="botao-secundario botao-pequeno" onClick={() => definirEmProximaEtapa(t)}><CalendarClock className="size-4" aria-hidden />Próxima etapa</button>
                  )}
                  {t.ativa && porColocacao && !t.idEtapaFinal && (
                    <button className="botao-secundario botao-pequeno" onClick={() => definirEmMesaFinal(t)}><Crown className="size-4" aria-hidden />Mesa final</button>
                  )}
                  <button className="botao-discreto botao-pequeno" onClick={() => definirEmEdicao(t)} aria-label="Editar"><Pencil className="size-4" /></button>
                  {t.ativa && <button className="botao-discreto botao-pequeno" onClick={() => confirmarEncerramento(t)}><Flag className="size-4" aria-hidden />Encerrar</button>}
                  {t.quantidadeRodadas === 0 && (
                    <button className="botao-discreto botao-pequeno text-red-300" onClick={() => confirmarExclusao(t)} aria-label="Excluir"><Trash2 className="size-4" /></button>
                  )}
                </div>
              </div>
            </li>
          )
        })}
      </ul>
      {temporadas.data.length > 0 && (
        <LegendaSiglas className="mt-3" itens={[
          ['5-4-3-2', 'pontos do 1º, 2º, 3º e 4º da etapa'], ['demais', 'pontos de quem fica abaixo da tabela'],
          ['V', 'vitória'], ['E', 'empate (cada jogador)'], ['D', 'derrota (presença)'],
          ['Bye 3/3/3', 'pontos de bye com 1, 2 ou 3 jogadores de fora'], ['min', 'minutos do timer'],
        ]} />
      )}
      <JanelaTemporada key={`temporada-${emEdicao === 'nova' ? 'nova' : emEdicao?.id ?? 'nenhuma'}`} temporada={emEdicao} aoFechar={() => definirEmEdicao(null)} />
      <JanelaConfiguracao key={`configuracao-${emConfiguracao?.id ?? 'nenhuma'}`} temporada={emConfiguracao} aoFechar={() => definirEmConfiguracao(null)} />
      <JanelaProximaEtapa key={`proxima-${emProximaEtapa?.id ?? 'nenhuma'}`} temporada={emProximaEtapa} aoFechar={() => definirEmProximaEtapa(null)} />
      {emMesaFinal && <JanelaMesaFinal temporada={emMesaFinal} aoFechar={() => definirEmMesaFinal(null)} />}
    </>
  )
}

function JanelaTemporada({ temporada, aoFechar }: { temporada: Temporada | 'nova' | null; aoFechar: () => void }) {
  const salvar = useSalvarTemporada()
  const existente = temporada && temporada !== 'nova' ? temporada : null
  const [nome, definirNome] = useState(existente?.nome ?? '')
  const [dataInicio, definirDataInicio] = useState(existente?.dataInicio ?? hojeIso())
  const [dataFim, definirDataFim] = useState(existente?.dataFim ?? '')
  const [ativa, definirAtiva] = useState(existente?.ativa ?? true)

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    await salvar.mutateAsync({ id: existente?.id, dados: { nome, dataInicio, dataFim: dataFim || null, ativa } })
    aoFechar()
  }

  return (
    <Janela aberta={temporada !== null} aoFechar={aoFechar} titulo={existente ? 'Editar temporada' : 'Nova temporada'}>
      <form onSubmit={enviar} className="space-y-4">
        <Campo rotulo="Nome"><input required value={nome} onChange={(e) => definirNome(e.target.value)} className="w-full" autoFocus /></Campo>
        <div className="grid grid-cols-2 gap-3">
          <Campo rotulo="Início"><input type="date" required value={dataInicio} onChange={(e) => definirDataInicio(e.target.value)} className="w-full" /></Campo>
          <Campo rotulo="Fim (opcional)"><input type="date" value={dataFim} onChange={(e) => definirDataFim(e.target.value)} className="w-full" /></Campo>
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={ativa} onChange={(e) => definirAtiva(e.target.checked)} /> Ativa (aceita novas rodadas)
        </label>
        {!existente && (
          <p className="text-xs text-zinc-500">
            Começa no modo “colocação na etapa” com a tabela do tratado (5-4-3-2, demais 1). Ajuste em “Pontuação” antes de encerrar a primeira rodada.
          </p>
        )}
        <ErroEmLinha erro={salvar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={salvar.isPending}>Salvar</button>
        </div>
      </form>
    </Janela>
  )
}

const todosCriterios = Object.keys(rotuloCriterioDesempate) as CriterioDesempate[]
const modos: ModoPontuacao[] = ['ColocacaoNaEtapa', 'SomaDasRodadas']

function JanelaConfiguracao({ temporada, aoFechar }: { temporada: Temporada | null; aoFechar: () => void }) {
  const salvar = useSalvarConfiguracaoTemporada()
  const [configuracao, definirConfiguracao] = useState<ConfiguracaoTemporada | null>(temporada?.configuracao ?? null)
  if (!temporada || !configuracao) return <Janela aberta={false} aoFechar={aoFechar} titulo="">{null}</Janela>
  const travada = temporada.configuracaoTravada
  const porColocacao = configuracao.modoPontuacao === 'ColocacaoNaEtapa'

  const campoNumero = (chave: keyof ConfiguracaoTemporada, rotulo: string, dica?: string, minimo = 0, maximo = 100) => (
    <Campo rotulo={rotulo} dica={dica}>
      <input type="number" min={minimo} max={maximo} required disabled={travada} value={configuracao[chave] as number}
        onChange={(e) => definirConfiguracao({ ...configuracao, [chave]: Number(e.target.value) })} className="w-full" />
    </Campo>
  )

  function mover(indice: number, deslocamento: number) {
    const lista = [...configuracao!.criteriosDesempate]
    const destino = indice + deslocamento
    if (destino < 0 || destino >= lista.length) return
    ;[lista[indice], lista[destino]] = [lista[destino], lista[indice]]
    definirConfiguracao({ ...configuracao!, criteriosDesempate: lista })
  }

  function alterarColocacao(indice: number, valor: number) {
    const tabela = [...configuracao!.pontosPorColocacao]
    tabela[indice] = valor
    definirConfiguracao({ ...configuracao!, pontosPorColocacao: tabela })
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    await salvar.mutateAsync({ id: temporada!.id, configuracao: configuracao! })
    aoFechar()
  }

  const disponiveis = todosCriterios.filter((c) => !configuracao.criteriosDesempate.includes(c))
  const tabela = configuracao.pontosPorColocacao

  return (
    <Janela aberta aoFechar={aoFechar} titulo={`Pontuação · ${temporada.nome}`} larga>
      <form onSubmit={enviar} className="space-y-5">
        {travada && (
          <p className="flex items-start gap-2 rounded-lg border border-zinc-700 bg-zinc-800/60 p-2 text-sm text-zinc-300">
            <Lock className="mt-0.5 size-4 shrink-0" aria-hidden /> Travada: a primeira rodada já foi encerrada.
          </p>
        )}

        <fieldset>
          <legend className="rotulo">Como a temporada pontua</legend>
          <div className="grid gap-2 sm:grid-cols-2">
            {modos.map((modo) => (
              <label key={modo}
                className={`cursor-pointer rounded-lg border p-3 text-sm ${configuracao.modoPontuacao === modo ? 'border-amber-500 bg-amber-500/10' : 'border-zinc-700'} ${travada ? 'cursor-default opacity-80' : ''}`}>
                <span className="flex items-center gap-2 font-semibold">
                  <input type="radio" name="modo" disabled={travada} checked={configuracao.modoPontuacao === modo}
                    onChange={() => definirConfiguracao({ ...configuracao, modoPontuacao: modo })} />
                  {rotuloModoPontuacao[modo]}{modo === 'ColocacaoNaEtapa' && <span className="text-xs font-normal text-zinc-400">(padrão)</span>}
                </span>
                <span className="mt-1 block text-xs text-zinc-400">{explicacaoModoPontuacao[modo]}</span>
              </label>
            ))}
          </div>
        </fieldset>

        <div>
          <span className="rotulo">Pontos de cada rodada {porColocacao && '(definem a colocação na etapa)'}</span>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {campoNumero('pontosVitoria', 'Vitória')}
            {campoNumero('pontosEmpate', 'Empate (cada)')}
            {campoNumero('pontosDerrota', 'Derrota (presença)', 'Quem jogou e perdeu. Quem faltou fica com 0.')}
            {campoNumero('pontosBye1', 'Bye · 1 de fora')}
            {campoNumero('pontosBye2', 'Bye · 2 de fora')}
            {campoNumero('pontosBye3', 'Bye · 3 ou mais')}
          </div>
        </div>

        {porColocacao && (
          <div>
            <span className="rotulo">Pontos na temporada pela colocação na etapa</span>
            <div className="flex flex-wrap items-end gap-2">
              {tabela.map((pontos, indice) => (
                <label key={indice} className="block w-16">
                  <span className="block text-center text-xs text-zinc-400">{indice + 1}º</span>
                  <input type="number" min={0} max={100} required disabled={travada} value={pontos}
                    onChange={(e) => alterarColocacao(indice, Number(e.target.value))} className="w-full text-center" />
                </label>
              ))}
              <label className="block w-20">
                <span className="block text-center text-xs text-zinc-400">{tabela.length + 1}º em diante</span>
                <input type="number" min={0} max={100} required disabled={travada} value={configuracao.pontosDemaisColocados}
                  onChange={(e) => definirConfiguracao({ ...configuracao, pontosDemaisColocados: Number(e.target.value) })} className="w-full text-center" />
              </label>
              {!travada && (
                <div className="flex gap-1 pb-1">
                  <button type="button" className="botao-discreto botao-pequeno px-1.5" aria-label="Mais uma posição" disabled={tabela.length >= 16}
                    onClick={() => definirConfiguracao({ ...configuracao, pontosPorColocacao: [...tabela, configuracao.pontosDemaisColocados] })}>
                    <Plus className="size-3.5" />
                  </button>
                  <button type="button" className="botao-discreto botao-pequeno px-1.5" aria-label="Menos uma posição" disabled={tabela.length <= 1}
                    onClick={() => definirConfiguracao({ ...configuracao, pontosPorColocacao: tabela.slice(0, -1) })}>
                    <Minus className="size-3.5" />
                  </button>
                </div>
              )}
            </div>
            <p className="mt-2 text-xs text-zinc-500">
              Só etapas concluídas (todas as rodadas encerradas) pontuam. Empatados na etapa ganham os pontos da melhor posição.
              Desempate da temporada (tratado, Art. 6º): mais 1º lugares → mais etapas disputadas → melhor colocação na última etapa
              em que os empatados jogaram juntos → persistindo, ficam empatados (a loja decide).
            </p>
          </div>
        )}

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {campoNumero('minutosPadraoRodada', 'Timer padrão (min)', undefined, 1, 600)}
          {campoNumero('turnosExtrasAposTempo', 'Turnos após o tempo', 'Só exibido no timer.', 0, 20)}
          {campoNumero('minimoPartidasPercentualComandante', 'Mín. partidas p/ % cmd.', 'Mínimo de partidas para mostrar o % de vitória de um comandante.', 1, 100)}
        </div>

        {!porColocacao && (
          <div>
            <span className="rotulo">Desempate (depois dos pontos)</span>
            <ol className="space-y-1">
              {configuracao.criteriosDesempate.map((criterio, indice) => (
                <li key={criterio} className="flex items-center gap-2 rounded-lg border border-zinc-800 bg-zinc-900 px-2 py-1.5 text-sm">
                  <span className="w-4 text-zinc-500">{indice + 1}.</span>
                  <span className="flex-1">{rotuloCriterioDesempate[criterio]}</span>
                  {!travada && (
                    <>
                      <button type="button" className="botao-discreto botao-pequeno px-1.5" onClick={() => mover(indice, -1)} aria-label="Subir"><ArrowUp className="size-3.5" /></button>
                      <button type="button" className="botao-discreto botao-pequeno px-1.5" onClick={() => mover(indice, 1)} aria-label="Descer"><ArrowDown className="size-3.5" /></button>
                      <button type="button" className="botao-discreto botao-pequeno px-1.5"
                        onClick={() => definirConfiguracao({ ...configuracao, criteriosDesempate: configuracao.criteriosDesempate.filter((c) => c !== criterio) })}
                        aria-label="Remover"><X className="size-3.5" /></button>
                    </>
                  )}
                </li>
              ))}
              <li className="px-2 text-xs text-zinc-500">{configuracao.criteriosDesempate.length + 1}. Nome (sempre por último)</li>
            </ol>
            {!travada && disponiveis.length > 0 && (
              <select className="mt-2 w-full" value=""
                onChange={(e) => e.target.value && definirConfiguracao({ ...configuracao, criteriosDesempate: [...configuracao.criteriosDesempate, e.target.value as CriterioDesempate] })}>
                <option value="">+ Adicionar critério…</option>
                {disponiveis.map((c) => <option key={c} value={c}>{rotuloCriterioDesempate[c]}</option>)}
              </select>
            )}
          </div>
        )}

        <ErroEmLinha erro={salvar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>{travada ? 'Fechar' : 'Cancelar'}</button>
          {!travada && <button className="botao-primario" disabled={salvar.isPending}>Salvar</button>}
        </div>
      </form>
    </Janela>
  )
}

/** Data/hora da próxima etapa e prazo das listas: aparecem no banner da página inicial. */
function JanelaProximaEtapa({ temporada, aoFechar }: { temporada: Temporada | null; aoFechar: () => void }) {
  const salvar = useDefinirProximaEtapa()
  const [quando, definirQuando] = useState(paraCampoDataHora(temporada?.proximaEtapaEm))
  const [prazo, definirPrazo] = useState(temporada?.prazoListas ?? '')

  async function gravar(proximaEtapaEm: string | null, prazoListas: string | null) {
    await salvar.mutateAsync({ id: temporada!.id, proximaEtapaEm, prazoListas })
    aoFechar()
  }

  return (
    <Janela aberta={temporada !== null} aoFechar={aoFechar} titulo={`Próxima etapa · ${temporada?.nome ?? ''}`}>
      <form onSubmit={(e) => { e.preventDefault(); void gravar(deCampoDataHora(quando), prazo || null) }} className="space-y-4">
        <Campo rotulo="Data e hora da etapa">
          <input type="datetime-local" required value={quando} onChange={(e) => definirQuando(e.target.value)} className="w-full" />
        </Campo>
        <Campo rotulo="Prazo para enviar as listas (opcional)">
          <input type="date" value={prazo} onChange={(e) => definirPrazo(e.target.value)} className="w-full" />
        </Campo>
        <p className="text-xs text-zinc-500">Aparece na página inicial, junto com a temporada.</p>
        <ErroEmLinha erro={salvar.error} />
        <div className="flex flex-wrap justify-end gap-2">
          {temporada?.proximaEtapaEm && (
            <button type="button" className="botao-discreto mr-auto" disabled={salvar.isPending} onClick={() => void gravar(null, null)}>Tirar aviso</button>
          )}
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={salvar.isPending}>Salvar</button>
        </div>
      </form>
    </Janela>
  )
}

/** Mesa final do top 4: confere os finalistas (sugeridos pelo ranking), escolhe data e quantidade de rodadas. */
function JanelaMesaFinal({ temporada, aoFechar }: { temporada: Temporada; aoFechar: () => void }) {
  const sugeridos = useFinalistasSugeridos(temporada.id, 4, true)
  return (
    <Janela aberta aoFechar={aoFechar} titulo={`Mesa final · ${temporada.nome}`} larga>
      {sugeridos.isPending && <Carregando />}
      {sugeridos.error && <CaixaErro erro={sugeridos.error} aoTentarDeNovo={() => sugeridos.refetch()} />}
      {sugeridos.data && <FormularioMesaFinal temporada={temporada} sugeridos={sugeridos.data} aoFechar={aoFechar} />}
    </Janela>
  )
}

function FormularioMesaFinal({ temporada, sugeridos, aoFechar }: { temporada: Temporada; sugeridos: FinalistaSugerido[]; aoFechar: () => void }) {
  const navegar = useNavigate()
  const criar = useCriarMesaFinal()
  const jogadores = useJogadores()
  const decks = useDecks()
  const [data, definirData] = useState(hojeIso())
  const [quantidadeRodadas, definirQuantidadeRodadas] = useState(3)
  const [finalistas, definirFinalistas] = useState(sugeridos.map((s) => ({ idJogador: s.idJogador, idDeck: s.idDeck })))

  const decksDo = (idJogador: number) => (decks.data ?? []).filter((d) => d.idJogador === idJogador)
  const empateNoCorte = sugeridos.some((s) => s.empatado && s.posicao <= 4) && sugeridos.at(-1)?.empatado

  function trocar(indice: number, idJogador: number) {
    const lista = [...finalistas]
    lista[indice] = { idJogador, idDeck: decksDo(idJogador)[0]?.id ?? 0 }
    definirFinalistas(lista)
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    const rodada = await criar.mutateAsync({ idTemporada: temporada.id, data, quantidadeRodadas, finalistas })
    navegar(`/admin/rodadas/${rodada.id}`)
  }

  const ranking = new Map(sugeridos.map((s) => [s.idJogador, s]))
  const invalido = finalistas.some((f) => !f.idJogador || !f.idDeck) || new Set(finalistas.map((f) => f.idJogador)).size !== finalistas.length

  return (
    <form onSubmit={enviar} className="space-y-4">
      <p className="text-sm text-zinc-400">
        Os 4 primeiros do ranking jogam entre si. A colocação na final soma pontos no ranking pela mesma tabela das etapas, e o
        campeão é quem terminar em 1º no total. Se alguém não puder jogar, troque pelo próximo do ranking.
      </p>
      {empateNoCorte && <p className="rounded-lg border border-amber-700 bg-amber-950/40 p-2 text-sm text-amber-200">Há empate na 4ª vaga: confira e decida quem entra.</p>}
      <ol className="space-y-2">
        {finalistas.map((finalista, indice) => {
          const sugestao = ranking.get(finalista.idJogador)
          return (
            <li key={indice} className="grid gap-2 sm:grid-cols-[2rem_1fr_1fr] sm:items-center">
              <span className="font-display text-lg font-bold text-amber-400">{indice + 1}</span>
              <select value={finalista.idJogador} onChange={(e) => trocar(indice, Number(e.target.value))} aria-label={`Finalista ${indice + 1}`}>
                {(jogadores.data ?? []).filter((j) => j.ativo || j.id === finalista.idJogador).map((j) => (
                  <option key={j.id} value={j.id}>{j.nome}{ranking.get(j.id) ? ` · ${ranking.get(j.id)!.pontos} pts` : ''}</option>
                ))}
              </select>
              <select value={finalista.idDeck} aria-label={`Deck do finalista ${indice + 1}`}
                onChange={(e) => definirFinalistas(finalistas.map((f, i) => (i === indice ? { ...f, idDeck: Number(e.target.value) } : f)))}>
                <option value={0}>Escolha o deck…</option>
                {decksDo(finalista.idJogador).map((d) => <option key={d.id} value={d.id}>{d.nome}</option>)}
              </select>
              {sugestao && <span className="text-xs text-zinc-500 sm:col-start-2">{sugestao.posicao}º no ranking · {sugestao.pontos} pts{sugestao.empatado && ' (empatado)'}</span>}
            </li>
          )
        })}
      </ol>
      <div className="grid grid-cols-2 gap-3">
        <Campo rotulo="Data da final"><input type="date" required value={data} onChange={(e) => definirData(e.target.value)} className="w-full" /></Campo>
        <Campo rotulo="Rodadas da final"><input type="number" min={1} max={10} required value={quantidadeRodadas} onChange={(e) => definirQuantidadeRodadas(Number(e.target.value))} className="w-full" /></Campo>
      </div>
      <ErroEmLinha erro={criar.error} />
      <div className="flex justify-end gap-2">
        <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
        <button className="botao-primario" disabled={criar.isPending || invalido}><Crown className="size-4" aria-hidden />Formar mesa final</button>
      </div>
    </form>
  )
}
