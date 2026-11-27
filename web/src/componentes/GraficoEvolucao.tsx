import { useState } from 'react'
import type { PontoMensal } from '../api/tipos'
import { formatarPercentual } from '../utilitarios/formatacao'

// Gráfico de linhas da taxa de vitória mês a mês (página do jogador e do comandante).
// Duas séries no mesmo eixo (0–100%): "% no mês" (azul, tracejada) e "% acumulado" (laranja, contínua).
// Identidade nunca só pela cor: legenda + rótulo no fim de cada linha + traço diferente. Passar o mouse/dedo
// mostra a linha-guia e os números do mês; "Ver como tabela" mostra os mesmos dados em texto.

const Largura = 640
const Altura = 240
const Margem = { topo: 16, direita: 84, baixo: 28, esquerda: 40 }
const AreaX = Largura - Margem.esquerda - Margem.direita
const AreaY = Altura - Margem.topo - Margem.baixo

const series = [
  { chave: 'percentualAcumulado', nome: '% acumulado', cor: 'var(--cor-serie-1)', traco: undefined },
  { chave: 'percentualMes', nome: '% no mês', cor: 'var(--cor-serie-2)', traco: '6 5' },
] as const

export function GraficoEvolucao({ pontos, titulo = 'Taxa de vitória por mês' }: { pontos: PontoMensal[]; titulo?: string }) {
  const [indiceAtivo, definirIndiceAtivo] = useState<number | null>(null)
  const [verTabela, definirVerTabela] = useState(false)

  if (pontos.length === 0) {
    return <p className="rounded-xl border border-dashed border-zinc-800 p-6 text-center text-sm text-zinc-400">Ainda sem partidas para o gráfico.</p>
  }

  const passoX = pontos.length > 1 ? AreaX / (pontos.length - 1) : 0
  const x = (i: number) => Margem.esquerda + (pontos.length > 1 ? i * passoX : AreaX / 2)
  const y = (valor: number) => Margem.topo + (1 - valor) * AreaY
  // Rótulos do eixo X sem encavalar: no máximo ~8.
  const saltoRotulo = Math.max(1, Math.ceil(pontos.length / 8))
  const ativo = indiceAtivo == null ? null : pontos[indiceAtivo]
  const ultimo = pontos.length - 1

  // Rótulos no fim das linhas: se os dois valores estão perto, afasta um do outro (mínimo 16px) para não encavalar.
  const yAcumulado = y(pontos[ultimo].percentualAcumulado) + 4
  const yMes = y(pontos[ultimo].percentualMes) + 4
  const separacao = 16
  const yRotulo = { percentualAcumulado: yAcumulado, percentualMes: yMes }
  if (Math.abs(yAcumulado - yMes) < separacao) {
    const meio = (yAcumulado + yMes) / 2
    const acumuladoEmCima = yAcumulado <= yMes
    yRotulo.percentualAcumulado = meio + (acumuladoEmCima ? -separacao / 2 : separacao / 2)
    yRotulo.percentualMes = meio + (acumuladoEmCima ? separacao / 2 : -separacao / 2)
  }

  return (
    <figure className="space-y-3">
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
        <figcaption className="mr-auto text-sm font-semibold text-zinc-200">{titulo}</figcaption>
        {series.map((s) => (
          <span key={s.chave} className="inline-flex items-center gap-1.5 text-xs text-zinc-300">
            <svg width="22" height="8" aria-hidden><line x1="1" y1="4" x2="21" y2="4" stroke={s.cor} strokeWidth="2" strokeDasharray={s.traco} strokeLinecap="round" /></svg>
            {s.nome}
          </span>
        ))}
        <button type="button" className="botao-discreto botao-pequeno" onClick={() => definirVerTabela(!verTabela)}>
          {verTabela ? 'Ver gráfico' : 'Ver como tabela'}
        </button>
      </div>

      {verTabela ? (
        <div className="overflow-x-auto">
          <table className="w-full text-sm tabular-nums">
            <thead className="text-xs uppercase tracking-wide text-zinc-400">
              <tr className="border-b border-zinc-800">
                <th className="px-2 py-1.5 text-left">Mês</th><th className="px-2 py-1.5 text-right">% no mês</th>
                <th className="px-2 py-1.5 text-right">% acumulado</th><th className="px-2 py-1.5 text-right">V/E/D</th>
              </tr>
            </thead>
            <tbody>
              {pontos.map((p) => (
                <tr key={p.mes} className="border-b border-zinc-800/60 last:border-0">
                  <td className="px-2 py-1.5">{p.mes}</td>
                  <td className="px-2 py-1.5 text-right">{formatarPercentual(p.percentualMes)}</td>
                  <td className="px-2 py-1.5 text-right">{formatarPercentual(p.percentualAcumulado)}</td>
                  <td className="px-2 py-1.5 text-right text-zinc-400">{p.vitorias}/{p.empates}/{p.derrotas}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="relative">
          <svg viewBox={`0 0 ${Largura} ${Altura}`} className="h-auto w-full overflow-visible" role="img"
            aria-label={`${titulo}: ${pontos.map((p) => `${p.mes} ${formatarPercentual(p.percentualMes)}`).join(', ')}`}
            onMouseLeave={() => definirIndiceAtivo(null)}>
            {/* grade recessiva e eixo Y em % */}
            {[0, 0.25, 0.5, 0.75, 1].map((v) => (
              <g key={v}>
                <line x1={Margem.esquerda} x2={Largura - Margem.direita} y1={y(v)} y2={y(v)} className="stroke-zinc-800" strokeWidth="1" />
                <text x={Margem.esquerda - 8} y={y(v) + 4} textAnchor="end" className="fill-zinc-500 text-[11px]">{v * 100}%</text>
              </g>
            ))}
            {pontos.map((p, i) => (i % saltoRotulo === 0 || i === ultimo) && (
              <text key={p.mes} x={x(i)} y={Altura - 6} textAnchor="middle" className="fill-zinc-500 text-[11px]">{p.mes}</text>
            ))}

            {/* linha-guia do mês sob o cursor */}
            {indiceAtivo != null && (
              <line x1={x(indiceAtivo)} x2={x(indiceAtivo)} y1={Margem.topo} y2={Margem.topo + AreaY} className="stroke-zinc-500" strokeWidth="1" />
            )}

            {series.map((s) => {
              const caminho = pontos.map((p, i) => `${i === 0 ? 'M' : 'L'}${x(i)},${y(p[s.chave])}`).join(' ')
              return (
                <g key={s.chave}>
                  {/* A linha contínua "se desenha"; a tracejada aparece aos poucos (o desenho usa o próprio tracejado). */}
                  {pontos.length > 1 && (s.traco
                    ? <path d={caminho} fill="none" stroke={s.cor} strokeWidth="2" strokeLinejoin="round" strokeDasharray={s.traco}
                        className="ponto-surgindo" style={{ animationDuration: '1.2s' }} />
                    : <path d={caminho} fill="none" stroke={s.cor} strokeWidth="2" strokeLinejoin="round" strokeLinecap="round"
                        pathLength={1} className="linha-desenhando" />)}
                  {pontos.map((p, i) => (
                    <circle key={p.mes} cx={x(i)} cy={y(p[s.chave])} r={indiceAtivo === i ? 5 : 4} fill={s.cor}
                      className="ponto-surgindo stroke-zinc-900" strokeWidth="2" style={{ animationDelay: `${0.3 + (i / pontos.length)}s` }} />
                  ))}
                  {/* rótulo direto no fim da linha */}
                  <text x={x(ultimo) + 10} y={yRotulo[s.chave]} className="fill-zinc-300 text-[11px] font-semibold">
                    {formatarPercentual(pontos[ultimo][s.chave])} {s.chave === 'percentualMes' ? 'mês' : 'acum.'}
                  </text>
                </g>
              )
            })}

            {/* áreas de toque largas (uma por mês) para o hover/toque */}
            {pontos.map((p, i) => (
              <rect key={p.mes} x={x(i) - Math.max(passoX, 40) / 2} y={Margem.topo} width={Math.max(passoX, 40)} height={AreaY}
                fill="transparent" onMouseEnter={() => definirIndiceAtivo(i)} onClick={() => definirIndiceAtivo(i)} />
            ))}
          </svg>

          {ativo && indiceAtivo != null && (
            <div className="pointer-events-none absolute top-0 z-10 min-w-36 -translate-x-1/2 rounded-lg border border-zinc-700 bg-zinc-950/95 px-3 py-2 text-xs shadow-xl"
              style={{ left: `${(x(indiceAtivo) / Largura) * 100}%` }}>
              <div className="mb-1 font-semibold text-zinc-100">{ativo.mes}</div>
              <div className="text-zinc-300">% no mês: <b className="text-zinc-100">{formatarPercentual(ativo.percentualMes)}</b></div>
              <div className="text-zinc-300">% acumulado: <b className="text-zinc-100">{formatarPercentual(ativo.percentualAcumulado)}</b></div>
              <div className="mt-1 text-zinc-400">{ativo.partidas} partida{ativo.partidas === 1 ? '' : 's'} · V/E/D {ativo.vitorias}/{ativo.empates}/{ativo.derrotas}</div>
            </div>
          )}
        </div>
      )}
    </figure>
  )
}
