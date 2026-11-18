import { ChevronRight, Medal, Plus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useCriarRodada, useRodadasTemporada, useTemporadas } from '../../api/consultas'
import type { Rodada, Temporada } from '../../api/tipos'
import { CabecalhoPagina, CaixaErro, Campo, Carregando, ErroEmLinha, Janela, SeloSituacaoRodada, Vazio } from '../../componentes/comum'
import { formatarData, hojeIso } from '../../utilitarios/formatacao'

/** Tela inicial do admin: etapas de cada temporada ativa com suas rodadas, e "nova rodada". */
export function PaginaEtapasRodadas() {
  const temporadas = useTemporadas()
  if (temporadas.isPending) return <Carregando />
  if (temporadas.error) return <CaixaErro erro={temporadas.error} aoTentarDeNovo={() => temporadas.refetch()} />
  const ativas = temporadas.data.filter((t) => t.ativa)

  return (
    <>
      <CabecalhoPagina titulo="Etapas e rodadas" subtitulo="Cada dia de liga é uma etapa, com uma ou mais rodadas." />
      {ativas.length === 0 && (
        <Vazio>
          Nenhuma temporada ativa. <Link className="text-amber-300 hover:underline" to="/admin/temporadas">Crie ou ative uma temporada</Link>.
        </Vazio>
      )}
      <div className="space-y-8">
        {ativas.map((t) => <EtapasDaTemporada key={t.id} temporada={t} />)}
      </div>
    </>
  )
}

interface GrupoEtapa {
  idEtapa: number
  numero: number
  data: string
  rodadas: Rodada[]
}

function EtapasDaTemporada({ temporada }: { temporada: Temporada }) {
  const rodadas = useRodadasTemporada(temporada.id)
  const [dataNovaRodada, definirDataNovaRodada] = useState<string | null>(null)

  const grupos: GrupoEtapa[] = []
  for (const rodada of rodadas.data ?? []) {
    let grupo = grupos.find((g) => g.idEtapa === rodada.idEtapa)
    if (!grupo) grupos.push((grupo = { idEtapa: rodada.idEtapa, numero: rodada.numeroEtapa, data: rodada.data, rodadas: [] }))
    grupo.rodadas.push(rodada)
  }
  grupos.sort((a, b) => b.numero - a.numero)
  grupos.forEach((g) => g.rodadas.sort((a, b) => a.numeroNaEtapa - b.numeroNaEtapa))
  const hoje = hojeIso()
  const temEtapaHoje = grupos.some((g) => g.data === hoje)

  return (
    <section>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <h2 className="font-display text-lg font-bold text-zinc-100">{temporada.nome}</h2>
        <button className="botao-primario botao-pequeno ml-auto" onClick={() => definirDataNovaRodada(hoje)}>
          <Plus className="size-4" aria-hidden /> {temEtapaHoje ? 'Nova rodada hoje' : 'Nova etapa (hoje)'}
        </button>
        <button className="botao-secundario botao-pequeno" onClick={() => definirDataNovaRodada('')}>Outra data…</button>
      </div>
      {rodadas.isPending && <Carregando />}
      {rodadas.error && <CaixaErro erro={rodadas.error} />}
      {rodadas.data?.length === 0 && <Vazio>Nenhuma etapa nesta temporada ainda.</Vazio>}

      <div className="space-y-3">
        {grupos.map((grupo) => (
          <div key={grupo.idEtapa} className="cartao p-3">
            <div className="mb-2 flex items-center gap-2">
              <Medal className="size-4 text-amber-400" aria-hidden />
              <span className="font-semibold text-zinc-100">Etapa {grupo.numero}</span>
              <span className="text-sm text-zinc-400">{formatarData(grupo.data)}</span>
              <button className="botao-discreto botao-pequeno ml-auto" onClick={() => definirDataNovaRodada(grupo.data)}>
                <Plus className="size-4" aria-hidden /> Rodada nesta etapa
              </button>
            </div>
            <ul className="space-y-1.5">
              {grupo.rodadas.map((r) => (
                <li key={r.id}>
                  <Link to={`/admin/rodadas/${r.id}`} className="flex items-center gap-3 rounded-xl border border-zinc-800 bg-zinc-900 p-3 transition hover:border-zinc-600">
                    <span className="font-medium text-zinc-100">Rodada {r.numeroNaEtapa}</span>
                    <span className="text-xs text-zinc-500">{r.participacoes.length} presentes</span>
                    <span className="ml-auto"><SeloSituacaoRodada situacao={r.situacao} /></span>
                    <ChevronRight className="size-4 text-zinc-500" aria-hidden />
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      {dataNovaRodada !== null && (
        <JanelaNovaRodada key={dataNovaRodada} temporada={temporada} dataInicial={dataNovaRodada} aoFechar={() => definirDataNovaRodada(null)} />
      )}
    </section>
  )
}

function JanelaNovaRodada({ temporada, dataInicial, aoFechar }: { temporada: Temporada; dataInicial: string; aoFechar: () => void }) {
  const criar = useCriarRodada()
  const navegar = useNavigate()
  const [data, definirData] = useState(dataInicial || hojeIso())
  const [minutos, definirMinutos] = useState(temporada.configuracao.minutosPadraoRodada)

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    const rodada = await criar.mutateAsync({ idTemporada: temporada.id, data, duracaoMinutos: minutos })
    aoFechar()
    navegar(`/admin/rodadas/${rodada.id}`)
  }

  return (
    <Janela aberta aoFechar={aoFechar} titulo={`Nova rodada · ${temporada.nome}`}>
      <form onSubmit={enviar} className="space-y-4">
        <Campo rotulo="Data da etapa" dica="Mesma data = mesma etapa, já com os presentes da rodada anterior. Data nova cria outra etapa.">
          <input type="date" required value={data} min={temporada.dataInicio} max={temporada.dataFim ?? undefined}
            onChange={(e) => definirData(e.target.value)} className="w-full" />
        </Campo>
        <Campo rotulo="Duração (min)" dica="Pode ser ajustada de novo ao iniciar o timer.">
          <input type="number" min={1} max={600} required value={minutos} onChange={(e) => definirMinutos(Number(e.target.value))} className="w-full" />
        </Campo>
        <ErroEmLinha erro={criar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={criar.isPending}>Criar e marcar presentes</button>
        </div>
      </form>
    </Janela>
  )
}
