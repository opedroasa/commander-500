import { ChevronRight, Crown, History, Medal, Plus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useCriarRodada, useRodadasTemporada, useSalvarTemporada, useTemporadas } from '../../api/consultas'
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
  final: boolean
  rodadasPrevistas: number | null
  rodadas: Rodada[]
}

/** Nova rodada: numa data (cria a etapa se for um dia novo) ou direto numa etapa que já existe. */
type PedidoNovaRodada = { data: string } | { grupo: GrupoEtapa }

function EtapasDaTemporada({ temporada }: { temporada: Temporada }) {
  const rodadas = useRodadasTemporada(temporada.id)
  const [pedido, definirPedido] = useState<PedidoNovaRodada | null>(null)

  const grupos: GrupoEtapa[] = []
  for (const rodada of rodadas.data ?? []) {
    let grupo = grupos.find((g) => g.idEtapa === rodada.idEtapa)
    if (!grupo) {
      grupos.push((grupo = {
        idEtapa: rodada.idEtapa, numero: rodada.numeroEtapa, data: rodada.data, final: rodada.etapaFinal,
        rodadasPrevistas: rodada.rodadasPrevistasEtapa, rodadas: [],
      }))
    }
    grupo.rodadas.push(rodada)
  }
  grupos.sort((a, b) => b.numero - a.numero)
  grupos.forEach((g) => g.rodadas.sort((a, b) => a.numeroNaEtapa - b.numeroNaEtapa))
  const hoje = hojeIso()
  const temEtapaHoje = grupos.some((g) => g.data === hoje && !g.final)

  return (
    <section>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <h2 className="font-display text-lg font-bold text-zinc-100">{temporada.nome}</h2>
        <button className="botao-primario botao-pequeno ml-auto" onClick={() => definirPedido({ data: hoje })}>
          <Plus className="size-4" aria-hidden /> {temEtapaHoje ? 'Nova rodada hoje' : 'Nova etapa (hoje)'}
        </button>
        <button className="botao-secundario botao-pequeno" onClick={() => definirPedido({ data: '' })} title="Também serve para lançar rodadas que já aconteceram">
          <History className="size-4" aria-hidden /> Outra data / retroativa…
        </button>
      </div>
      {rodadas.isPending && <Carregando />}
      {rodadas.error && <CaixaErro erro={rodadas.error} />}
      {rodadas.data?.length === 0 && <Vazio>Nenhuma etapa nesta temporada ainda.</Vazio>}

      <div className="space-y-3">
        {grupos.map((grupo) => (
          <div key={grupo.idEtapa} className="cartao p-3">
            <div className="mb-2 flex items-center gap-2">
              {grupo.final ? <Crown className="size-4 text-amber-400" aria-hidden /> : <Medal className="size-4 text-amber-400" aria-hidden />}
              <span className="font-semibold text-zinc-100">{grupo.final ? 'Mesa final' : `Etapa ${grupo.numero}`}</span>
              <span className="text-sm text-zinc-400">
                {formatarData(grupo.data)}{grupo.final && grupo.rodadasPrevistas ? ` · ${grupo.rodadas.length} de ${grupo.rodadasPrevistas} rodadas` : ''}
              </span>
              <button className="botao-discreto botao-pequeno ml-auto" onClick={() => definirPedido({ grupo })}>
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

      {pedido !== null && <JanelaNovaRodada temporada={temporada} pedido={pedido} aoFechar={() => definirPedido(null)} />}
    </section>
  )
}

function JanelaNovaRodada({ temporada, pedido, aoFechar }: { temporada: Temporada; pedido: PedidoNovaRodada; aoFechar: () => void }) {
  const criar = useCriarRodada()
  const navegar = useNavigate()
  const grupo = 'grupo' in pedido ? pedido.grupo : null
  const [data, definirData] = useState(('data' in pedido && pedido.data) || hojeIso())
  const [minutos, definirMinutos] = useState(temporada.configuracao.minutosPadraoRodada)
  const salvarTemporada = useSalvarTemporada()
  const retroativa = !grupo && data < hojeIso()
  // Temporada cadastrada depois de já ter começado: a data de início anda para trás junto com a rodada retroativa.
  const antesDoInicio = !grupo && !!data && data < temporada.dataInicio

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    if (antesDoInicio) {
      await salvarTemporada.mutateAsync({
        id: temporada.id,
        dados: { nome: temporada.nome, dataInicio: data, dataFim: temporada.dataFim, ativa: temporada.ativa },
      })
    }
    const rodada = await criar.mutateAsync({ idTemporada: temporada.id, data: grupo ? null : data, duracaoMinutos: minutos, idEtapa: grupo?.idEtapa })
    aoFechar()
    navegar(`/admin/rodadas/${rodada.id}`)
  }

  const titulo = grupo ? `Nova rodada · ${grupo.final ? 'Mesa final' : `Etapa ${grupo.numero}`}` : `Nova rodada · ${temporada.nome}`
  return (
    <Janela aberta aoFechar={aoFechar} titulo={titulo}>
      <form onSubmit={enviar} className="space-y-4">
        {grupo ? (
          <p className="text-sm text-zinc-400">
            {formatarData(grupo.data)} · rodada {grupo.rodadas.length + 1}. Já vem com os presentes e decks da rodada anterior.
          </p>
        ) : (
          <Campo rotulo="Data da etapa" dica="Mesma data = mesma etapa, já com os presentes da rodada anterior. Data nova cria outra etapa.">
            <input type="date" required value={data} max={temporada.dataFim ?? undefined}
              onChange={(e) => definirData(e.target.value)} className="w-full" />
          </Campo>
        )}
        {retroativa && (
          <p className="rounded-lg border border-sky-900 bg-sky-950/40 p-2 text-sm text-sky-200">
            Data passada: rodada retroativa. Você marca quem jogou, monta as mesas como foram e lança os resultados, sem timer.
          </p>
        )}
        {antesDoInicio && (
          <p className="rounded-lg border border-amber-800 bg-amber-950/40 p-2 text-sm text-amber-200">
            A temporada está cadastrada a partir de {formatarData(temporada.dataInicio)}. Ao criar, o início dela passa a ser {formatarData(data)}.
          </p>
        )}
        <Campo rotulo="Duração (min)" dica="Pode ser ajustada de novo ao iniciar o timer.">
          <input type="number" min={1} max={600} required value={minutos} onChange={(e) => definirMinutos(Number(e.target.value))} className="w-full" />
        </Campo>
        <ErroEmLinha erro={salvarTemporada.error ?? criar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={criar.isPending || salvarTemporada.isPending}>Criar e marcar presentes</button>
        </div>
      </form>
    </Janela>
  )
}
