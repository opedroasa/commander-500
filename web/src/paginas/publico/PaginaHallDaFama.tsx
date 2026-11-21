import { CalendarDays, Crown, Medal, Trophy } from 'lucide-react'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { useHallDaFama } from '../../api/consultas'
import type { LinhaHallDaFama, TemporadaHallDaFama } from '../../api/tipos'
import { ArteComandante } from '../../componentes/ArteComandante'
import { CabecalhoPagina, CaixaErro, Carregando, LegendaSiglas, Vazio } from '../../componentes/comum'
import { FotoDestaque, FundoDesfocado } from '../../componentes/Fotos'
import { JanelaPerfilJogador } from '../../componentes/JanelaPerfilJogador'
import { formatarData } from '../../utilitarios/formatacao'

/** Cor da medalha do pódio (1º laranja da logo, 2º prata, 3º bronze). */
const corPodio: Record<number, string> = { 1: 'bg-brand-orange text-white', 2: 'bg-zinc-300 text-zinc-900', 3: 'bg-amber-800 text-amber-50' }

/**
 * Hall da Fama: o pódio de cada temporada encerrada (galeria) e, ao escolher uma, o ranking final completo com o deck
 * de cada jogador. A foto de destaque é a que o lojista sobe ao fim da temporada.
 */
