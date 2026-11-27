import { Search, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { useRodada, useRodadasAtuais } from '../../api/consultas'
import type { Rodada } from '../../api/tipos'
import { CabecalhoPagina, CaixaErro, Carregando, SeloSituacaoRodada, Vazio } from '../../componentes/comum'
import { QuadroRodada } from '../../componentes/QuadroRodada'
import { chaveBusca, formatarData } from '../../utilitarios/formatacao'

const INTERVALO_ATUALIZACAO_MS = 10_000

/** Busca "encontre sua mesa": destaca os jogadores cujo nome combina, em todas as rodadas exibidas. */
function useBuscaLugar(rodadas: Rodada[] | undefined) {
  const [texto, definirTexto] = useState('')
  const destacados = useMemo(() => {
    const busca = chaveBusca(texto)
    const ids = new Set<number>()
    if (busca.length < 2 || !rodadas) return ids
    for (const rodada of rodadas)
      for (const p of rodada.participacoes)
        if (chaveBusca(`${p.nomeJogador} ${p.apelido ?? ''}`).includes(busca)) ids.add(p.idJogador)
    return ids
  }, [texto, rodadas])

  const campo = (
    <div className="relative mb-5">
      <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
      <input
        type="search"
        value={texto}
        onChange={(e) => definirTexto(e.target.value)}
        placeholder="Encontre sua mesa: digite seu nome"
        className="w-full pl-9 pr-9"
        aria-label="Buscar jogador"
      />
      {texto && (
        <button className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-zinc-400 hover:text-zinc-100" onClick={() => definirTexto('')} aria-label="Limpar busca">
          <X className="size-4" />
        </button>
      )}
      {texto.length >= 2 && (
        <p className="mt-1.5 text-xs text-zinc-400">
          {destacados.size === 0 ? 'Nenhum jogador encontrado.' : `${destacados.size} jogador${destacados.size > 1 ? 'es' : ''} destacado${destacados.size > 1 ? 's' : ''}.`}
        </p>
      )}
    </div>
  )
  return { destacados, campo }
}

/** /ao-vivo — todas as rodadas sorteadas / em andamento das temporadas ativas. */
export function PaginaAoVivo() {
  const rodadas = useRodadasAtuais(INTERVALO_ATUALIZACAO_MS)
  const { destacados, campo } = useBuscaLugar(rodadas.data)

  return (
    <>
      <CabecalhoPagina titulo="Ao vivo" subtitulo="Mesas sorteadas e tempo da rodada." />
      {rodadas.isPending && <Carregando />}
      {rodadas.error && <CaixaErro erro={rodadas.error} aoTentarDeNovo={() => rodadas.refetch()} />}
      {rodadas.data?.length === 0 && (
        <Vazio>
          Nenhuma rodada acontecendo agora. <Link to="/" className="text-amber-300 hover:underline">Ver temporadas</Link>
        </Vazio>
      )}
      {rodadas.data && rodadas.data.length > 0 && campo}
      <div className="space-y-10">
        {rodadas.data?.map((r) => (
          <section key={r.id}>
            <TituloRodada rodada={r} />
            <QuadroRodada rodada={r} idsJogadoresDestacados={destacados} />
          </section>
        ))}
      </div>
    </>
  )
}

/** /rodadas/:id — uma rodada (ao vivo enquanto aberta, resultados depois de encerrada). */
export function PaginaRodada() {
  const id = Number(useParams().id)
  const rodada = useRodada(id, INTERVALO_ATUALIZACAO_MS)
  const dados = rodada.data
  const { destacados, campo } = useBuscaLugar(dados ? [dados] : undefined)

  if (rodada.isPending) return <Carregando />
  if (rodada.error) return <CaixaErro erro={rodada.error} aoTentarDeNovo={() => rodada.refetch()} />
  const r = rodada.data

  return (
    <>
      <TituloRodada rodada={r} tituloDaPagina />
      {r.situacao === 'Rascunho' ? (
        <Vazio>As mesas desta rodada ainda não foram sorteadas.</Vazio>
      ) : (
        <>
          {r.situacao !== 'Encerrada' && campo}
          <QuadroRodada rodada={r} idsJogadoresDestacados={destacados} />
        </>
      )}
    </>
  )
}

function TituloRodada({ rodada, tituloDaPagina = false }: { rodada: Rodada; tituloDaPagina?: boolean }) {
  const Titulo = tituloDaPagina ? 'h1' : 'h2'
  return (
    <div className="mb-4 flex flex-wrap items-center gap-x-3 gap-y-1">
      <Titulo className="font-display text-2xl font-bold text-amber-100">{rodada.etapaFinal ? 'Mesa final' : `Etapa ${rodada.numeroEtapa}`} · Rodada {rodada.numeroNaEtapa}</Titulo>
      <SeloSituacaoRodada situacao={rodada.situacao} />
      <span className="w-full text-sm text-zinc-400 sm:w-auto">
        <Link to={`/temporadas/${rodada.idTemporada}`} className="hover:text-zinc-200 hover:underline">{rodada.nomeTemporada}</Link> · {formatarData(rodada.data)}
      </span>
    </div>
  )
}
