import { CalendarClock, CalendarDays, ChevronRight, Crown, FileText, Radio, ScrollText, Trophy } from 'lucide-react'
import { Link } from 'react-router'
import { useEstatisticasComandantes, useRanking, useRodadasAtuais, useTemporadas } from '../../api/consultas'
import type { EstatisticaComandante, Temporada } from '../../api/tipos'
import { ArteComandante } from '../../componentes/ArteComandante'
import { CaixaErro, Carregando, Vazio } from '../../componentes/comum'
import { LogoLoja } from '../../componentes/Layout'
import { CartoesArteComandante, ListaComandantes } from '../../componentes/RankingComandantes'
import { formatarData, formatarDiaEHora, hojeIso, paraCampoDataHora } from '../../utilitarios/formatacao'

/** Comandante com mais vitórias (empate: maior % de vitória, depois menos partidas). */
function comandanteDestaque(linhas: EstatisticaComandante[] | undefined): EstatisticaComandante | undefined {
  return [...(linhas ?? [])]
    .filter((l) => l.vitorias > 0)
    .sort((a, b) => b.vitorias - a.vitorias || (b.percentualVitoria ?? 0) - (a.percentualVitoria ?? 0) || a.partidas - b.partidas)[0]
}

/** Página inicial: banner da temporada em andamento, comandantes em destaque e temporadas passadas. */
export function PaginaInicial() {
  const temporadas = useTemporadas()
  const aoVivo = useRodadasAtuais()
  const comandantes = useEstatisticasComandantes(null)

  const ativas = temporadas.data?.filter((t) => t.ativa) ?? []
  const passadas = temporadas.data?.filter((t) => !t.ativa) ?? []
  const temDadosComandantes = (comandantes.data ?? []).some((l) => l.partidas > 0 || l.decks > 0)

  return (
    <div className="space-y-10">
      {aoVivo.data && aoVivo.data.length > 0 && (
        <Link to="/ao-vivo" className="flex items-center gap-3 rounded-2xl border border-emerald-800 bg-emerald-950/40 p-4 transition hover:bg-emerald-950/70">
          <span className="relative flex size-3">
            <span className="absolute inline-flex size-full animate-ping rounded-full bg-emerald-400 opacity-60" />
            <span className="relative inline-flex size-3 rounded-full bg-emerald-400" />
          </span>
          <span className="flex-1">
            <span className="block font-semibold text-emerald-100">Rodada acontecendo agora</span>
            <span className="block text-sm text-emerald-300/80">Veja as mesas sorteadas e encontre seu lugar.</span>
          </span>
          <Radio className="size-5 text-emerald-300" aria-hidden />
        </Link>
      )}

      {temporadas.isPending && <Carregando />}
      {temporadas.error && <CaixaErro erro={temporadas.error} aoTentarDeNovo={() => temporadas.refetch()} />}

      {temporadas.data && temporadas.data.length === 0 && (
        <div className="flex flex-col items-center gap-4 py-10 text-center">
          <LogoLoja className="size-24" />
          <Vazio>Nenhuma temporada cadastrada ainda.</Vazio>
        </div>
      )}

      {ativas.length > 0 && (
        <section className="space-y-4" aria-label="Temporadas em andamento">
          {ativas.map((t) => <BannerTemporada key={t.id} temporada={t} />)}
        </section>
      )}

      {ativas.length > 0 && (
        <Link to="/regras" className="flex items-center gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/60 p-4 transition hover:border-zinc-600">
          <ScrollText className="size-6 text-amber-400" aria-hidden />
          <span className="flex-1">
            <span className="block font-semibold text-zinc-100">Regulamento da liga</span>
            <span className="block text-sm text-zinc-400">Limite de preço, proxies, pontuação, desempate e final.</span>
          </span>
          <ChevronRight className="size-5 text-zinc-500" aria-hidden />
        </Link>
      )}

      {temDadosComandantes && comandantes.data && (
        <>
          <section>
            <TituloSecao titulo="Comandantes mais jogados" subtitulo="todas as temporadas" link={{ para: '/comandantes', rotulo: 'Ver ranking completo' }} />
            <CartoesArteComandante linhas={comandantes.data} ordem="jogados" />
          </section>

          {/* grid-cols-1 = coluna "minmax(0, 1fr)": pode encolher até a largura da tela (sem ela, a coluna estica até o texto mais comprido). */}
          <section className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="cartao p-4">
              <TituloSecao titulo="Top comandantes" subtitulo="% de vitória" link={{ para: '/comandantes?ordem=percentual', rotulo: 'Ver todos' }} />
              <ListaComandantes linhas={comandantes.data} ordem="percentual" limite={5} minimoPartidas={3} />
            </div>
            <div className="cartao p-4">
              <TituloSecao titulo="Mais decks cadastrados" subtitulo="na loja" link={{ para: '/comandantes?ordem=decks', rotulo: 'Ver todos' }} />
              <ListaComandantes linhas={comandantes.data} ordem="decks" limite={5} />
            </div>
          </section>
        </>
      )}

      {passadas.length > 0 && (
        <section>
          <TituloSecao titulo="Temporadas passadas" />
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {passadas.map((t) => <CartaoTemporadaPassada key={t.id} temporada={t} />)}
          </div>
        </section>
      )}
    </div>
  )
}

