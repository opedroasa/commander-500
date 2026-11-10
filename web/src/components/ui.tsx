import { AlertTriangle, Loader2 } from 'lucide-react'
import { useEffect, useRef, type ReactNode } from 'react'
import type { DeckStatus, RoundStatus } from '../api/types'
import { deckStatusLabel, roundStatusLabel } from '../lib/format'

export function Spinner({ label = 'Carregando…' }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-10 text-sm text-zinc-400" role="status">
      <Loader2 className="size-4 animate-spin" aria-hidden />
      {label}
    </div>
  )
}

export function ErrorBox({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const message = error instanceof Error ? error.message : String(error)
  return (
    <div className="flex flex-col items-start gap-3 rounded-xl border border-red-900/60 bg-red-950/40 p-4 text-sm text-red-200" role="alert">
      <div className="flex items-start gap-2">
        <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
        <span>{message}</span>
      </div>
      {onRetry && <button className="btn-secondary btn-sm" onClick={onRetry}>Tentar de novo</button>}
    </div>
  )
}

/** Inline error under a form/action. */
export function InlineError({ error }: { error: unknown }) {
  if (!error) return null
  const message = error instanceof Error ? error.message : String(error)
  return <p className="text-sm text-red-300" role="alert">{message}</p>
}

export function Empty({ children }: { children: ReactNode }) {
  return <div className="rounded-xl border border-dashed border-zinc-800 p-6 text-center text-sm text-zinc-400">{children}</div>
}

export function PageHeader({ title, subtitle, actions }: { title: ReactNode; subtitle?: ReactNode; actions?: ReactNode }) {
  return (
    <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
      <div className="min-w-0">
        <h1 className="font-display text-2xl font-bold tracking-wide text-amber-100 sm:text-3xl">{title}</h1>
        {subtitle && <div className="mt-1 text-sm text-zinc-400">{subtitle}</div>}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  )
}

const roundStatusStyle: Record<RoundStatus, string> = {
  Draft: 'bg-zinc-800 text-zinc-300',
  Drawn: 'bg-sky-950 text-sky-300 ring-1 ring-sky-800',
  InProgress: 'bg-emerald-950 text-emerald-300 ring-1 ring-emerald-800',
  Closed: 'bg-zinc-800 text-zinc-400',
}

export function RoundStatusBadge({ status }: { status: RoundStatus }) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${roundStatusStyle[status]}`}>
      {status === 'InProgress' && <span className="size-1.5 animate-pulse rounded-full bg-emerald-400" aria-hidden />}
      {roundStatusLabel[status]}
    </span>
  )
}

const deckStatusStyle: Record<DeckStatus, string> = {
  Pending: 'bg-amber-950 text-amber-300 ring-1 ring-amber-800/70',
  Approved: 'bg-emerald-950 text-emerald-300 ring-1 ring-emerald-800/70',
  Rejected: 'bg-red-950 text-red-300 ring-1 ring-red-800/70',
}

export function DeckStatusBadge({ status }: { status: DeckStatus }) {
  return <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${deckStatusStyle[status]}`}>{deckStatusLabel[status]}</span>
}

export function Tabs<T extends string>({ value, onChange, options }: {
  value: T
  onChange: (v: T) => void
  options: { value: T; label: string }[]
}) {
  return (
    <div className="mb-4 flex gap-1 overflow-x-auto rounded-xl border border-zinc-800 bg-zinc-900/60 p-1" role="tablist">
      {options.map((o) => (
        <button
          key={o.value}
          role="tab"
          aria-selected={value === o.value}
          onClick={() => onChange(o.value)}
          className={`flex-1 whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium transition ${
            value === o.value ? 'bg-amber-500 text-zinc-950' : 'text-zinc-300 hover:bg-zinc-800'
          }`}
        >
          {o.label}
        </button>
      ))}
    </div>
  )
}

/** Native <dialog> modal; closes on Esc and backdrop click. */
export function Modal({ open, onClose, title, children }: { open: boolean; onClose: () => void; title: string; children: ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const d = ref.current
    if (!d) return
    if (open && !d.open) d.showModal()
    if (!open && d.open) d.close()
  }, [open])
  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(e) => { if (e.target === ref.current) onClose() }}
      className="m-auto w-[min(32rem,calc(100vw-2rem))] rounded-2xl border border-zinc-800 bg-zinc-900 p-0 text-zinc-100 backdrop:bg-black/70"
    >
      {open && (
        <div className="p-5">
          <h2 className="mb-4 text-lg font-semibold">{title}</h2>
          {children}
        </div>
      )}
    </dialog>
  )
}

export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: ReactNode }) {
  return (
    <label className="block">
      <span className="label">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-zinc-500">{hint}</span>}
    </label>
  )
}
