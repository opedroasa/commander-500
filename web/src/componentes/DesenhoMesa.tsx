import { AlertTriangle, Crown, Handshake } from 'lucide-react'
import type { Cadeira, Mesa } from '../api/tipos'
import { rotuloSituacaoDeck } from '../utilitarios/formatacao'

/**
 * Mesa retangular da loja: metade dos jogadores de cada lado (2 + 2 na mesa de 4).
 * Ordem dos turnos em sentido horário: 1 (em cima à esquerda, começa) → … → em cima à direita → embaixo à direita → … → embaixo à esquerda.
 * Mesas montadas à mão podem ter de 2 a 6 cadeiras.
 */
function posicaoDaCadeira(numero: number, colunas: number) {
  return numero <= colunas
    ? { gridRowStart: 1, gridColumnStart: numero }
    : { gridRowStart: 3, gridColumnStart: colunas - (numero - colunas) + 1 }
}

interface Propriedades {
  mesa: Mesa
  /** Jogadores destacados (ex.: busca "encontre sua mesa"). */
  idsJogadoresDestacados?: Set<number>
  /** Lançamento de resultado no admin: clicar numa cadeira marca/desmarca o jogador. */
  aoClicarCadeira?: (cadeira: Cadeira) => void
  /**
   * Jogadores marcados no admin, antes de salvar: 1 = vencedor; 2 ou mais = empate entre eles.
   * Indefinido = mostra o resultado gravado.
   */
  idsEscolhidos?: number[]
  mostrarAvisoDeck?: boolean
}

/** Desenho da mesa (pod) com as cadeiras (normalmente 4), jogador e comandante em cada uma (spec 4.3). */
export function DesenhoMesa({ mesa, idsJogadoresDestacados, aoClicarCadeira, idsEscolhidos, mostrarAvisoDeck = true }: Propriedades) {
  const editando = idsEscolhidos !== undefined
  // Quem aparece como vencedor, quem aparece no empate e quem fica apagado (perdeu).
  const idVencedor = editando ? (idsEscolhidos.length === 1 ? idsEscolhidos[0] : null)
    : mesa.resultado === 'Vitoria' ? mesa.idParticipacaoVencedora : null
  const empatados = new Set(editando ? (idsEscolhidos.length >= 2 ? idsEscolhidos : [])
    : mesa.cadeiras.filter((c) => c.empatou).map((c) => c.participacao.id))
  const decidida = idVencedor != null || empatados.size > 0
  const empateGravado = !editando && mesa.resultado === 'Empate'
  const empateParcial = empateGravado && empatados.size < mesa.cadeiras.length
  const colunas = Math.max(2, Math.ceil(mesa.cadeiras.length / 2))

  return (
    <div className="grid gap-1.5 sm:gap-2" style={{ gridTemplateColumns: `repeat(${colunas}, minmax(0, 1fr))` }}>
      {/* o tampo: feltro verde com borda de madeira, entre os dois lados */}
      <div style={{ gridColumn: '1 / -1' }} className="row-start-2 flex h-16 items-center justify-center gap-3 rounded-lg border-[5px] border-[#6b3f1d] bg-[radial-gradient(ellipse_at_center,#1f5c3a,#0f3a24_75%)] shadow-[inset_0_0_18px_rgba(0,0,0,.55),0_2px_6px_rgba(0,0,0,.35)]">
        <span className="font-display text-xs uppercase tracking-[0.3em] text-[#cfe8d6]/80">Mesa</span>
        <span className="font-display text-3xl font-bold leading-none text-white">{mesa.numero}</span>
        {(empateGravado || (editando && empatados.size > 0)) && (
          <span className="inline-flex items-center gap-1 rounded-full bg-black/40 px-2 py-0.5 text-[0.7rem] font-semibold text-white">
            <Handshake className="size-3" aria-hidden /> {empateParcial || (editando && empatados.size < mesa.cadeiras.length) ? `Empate entre ${empatados.size}` : 'Empate'}
          </span>
        )}
      </div>

      {mesa.cadeiras.map((cadeira) => (
        <CartaoCadeira
          key={cadeira.numero}
          cadeira={cadeira}
          posicao={posicaoDaCadeira(cadeira.numero, colunas)}
          vencedor={idVencedor === cadeira.participacao.id}
          empatou={empatados.has(cadeira.participacao.id)}
          apagada={decidida && idVencedor !== cadeira.participacao.id && !empatados.has(cadeira.participacao.id)}
          destacada={idsJogadoresDestacados?.has(cadeira.participacao.idJogador) ?? false}
          aoClicar={aoClicarCadeira ? () => aoClicarCadeira(cadeira) : undefined}
          mostrarAvisoDeck={mostrarAvisoDeck}
        />
      ))}
    </div>
  )
}

