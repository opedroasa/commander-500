// Peças pequenas reaproveitadas em várias telas.
import { AlertTriangle, Loader2, X } from 'lucide-react'
import { useEffect, useRef, type ReactNode } from 'react'
import type { SituacaoDeck, SituacaoRodada } from '../api/tipos'
import { rotuloSituacaoDeck, rotuloSituacaoRodada } from '../utilitarios/formatacao'

export function Carregando({ texto = 'Carregando…' }: { texto?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-10 text-sm text-zinc-400" role="status">
      <Loader2 className="size-4 animate-spin" aria-hidden />
      {texto}
    </div>
  )
}

export function CaixaErro({ erro, aoTentarDeNovo }: { erro: unknown; aoTentarDeNovo?: () => void }) {
  const mensagem = erro instanceof Error ? erro.message : String(erro)
  return (
    <div className="flex flex-col items-start gap-3 rounded-xl border border-red-900/60 bg-red-950/40 p-4 text-sm text-red-200" role="alert">
      <div className="flex items-start gap-2">
        <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
        <span>{mensagem}</span>
      </div>
      {aoTentarDeNovo && <button className="botao-secundario botao-pequeno" onClick={aoTentarDeNovo}>Tentar de novo</button>}
    </div>
  )
}

/** Erro pequeno embaixo de um formulário/ação. */
export function ErroEmLinha({ erro }: { erro: unknown }) {
  if (!erro) return null
  const mensagem = erro instanceof Error ? erro.message : String(erro)
  return <p className="text-sm text-red-300" role="alert">{mensagem}</p>
}

export function Vazio({ children }: { children: ReactNode }) {
  return <div className="rounded-xl border border-dashed border-zinc-800 p-6 text-center text-sm text-zinc-400">{children}</div>
}

export function CabecalhoPagina({ titulo, subtitulo, acoes }: { titulo: ReactNode; subtitulo?: ReactNode; acoes?: ReactNode }) {
  return (
    <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
      <div className="min-w-0">
        <h1 className="font-display text-2xl font-bold tracking-wide text-amber-100 sm:text-3xl">{titulo}</h1>
        {subtitulo && <div className="mt-1 text-sm text-zinc-400">{subtitulo}</div>}
      </div>
      {acoes && <div className="flex flex-wrap gap-2">{acoes}</div>}
    </div>
  )
}

const estiloSituacaoRodada: Record<SituacaoRodada, string> = {
  Rascunho: 'bg-zinc-800 text-zinc-300',
  Sorteada: 'bg-sky-950 text-sky-300 ring-1 ring-sky-800',
  EmAndamento: 'bg-emerald-950 text-emerald-300 ring-1 ring-emerald-800',
  Encerrada: 'bg-zinc-800 text-zinc-400',
}

export function SeloSituacaoRodada({ situacao }: { situacao: SituacaoRodada }) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${estiloSituacaoRodada[situacao]}`}>
      {situacao === 'EmAndamento' && <span className="size-1.5 animate-pulse rounded-full bg-emerald-400" aria-hidden />}
      {rotuloSituacaoRodada[situacao]}
    </span>
  )
}

const estiloSituacaoDeck: Record<SituacaoDeck, string> = {
  Pendente: 'bg-amber-950 text-amber-300 ring-1 ring-amber-800/70',
  Aprovado: 'bg-emerald-950 text-emerald-300 ring-1 ring-emerald-800/70',
  Reprovado: 'bg-red-950 text-red-300 ring-1 ring-red-800/70',
}

export function SeloSituacaoDeck({ situacao }: { situacao: SituacaoDeck }) {
  return <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${estiloSituacaoDeck[situacao]}`}>{rotuloSituacaoDeck[situacao]}</span>
}

export function Abas<T extends string>({ valor, aoMudar, opcoes }: {
  valor: T
  aoMudar: (novo: T) => void
  opcoes: { valor: T; rotulo: string }[]
}) {
  return (
    <div className="mb-4 flex gap-1 overflow-x-auto rounded-xl border border-zinc-800 bg-zinc-900/60 p-1" role="tablist">
      {opcoes.map((o) => (
        <button
          key={o.valor}
          role="tab"
          aria-selected={valor === o.valor}
          onClick={() => aoMudar(o.valor)}
          className={`flex-1 whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium transition ${
            valor === o.valor ? 'bg-amber-500 text-zinc-950' : 'text-zinc-300 hover:bg-zinc-800'
          }`}
        >
          {o.rotulo}
        </button>
      ))}
    </div>
  )
}

/**
 * Janela (modal) usando o &lt;dialog&gt; do navegador: fecha com Esc e clicando fora.
 * Duas janelas podem ficar abertas uma sobre a outra (ex.: perfil do jogador + cadastro de deck).
 */
export function Janela({ aberta, aoFechar, titulo, children, larga = false }: {
  aberta: boolean
  aoFechar: () => void
  titulo: string
  children: ReactNode
  larga?: boolean
}) {
  const referencia = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const dialogo = referencia.current
    if (!dialogo) return
    if (aberta && !dialogo.open) dialogo.showModal()
    if (!aberta && dialogo.open) dialogo.close()
  }, [aberta])
  return (
    <dialog
      ref={referencia}
      onClose={aoFechar}
      onClick={(e) => { if (e.target === referencia.current) aoFechar() }}
      className={`m-auto max-h-[calc(100dvh-2rem)] rounded-2xl border border-zinc-800 bg-zinc-900 p-0 text-zinc-100 backdrop:bg-black/70 ${
        larga ? 'w-[min(44rem,calc(100vw-1.5rem))]' : 'w-[min(32rem,calc(100vw-2rem))]'
      }`}
    >
      {aberta && (
        <div className="p-5">
          <div className="mb-4 flex items-start gap-3">
            <h2 className="flex-1 text-lg font-semibold">{titulo}</h2>
            <button type="button" onClick={aoFechar} className="-m-1 rounded-lg p-1.5 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100" aria-label="Fechar">
              <X className="size-5" />
            </button>
          </div>
          {children}
        </div>
      )}
    </dialog>
  )
}

export function Campo({ rotulo, children, dica }: { rotulo: string; children: ReactNode; dica?: ReactNode }) {
  return (
    <label className="block">
      <span className="rotulo">{rotulo}</span>
      {children}
      {dica && <span className="mt-1 block text-xs text-zinc-500">{dica}</span>}
    </label>
  )
}

/**
 * Explicação das siglas de uma tabela, sempre visível embaixo dela (ex.: "Pts = pontos").
 * Cada item é [sigla, significado].
 */
export function LegendaSiglas({ itens, className = '' }: { itens: [string, string][]; className?: string }) {
  return (
    <dl className={`flex flex-wrap gap-x-4 gap-y-1 text-xs text-zinc-400 ${className}`} aria-label="Legenda das siglas">
      {itens.map(([sigla, significado]) => (
        <div key={sigla} className="flex gap-1">
          <dt className="font-semibold text-zinc-300">{sigla}</dt>
          <dd>= {significado}</dd>
        </div>
      ))}
    </dl>
  )
}