function TituloSecao({ titulo, subtitulo, link }: { titulo: string; subtitulo?: string; link?: { para: string; rotulo: string } }) {
  return (
    <div className="mb-3 flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
      <h2 className="text-lg font-bold text-zinc-50">{titulo}</h2>
      {subtitulo && <span className="hidden text-sm text-zinc-400 sm:inline">· {subtitulo}</span>}
      {link && <Link to={link.para} className="ml-auto whitespace-nowrap text-sm font-medium text-amber-300 hover:underline">{link.rotulo}</Link>}
    </div>
  )
}

/**
 * Banner largo da temporada em andamento: arte do comandante com mais vitórias na temporada,
 * sombra por cima para o texto ficar legível e o botão principal.
 */
function BannerTemporada({ temporada }: { temporada: Temporada }) {
  const estatisticas = useEstatisticasComandantes(temporada.id)
  const ranking = useRanking(temporada.id)
  const destaque = comandanteDestaque(estatisticas.data)
  const lider = ranking.data?.[0]
  // Aviso da próxima etapa: some sozinho depois do dia marcado.
  const proximaEtapa = temporada.proximaEtapaEm && paraCampoDataHora(temporada.proximaEtapaEm).slice(0, 10) >= hojeIso()
    ? temporada.proximaEtapaEm : null

  return (
    <Link
      to={`/temporadas/${temporada.id}`}
      className="group relative isolate block min-h-[18rem] overflow-hidden rounded-3xl border border-zinc-800 shadow-2xl sm:min-h-[22rem]"
      aria-label={`${temporada.nome}: ver ranking da temporada`}
    >
      {/* fundo: arte do comandante em destaque, ou degradê nas cores da loja */}
      {destaque ? (
        <div className="absolute inset-0 -z-20">
          <ArteComandante comandantes={destaque.comandantes} className="size-full transition duration-700 group-hover:scale-105" arredondamento="rounded-none" />
        </div>
      ) : (
        <div className="absolute inset-0 -z-20 bg-[linear-gradient(135deg,var(--color-brand-navy),#0a1128_55%,var(--color-brand-red))]" />
      )}
      {/* sombra para o texto ler bem em qualquer arte (cores fixas nos dois temas) */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-r from-black/80 via-black/35 to-transparent" />
      <div className="absolute inset-0 -z-10 bg-gradient-to-t from-black/75 via-black/10 to-transparent" />

      <div className="flex h-full min-h-[18rem] flex-col justify-end gap-3 p-5 text-white sm:min-h-[22rem] sm:p-8">
        <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-brand-orange px-3 py-1 text-xs font-bold uppercase tracking-wider text-white shadow">
          <span className="size-1.5 animate-pulse rounded-full bg-white" aria-hidden /> Temporada em andamento
        </span>
        <h1 className="font-display text-3xl font-bold leading-tight drop-shadow-lg sm:text-5xl">{temporada.nome}</h1>
        <p className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-white/85">
          <span className="inline-flex items-center gap-1.5">
            <CalendarDays className="size-4" aria-hidden />
            {formatarData(temporada.dataInicio)}{temporada.dataFim ? ` – ${formatarData(temporada.dataFim)}` : ' – em aberto'}
          </span>
          <span>{temporada.quantidadeEtapas} etapa{temporada.quantidadeEtapas === 1 ? '' : 's'} · {temporada.quantidadeRodadas} rodada{temporada.quantidadeRodadas === 1 ? '' : 's'}</span>
        </p>

        {proximaEtapa && (
          <div className="flex w-fit flex-col gap-1 rounded-2xl bg-black/55 px-4 py-3 ring-1 ring-amber-400/40 backdrop-blur-sm sm:flex-row sm:items-center sm:gap-5">
            <span className="inline-flex items-center gap-2 text-sm">
              <CalendarClock className="size-5 text-amber-300" aria-hidden />
              <span>Próxima etapa marcada: <b className="text-base text-amber-200">{formatarDiaEHora(proximaEtapa)}</b></span>
            </span>
            {temporada.prazoListas && (
              <span className="inline-flex items-center gap-2 text-sm">
                <FileText className="size-5 text-amber-300" aria-hidden />
                <span>Prazo para enviar listas: <b className="text-base text-amber-200">{formatarData(temporada.prazoListas)}</b></span>
              </span>
            )}
          </div>
        )}

        <div className="flex flex-wrap gap-2 text-sm">
          {lider && (
            <span className="inline-flex items-center gap-1.5 rounded-xl bg-black/45 px-3 py-1.5 ring-1 ring-white/15 backdrop-blur-sm">
              <Trophy className="size-4 text-amber-300" aria-hidden /> Líder: <b>{lider.nomeJogador}</b> · {lider.pontos} pts
            </span>
          )}
          {destaque && (
            <span className="inline-flex items-center gap-1.5 rounded-xl bg-black/45 px-3 py-1.5 ring-1 ring-white/15 backdrop-blur-sm">
              <Crown className="size-4 text-amber-300" aria-hidden /> Destaque: <b>{destaque.nome}</b> · {destaque.vitorias} vitória{destaque.vitorias === 1 ? '' : 's'}
            </span>
          )}
        </div>

        <span className="mt-1 inline-flex w-fit items-center gap-2 rounded-xl bg-white px-5 py-2.5 text-sm font-bold text-[#0a1128] shadow-lg transition group-hover:bg-amber-50">
          Ver ranking da temporada <ChevronRight className="size-4 transition group-hover:translate-x-0.5" aria-hidden />
        </span>
      </div>
    </Link>
  )
}

function CartaoTemporadaPassada({ temporada }: { temporada: Temporada }) {
  return (
    <Link to={`/temporadas/${temporada.id}`} className="group flex items-center gap-4 rounded-2xl border border-zinc-800 bg-zinc-900/60 p-4 transition hover:border-zinc-600">
      <div className="min-w-0 flex-1">
        <div className="truncate font-display text-lg font-bold text-zinc-100">{temporada.nome}</div>
        <div className="mt-1 flex items-center gap-1.5 text-sm text-zinc-400">
          <CalendarDays className="size-4" aria-hidden />
          {formatarData(temporada.dataInicio)}{temporada.dataFim ? ` – ${formatarData(temporada.dataFim)}` : ''}
        </div>
        <div className="mt-1 text-xs text-zinc-500">{temporada.quantidadeEtapas} etapas · {temporada.quantidadeRodadas} rodadas</div>
      </div>
      <ChevronRight className="size-5 text-zinc-500 transition group-hover:translate-x-0.5 group-hover:text-zinc-300" aria-hidden />
    </Link>
  )
}