function CartaoCadeira({ cadeira, posicao, vencedor, empatou, apagada, destacada, aoClicar, mostrarAvisoDeck }: {
  cadeira: Cadeira
  posicao: { gridRowStart: number; gridColumnStart: number }
  vencedor: boolean
  /** ficou no empate (num empate parcial só alguns da mesa) */
  empatou: boolean
  apagada: boolean
  destacada: boolean
  aoClicar?: () => void
  mostrarAvisoDeck: boolean
}) {
  const { participacao } = cadeira
  const deckNaoAprovado = participacao.situacaoDeck !== 'Aprovado'
  const Elemento = aoClicar ? 'button' : 'div'

  return (
    <Elemento
      type={aoClicar ? 'button' : undefined}
      onClick={aoClicar}
      aria-pressed={aoClicar ? vencedor || empatou : undefined}
      aria-label={aoClicar ? `Marcar ${participacao.nomeJogador}` : undefined}
      style={posicao}
      className={`relative isolate flex min-h-[5.5rem] min-w-0 flex-col justify-end overflow-hidden rounded-xl border p-2 text-left transition ${
        vencedor
          ? 'border-amber-400 shadow-[0_0_0_1px_rgba(242,101,34,.6),0_0_18px_rgba(242,101,34,.3)]'
          : empatou
            ? 'border-sky-400/80 shadow-[0_0_0_1px_rgba(74,137,220,.5)]'
            : destacada
            ? 'border-sky-400 ring-2 ring-sky-400/40'
            : 'border-zinc-800'
      } bg-zinc-900 ${apagada ? 'opacity-50' : ''} ${aoClicar ? 'cursor-pointer hover:border-amber-500/70 active:scale-[.98]' : ''}`}
    >
      {/* arte do comandante esmaecida no fundo */}
      {participacao.urlArteComandante && (
        <>
          <img src={participacao.urlArteComandante} alt="" loading="lazy" className="absolute inset-0 -z-20 size-full object-cover opacity-60" />
          <div className="absolute inset-0 -z-10 bg-gradient-to-t from-zinc-950 via-zinc-950/80 to-zinc-950/30" />
        </>
      )}
      <div className="mb-auto flex items-center gap-1.5 pb-1">
        <span
          className={`flex size-5 shrink-0 items-center justify-center rounded-full text-[0.65rem] font-bold ${
            cadeira.numero === 1 ? 'bg-brand-orange text-white' : 'bg-zinc-700 text-zinc-100'
          }`}
          title={cadeira.numero === 1 ? 'Cadeira 1 — começa jogando' : `Cadeira ${cadeira.numero}`}
        >
          {cadeira.numero}
        </span>
        {cadeira.numero === 1 && <span className="text-[0.6rem] font-semibold uppercase tracking-wide text-amber-400">começa</span>}
        {vencedor && <Crown className="ml-auto size-4 shrink-0 text-amber-400" aria-label="Vencedor" />}
        {empatou && <Handshake className="ml-auto size-4 shrink-0 text-sky-300" aria-label="Empatou" />}
      </div>
      <div className="line-clamp-2 text-sm font-semibold leading-tight text-zinc-50 break-words">{participacao.nomeJogador}</div>
      <div className="mt-0.5 line-clamp-2 text-[0.7rem] leading-tight text-zinc-300 break-words">{participacao.comandantes || participacao.nomeDeck}</div>
      {mostrarAvisoDeck && deckNaoAprovado && (
        <div
          className={`mt-1 inline-flex items-center gap-1 text-[0.65rem] font-medium ${participacao.situacaoDeck === 'Reprovado' ? 'text-red-300' : 'text-amber-300'}`}
          title={`Deck ${rotuloSituacaoDeck[participacao.situacaoDeck].toLowerCase()}`}
        >
          <AlertTriangle className="size-3 shrink-0" aria-hidden /> Deck {rotuloSituacaoDeck[participacao.situacaoDeck].toLowerCase()}
        </div>
      )}
    </Elemento>
  )
}
