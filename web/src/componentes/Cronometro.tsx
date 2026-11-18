import { Hourglass, TimerOff } from 'lucide-react'
import { useMemo } from 'react'
import type { Cronometro as DadosCronometro } from '../api/tipos'
import { formatarRelogio } from '../utilitarios/formatacao'
import { lerAgora, useAgora } from '../utilitarios/relogio'

/**
 * Timer da rodada calculado pelo início + duração gravados no servidor (spec 4.4). Usa a diferença para o relógio
 * do servidor, então todo aparelho mostra o mesmo tempo e recarregar a página não reinicia nada.
 */
export function Cronometro({ cronometro, agoraServidor, turnosExtras = 0, tamanho = 'grande' }: {
  cronometro: DadosCronometro
  agoraServidor: string
  turnosExtras?: number
  tamanho?: 'grande' | 'pequeno'
}) {
  const agoraNavegador = useAgora()
  // Diferença para o relógio do servidor, medida quando chega uma resposta nova (±0,5 s basta para um timer de rodada).
  const diferencaServidor = useMemo(() => new Date(agoraServidor).getTime() - lerAgora(), [agoraServidor])
  const terminaEm = new Date(cronometro.terminaEm).getTime()
  const agora = agoraNavegador + diferencaServidor

  const restante = Math.max(0, Math.ceil((terminaEm - agora) / 1000))
  const total = cronometro.duracaoMinutos * 60
  const proporcao = total > 0 ? restante / total : 0
  const esgotado = restante === 0
  const alerta = !esgotado && restante <= 5 * 60

  if (tamanho === 'pequeno') {
    return (
      <span className={`inline-flex items-center gap-1 font-mono text-sm tabular-nums ${esgotado ? 'text-red-300' : alerta ? 'text-amber-300' : 'text-zinc-200'}`}>
        {esgotado ? <TimerOff className="size-4" aria-hidden /> : <Hourglass className="size-4" aria-hidden />}
        {esgotado ? 'Tempo esgotado' : formatarRelogio(restante)}
      </span>
    )
  }

  return (
    <div
      className={`rounded-2xl border p-4 text-center ${
        esgotado ? 'border-red-800 bg-red-950/40' : alerta ? 'border-amber-700 bg-amber-950/30' : 'border-zinc-800 bg-zinc-900/60'
      }`}
      role="timer"
      aria-live="off"
    >
      <div className={`font-mono text-5xl font-bold tabular-nums sm:text-6xl ${esgotado ? 'text-red-300' : alerta ? 'text-amber-300' : 'text-zinc-50'}`}>
        {formatarRelogio(restante)}
      </div>
      <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-zinc-800">
        <div
          className={`h-full rounded-full transition-[width] duration-500 ${esgotado ? 'bg-red-500' : alerta ? 'bg-amber-400' : 'bg-emerald-500'}`}
          style={{ width: `${proporcao * 100}%` }}
        />
      </div>
      <p className="mt-2 text-sm text-zinc-400">
        {esgotado
          ? turnosExtras > 0
            ? `Tempo esgotado — joguem mais ${turnosExtras} turno${turnosExtras > 1 ? 's' : ''}.`
            : 'Tempo esgotado.'
          : `Rodada de ${cronometro.duracaoMinutos} min`}
      </p>
    </div>
  )
}
