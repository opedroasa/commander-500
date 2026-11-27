import { Medal, Search, Trophy } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { useJogadoresPublicos } from '../../api/consultas'
import { ArteComandante } from '../../componentes/ArteComandante'
import { CabecalhoPagina, CaixaErro, Carregando, LegendaSiglas, Vazio } from '../../componentes/comum'
import { Paginacao } from '../../componentes/Paginacao'
import { usePaginacao } from '../../utilitarios/paginacao'
import { chaveBusca, formatarPercentual, nomeComApelido } from '../../utilitarios/formatacao'

/** Lista pública de jogadores da liga. Tocar num jogador abre a página completa dele. */
export function PaginaListaJogadores() {
  const jogadores = useJogadoresPublicos()
  const [busca, definirBusca] = useState('')
  const termo = chaveBusca(busca)
  const filtrados = (jogadores.data ?? []).filter((j) => !termo || chaveBusca(`${j.nome} ${j.apelido ?? ''}`).includes(termo))
  const { itensDaPagina, estado } = usePaginacao(filtrados, termo)

  return (
    <>
      <CabecalhoPagina titulo="Jogadores" subtitulo="Quem joga a Liga Commander 500. Toque num jogador para ver tudo sobre ele." />
      <div className="relative mb-4 max-w-md">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-500" aria-hidden />
        <input type="search" placeholder="Buscar jogador" value={busca} onChange={(e) => definirBusca(e.target.value)} className="w-full pl-9" />
      </div>

      {jogadores.isPending && <Carregando />}
      {jogadores.error && <CaixaErro erro={jogadores.error} aoTentarDeNovo={() => jogadores.refetch()} />}
      {jogadores.data && filtrados.length === 0 && <Vazio>Nenhum jogador encontrado.</Vazio>}

      {itensDaPagina.length > 0 && (
        <>
          <ul className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
            {itensDaPagina.map((j) => (
              <li key={j.id}>
                <Link to={`/jogadores/${j.id}`} className="cartao flex items-center gap-3 p-3 transition hover:border-zinc-600">
                  <ArteComandante comandantes={j.comandantesPrincipais} className="h-14 w-20 shrink-0" arredondamento="rounded-lg" />
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center gap-1.5 font-semibold text-zinc-100">
                      <span className="break-words">{nomeComApelido(j.nome, j.apelido)}</span>
                      {j.titulos > 0 && <span className="inline-flex shrink-0 items-center gap-0.5 text-xs text-amber-300" title="Títulos de temporada"><Trophy className="size-3.5" aria-hidden />{j.titulos}</span>}
                      {j.etapasVencidas > 0 && <span className="inline-flex shrink-0 items-center gap-0.5 text-xs text-zinc-300" title="Etapas vencidas"><Medal className="size-3.5 text-amber-400" aria-hidden />{j.etapasVencidas}</span>}
                    </span>
                    <span className="block text-xs text-zinc-400">
                      {j.partidas} partida{j.partidas === 1 ? '' : 's'} · {j.vitorias} V · {formatarPercentual(j.percentualVitoria)} de vitória
                    </span>
                    {j.comandantesPrincipais.length > 0 && (
                      <span className="block text-xs leading-snug text-zinc-500 break-words">{j.comandantesPrincipais.map((c) => c.nome).join(' + ')}</span>
                    )}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
          <Paginacao estado={estado} rotulo="jogadores" />
          <LegendaSiglas className="mt-3" itens={[['🏆', 'títulos de temporada'], ['🏅', 'etapas vencidas'], ['V', 'vitórias'], ['Arte', 'comandante do deck que ele mais jogou']]} />
        </>
      )}
    </>
  )
}
