import { ArrowDown, ArrowUp, Coffee, Plus, Save, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import { useDefinirQuemComeca, useMontarMesas } from '../api/consultas'
import type { Mesa, Rodada } from '../api/tipos'
import { ErroEmLinha } from './comum'

const MinimoPorMesa = 2
const MaximoPorMesa = 6

/**
 * Mesas montadas à mão: para lançar rodadas que já aconteceram (retroativas) ou quando o lojista não quer sortear.
 * A ordem dentro da mesa é a ordem das cadeiras (a primeira começa jogando). Quem não entrar em mesa fica de bye.
 */
export function MontagemMesas({ rodada, aoConcluir, aoCancelar }: { rodada: Rodada; aoConcluir: () => void; aoCancelar: () => void }) {
  const montar = useMontarMesas()
  const porId = new Map(rodada.participacoes.map((p) => [p.id, p]))
  const [mesas, definirMesas] = useState<number[][]>(() =>
    rodada.mesas.length > 0
      ? rodada.mesas.map((m) => [...m.cadeiras].sort((a, b) => a.numero - b.numero).map((c) => c.participacao.id))
      : Array.from({ length: Math.max(1, Math.floor(rodada.participacoes.length / 4)) }, () => []))

  const sentados = new Set(mesas.flat())
  const foraDasMesas = rodada.participacoes.filter((p) => !sentados.has(p.id))
  const mesasPreenchidas = mesas.filter((m) => m.length > 0)
  const problema =
    mesasPreenchidas.length === 0 ? 'Coloque jogadores em pelo menos uma mesa.'
      : mesasPreenchidas.some((m) => m.length < MinimoPorMesa || m.length > MaximoPorMesa)
        ? `Cada mesa precisa de ${MinimoPorMesa} a ${MaximoPorMesa} jogadores.` : null

  function alterarMesa(indice: number, nova: number[]) {
    definirMesas(mesas.map((m, i) => (i === indice ? nova : m)))
  }
  function mover(indiceMesa: number, posicao: number, deslocamento: number) {
    const mesa = [...mesas[indiceMesa]]
    const destino = posicao + deslocamento
    if (destino < 0 || destino >= mesa.length) return
    ;[mesa[posicao], mesa[destino]] = [mesa[destino], mesa[posicao]]
    alterarMesa(indiceMesa, mesa)
  }

  async function salvar() {
    await montar.mutateAsync({ idRodada: rodada.id, mesas: mesasPreenchidas })
    aoConcluir()
  }

  return (
    <section className="space-y-4">
      <p className="rounded-xl border border-sky-900 bg-sky-950/40 p-3 text-sm text-sky-200">
        Monte as mesas como foram (ou serão) jogadas. A ordem na mesa é a ordem das cadeiras: o 1º começa jogando.
        Quem ficar fora das mesas fica de bye.
      </p>

      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {mesas.map((mesa, indiceMesa) => (
          <div key={indiceMesa} className="cartao p-3">
            <div className="mb-2 flex items-center gap-2">
              <span className="font-display text-lg font-bold text-amber-100">Mesa {indiceMesa + 1}</span>
              <span className={`text-xs ${mesa.length > 0 && (mesa.length < MinimoPorMesa || mesa.length > MaximoPorMesa) ? 'text-red-300' : 'text-zinc-500'}`}>
                {mesa.length} jogador{mesa.length === 1 ? '' : 'es'}
              </span>
              {mesas.length > 1 && (
                <button type="button" className="botao-discreto botao-pequeno ml-auto px-1.5" aria-label={`Remover mesa ${indiceMesa + 1}`}
                  onClick={() => definirMesas(mesas.filter((_, i) => i !== indiceMesa))}>
                  <Trash2 className="size-3.5" />
                </button>
              )}
            </div>
            <ol className="space-y-1">
              {mesa.map((idParticipacao, posicao) => (
                <li key={idParticipacao} className="flex items-center gap-2 rounded-lg border border-zinc-800 bg-zinc-900 px-2 py-1.5 text-sm">
                  <span className={`flex size-5 shrink-0 items-center justify-center rounded-full text-[0.65rem] font-bold ${posicao === 0 ? 'bg-brand-orange text-white' : 'bg-zinc-700 text-zinc-100'}`}>
                    {posicao + 1}
                  </span>
                  <span className="min-w-0 flex-1 truncate">{porId.get(idParticipacao)?.nomeJogador}</span>
                  <button type="button" className="botao-discreto botao-pequeno px-1" aria-label="Subir" onClick={() => mover(indiceMesa, posicao, -1)}><ArrowUp className="size-3.5" /></button>
                  <button type="button" className="botao-discreto botao-pequeno px-1" aria-label="Descer" onClick={() => mover(indiceMesa, posicao, 1)}><ArrowDown className="size-3.5" /></button>
                  <button type="button" className="botao-discreto botao-pequeno px-1" aria-label="Tirar da mesa"
                    onClick={() => alterarMesa(indiceMesa, mesa.filter((id) => id !== idParticipacao))}><X className="size-3.5" /></button>
                </li>
              ))}
            </ol>
            {foraDasMesas.length > 0 && mesa.length < MaximoPorMesa && (
              <select className="mt-2 w-full" value="" aria-label={`Adicionar jogador à mesa ${indiceMesa + 1}`}
                onChange={(e) => e.target.value && alterarMesa(indiceMesa, [...mesa, Number(e.target.value)])}>
                <option value="">+ Adicionar jogador…</option>
                {foraDasMesas.map((p) => <option key={p.id} value={p.id}>{p.nomeJogador}</option>)}
              </select>
            )}
          </div>
        ))}
        <button type="button" className="botao-secundario min-h-24 border-dashed" onClick={() => definirMesas([...mesas, []])}>
          <Plus className="size-4" aria-hidden /> Mesa
        </button>
      </div>

      <div className="cartao p-3 text-sm">
        <span className="flex items-center gap-2 font-semibold text-zinc-200"><Coffee className="size-4 text-amber-400" aria-hidden />Fora das mesas (bye)</span>
        <p className="mt-1 text-zinc-400">{foraDasMesas.length ? foraDasMesas.map((p) => p.nomeJogador).join(', ') : 'Ninguém.'}</p>
      </div>

      <div className="flex flex-wrap items-center justify-end gap-2">
        {problema && <span className="mr-auto text-sm text-amber-300">{problema}</span>}
        <ErroEmLinha erro={montar.error} />
        <button type="button" className="botao-discreto" onClick={aoCancelar} disabled={montar.isPending}>Cancelar</button>
        <button type="button" className="botao-primario" onClick={salvar} disabled={montar.isPending || problema !== null}>
          <Save className="size-4" aria-hidden /> Salvar mesas
        </button>
      </div>
    </section>
  )
}

/** Troca quem começa a mesa: o escolhido vai para a cadeira 1 e os outros giram junto (mantém a ordem de jogo). */
export function SeletorQuemComeca({ mesa }: { mesa: Mesa }) {
  const definir = useDefinirQuemComeca()
  const ordenadas = [...mesa.cadeiras].sort((a, b) => a.numero - b.numero)
  return (
    <label className="mt-2 flex items-center gap-2 text-xs text-zinc-400">
      Quem começa
      <select className="min-w-0 flex-1 py-1 text-sm" value={ordenadas[0]?.participacao.id} disabled={definir.isPending}
        onChange={(e) => definir.mutate({ idMesa: mesa.id, idParticipacao: Number(e.target.value) })}>
        {ordenadas.map((c) => <option key={c.participacao.id} value={c.participacao.id}>{c.participacao.nomeJogador}</option>)}
      </select>
      <ErroEmLinha erro={definir.error} />
    </label>
  )
}
