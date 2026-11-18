import { ArrowDown, ArrowUp, Flag, Lock, Pencil, Plus, Settings, Trash2, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useEncerrarTemporada, useExcluirTemporada, useSalvarConfiguracaoTemporada, useSalvarTemporada, useTemporadas } from '../../api/consultas'
import type { ConfiguracaoTemporada, CriterioDesempate, Temporada } from '../../api/tipos'
import { CabecalhoPagina, CaixaErro, Campo, Carregando, ErroEmLinha, Janela, Vazio } from '../../componentes/comum'
import { formatarData, hojeIso, rotuloCriterioDesempate } from '../../utilitarios/formatacao'

/** Temporadas: criar, editar, encerrar, excluir e configurar a pontuação. */
export function PaginaTemporadas() {
  const temporadas = useTemporadas()
  const encerrar = useEncerrarTemporada()
  const excluir = useExcluirTemporada()
  const [emEdicao, definirEmEdicao] = useState<Temporada | 'nova' | null>(null)
  const [emConfiguracao, definirEmConfiguracao] = useState<Temporada | null>(null)

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
        {temporadas.data.map((t) => (
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
                </div>
                <div className="mt-1 text-xs text-zinc-500">
                  V {t.configuracao.pontosVitoria} · E {t.configuracao.pontosEmpate} · Bye {t.configuracao.pontosBye1}/{t.configuracao.pontosBye2}/{t.configuracao.pontosBye3} · {t.configuracao.minutosPadraoRodada} min
                </div>
              </div>
              <div className="flex flex-wrap gap-1">
                <button className="botao-secundario botao-pequeno" onClick={() => definirEmConfiguracao(t)}><Settings className="size-4" aria-hidden />Pontuação</button>
                <button className="botao-discreto botao-pequeno" onClick={() => definirEmEdicao(t)} aria-label="Editar"><Pencil className="size-4" /></button>
                {t.ativa && <button className="botao-discreto botao-pequeno" onClick={() => confirmarEncerramento(t)}><Flag className="size-4" aria-hidden />Encerrar</button>}
                {t.quantidadeRodadas === 0 && (
                  <button className="botao-discreto botao-pequeno text-red-300" onClick={() => confirmarExclusao(t)} aria-label="Excluir"><Trash2 className="size-4" /></button>
                )}
              </div>
            </div>
          </li>
        ))}
      </ul>
      <JanelaTemporada key={`temporada-${emEdicao === 'nova' ? 'nova' : emEdicao?.id ?? 'nenhuma'}`} temporada={emEdicao} aoFechar={() => definirEmEdicao(null)} />
      <JanelaConfiguracao key={`configuracao-${emConfiguracao?.id ?? 'nenhuma'}`} temporada={emConfiguracao} aoFechar={() => definirEmConfiguracao(null)} />
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
        {!existente && <p className="text-xs text-zinc-500">A pontuação começa com os valores padrão; ajuste em “Pontuação”.</p>}
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

function JanelaConfiguracao({ temporada, aoFechar }: { temporada: Temporada | null; aoFechar: () => void }) {
  const salvar = useSalvarConfiguracaoTemporada()
  const [configuracao, definirConfiguracao] = useState<ConfiguracaoTemporada | null>(temporada?.configuracao ?? null)
  if (!temporada || !configuracao) return <Janela aberta={false} aoFechar={aoFechar} titulo="">{null}</Janela>
  const travada = temporada.configuracaoTravada

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

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    await salvar.mutateAsync({ id: temporada!.id, configuracao: configuracao! })
    aoFechar()
  }

  const disponiveis = todosCriterios.filter((c) => !configuracao.criteriosDesempate.includes(c))

  return (
    <Janela aberta aoFechar={aoFechar} titulo={`Pontuação · ${temporada.nome}`}>
      <form onSubmit={enviar} className="space-y-4">
        {travada && (
          <p className="flex items-start gap-2 rounded-lg border border-zinc-700 bg-zinc-800/60 p-2 text-sm text-zinc-300">
            <Lock className="mt-0.5 size-4 shrink-0" aria-hidden /> Travada: a primeira rodada já foi encerrada.
          </p>
        )}
        <div className="grid grid-cols-2 gap-3">
          {campoNumero('pontosVitoria', 'Vitória')}
          {campoNumero('pontosEmpate', 'Empate (cada)')}
          {campoNumero('pontosBye1', 'Bye · 1 de fora')}
          {campoNumero('pontosBye2', 'Bye · 2 de fora')}
          {campoNumero('pontosBye3', 'Bye · 3 de fora')}
          {campoNumero('minutosPadraoRodada', 'Timer padrão (min)', undefined, 1, 600)}
          {campoNumero('turnosExtrasAposTempo', 'Turnos após o tempo', 'Só exibido no timer.', 0, 20)}
          {campoNumero('minimoPartidasPercentualComandante', 'Mín. partidas p/ % cmd.', undefined, 1, 100)}
        </div>

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

        <ErroEmLinha erro={salvar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>{travada ? 'Fechar' : 'Cancelar'}</button>
          {!travada && <button className="botao-primario" disabled={salvar.isPending}>Salvar</button>}
        </div>
      </form>
    </Janela>
  )
}