export function PaginaHallDaFama() {
  const hall = useHallDaFama()
  const [parametros, definirParametros] = useSearchParams()
  const [idPerfil, definirIdPerfil] = useState<number | null>(null)

  if (hall.isPending) return <Carregando />
  if (hall.error) return <CaixaErro erro={hall.error} aoTentarDeNovo={() => hall.refetch()} />
  const temporadas = hall.data

  const idEscolhida = Number(parametros.get('temporada')) || temporadas[0]?.id
  const escolhida = temporadas.find((t) => t.id === idEscolhida) ?? temporadas[0]
  const escolher = (id: number) => {
    definirParametros({ temporada: String(id) }, { replace: true })
    document.getElementById('ranking-final')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  return (
    <>
      <CabecalhoPagina
        titulo={<span className="inline-flex items-center gap-2"><Trophy className="size-7 text-amber-400" aria-hidden />Hall da Fama</span>}
        subtitulo="Os campeões de cada temporada da Liga Commander 500."
      />

      {temporadas.length === 0 ? (
        <Vazio>
          Nenhuma temporada encerrada ainda. Quando a primeira acabar, o pódio aparece aqui.
          {' '}<Link className="text-amber-300 hover:underline" to="/">Ver a temporada em andamento</Link>
        </Vazio>
      ) : (
        <div className="space-y-8">
          <section aria-label="Temporadas encerradas" className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {temporadas.map((t) => (
              <CartaoTemporada key={t.id} temporada={t} selecionada={t.id === escolhida?.id} aoEscolher={() => escolher(t.id)} />
            ))}
          </section>

          {escolhida && <RankingFinal temporada={escolhida} temporadas={temporadas} aoEscolher={escolher} aoAbrirJogador={definirIdPerfil} />}
        </div>
      )}

      <JanelaPerfilJogador idJogador={idPerfil} idTemporada={escolhida?.id ?? null} aoFechar={() => definirIdPerfil(null)} />
    </>
  )
}

/** Cartão da galeria: foto (ou arte do comandante do campeão), nome, datas e o pódio. */
function CartaoTemporada({ temporada, selecionada, aoEscolher }: { temporada: TemporadaHallDaFama; selecionada: boolean; aoEscolher: () => void }) {
  const podio = temporada.ranking.filter((l) => l.posicao <= 3)
  const campeao = temporada.ranking[0]
  return (
    <button type="button" onClick={aoEscolher} aria-pressed={selecionada}
      className={`cartao group flex flex-col overflow-hidden text-left transition hover:border-zinc-600 ${selecionada ? 'ring-2 ring-amber-500' : ''}`}>
      <div className="relative isolate h-56 w-full">
        {/* Foto inteira (em pé ou deitada), sem corte; as sobras ficam com o fundo desfocado da própria foto. */}
        {temporada.urlFoto
          ? <><FundoDesfocado url={temporada.urlFoto} />
              <img src={temporada.urlFoto} alt={temporada.legendaFoto ?? `Top de ${temporada.nome}`} loading="lazy" className="size-full object-contain" /></>
          : campeao?.deck
            ? <ArteComandante comandantes={campeao.deck.comandantes} className="size-full" arredondamento="rounded-none" />
            : <div className="size-full bg-[linear-gradient(135deg,var(--color-brand-navy),#0a1128_55%,var(--color-brand-red))]" />}
        <div className="absolute inset-0 bg-gradient-to-t from-black/85 via-black/20 to-transparent" />
        <div className="absolute inset-x-0 bottom-0 p-3 text-white">
          <div className="font-display text-xl font-bold leading-tight drop-shadow">{temporada.nome}</div>
          <div className="flex items-center gap-1.5 text-xs text-white/80">
            <CalendarDays className="size-3.5" aria-hidden />
            {formatarData(temporada.dataInicio)}{temporada.dataFim ? ` – ${formatarData(temporada.dataFim)}` : ''}
          </div>
        </div>
      </div>
      <ol className="space-y-1.5 p-3">
        {podio.length === 0 && <li className="text-sm text-zinc-400">Sem partidas registradas.</li>}
        {podio.map((l) => <LinhaPodio key={l.idJogador} linha={l} />)}
      </ol>
      {temporada.legendaFoto && <p className="px-3 pb-3 text-xs text-zinc-400">{temporada.legendaFoto}</p>}
    </button>
  )
}

function LinhaPodio({ linha }: { linha: LinhaHallDaFama }) {
  return (
    <li className="flex items-center gap-2.5">
      <span className={`inline-flex size-6 shrink-0 items-center justify-center rounded-full text-xs font-bold ${corPodio[linha.posicao] ?? ''}`}>
        {linha.posicao}
      </span>
      {linha.deck && <ArteComandante comandantes={linha.deck.comandantes} className="h-8 w-11 shrink-0" arredondamento="rounded-md" />}
      <span className="min-w-0 flex-1">
        <span className="flex items-center gap-1 text-sm font-semibold text-zinc-100">
          <span className="break-words">{linha.nomeJogador}</span>
          {linha.posicao === 1 && <Crown className="size-3.5 shrink-0 text-amber-400" aria-label="Campeão" />}
        </span>
        {linha.deck && <span className="block text-xs leading-snug text-zinc-400 break-words">{nomesComandantes(linha)}</span>}
      </span>
      <span className="shrink-0 text-sm font-bold tabular-nums text-amber-300">{linha.pontos}</span>
    </li>
  )
}

const nomesComandantes = (linha: LinhaHallDaFama) => linha.deck?.comandantes.map((c) => c.nome).join(' + ') ?? ''

/** Ranking final completo da temporada escolhida. */
function RankingFinal({ temporada, temporadas, aoEscolher, aoAbrirJogador }: {
  temporada: TemporadaHallDaFama
  temporadas: TemporadaHallDaFama[]
  aoEscolher: (id: number) => void
  aoAbrirJogador: (id: number) => void
}) {
  return (
    <section id="ranking-final" className="scroll-mt-20 space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <h2 className="flex items-center gap-2 text-lg font-bold text-zinc-50"><Medal className="size-5 text-amber-400" aria-hidden />Ranking final</h2>
        <select value={temporada.id} onChange={(e) => aoEscolher(Number(e.target.value))} className="ml-auto" aria-label="Temporada">
          {temporadas.map((t) => <option key={t.id} value={t.id}>{t.nome}</option>)}
        </select>
      </div>

      {temporada.urlFoto && <FotoDestaque url={temporada.urlFoto} legenda={temporada.legendaFoto} />}

      <div className="cartao overflow-x-auto">
        <table className="w-full text-[13px] tabular-nums sm:text-sm">
          <thead className="text-xs uppercase tracking-wide text-zinc-400">
            <tr className="border-b border-zinc-800">
              <th className="px-2 py-2.5 text-left sm:px-3">#</th>
              <th className="px-1.5 py-2.5 text-left">Jogador</th>
              <th className="px-1.5 py-2.5 text-left">Deck</th>
              <th className="px-2 py-2.5 text-right sm:px-3" title="Pontos">Pts</th>
            </tr>
          </thead>
          <tbody>
            {temporada.ranking.map((l) => (
              <tr key={l.idJogador} className="border-b border-zinc-800/60 last:border-0">
                <td className="px-2 py-2 sm:px-3">
                  <span className={`inline-flex size-6 items-center justify-center rounded-full text-xs font-bold ${corPodio[l.posicao] ?? 'text-zinc-400'}`}
                    title={l.empatado ? 'Empatado' : undefined}>{l.posicao}</span>
                </td>
                <td className="px-1.5 py-2">
                  <button type="button" onClick={() => aoAbrirJogador(l.idJogador)}
                    className="text-left font-medium text-zinc-100 break-words hover:text-amber-300 hover:underline">{l.nomeJogador}</button>
                </td>
                <td className="px-1.5 py-2">
                  {l.deck ? (
                    <span className="flex items-center gap-2">
                      <ArteComandante comandantes={l.deck.comandantes} className="hidden h-7 w-10 shrink-0 sm:flex" arredondamento="rounded" />
                      <span className="min-w-0 text-xs leading-snug text-zinc-300 break-words">{nomesComandantes(l)}</span>
                    </span>
                  ) : <span className="text-zinc-500">–</span>}
                </td>
                <td className="px-2 py-2 text-right text-base font-bold text-amber-300 sm:px-3">{l.pontos}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <LegendaSiglas itens={[
        ['#', 'posição final (número repetido = empatados)'],
        ['Deck', 'o deck que o jogador mais usou na temporada'],
        ['Pts', 'pontos na temporada'],
      ]} />
      <Link to={`/temporadas/${temporada.id}`} className="inline-block text-sm font-medium text-amber-300 hover:underline">
        Ver etapas, rodadas e comandantes desta temporada →
      </Link>
    </section>
  )
}
