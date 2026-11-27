import { Check, ExternalLink, Pencil, Plus, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { useDecks, useExcluirDeck, useJogadores, useRevisarDeck } from '../../api/consultas'
import type { Deck, SituacaoDeck } from '../../api/tipos'
import { ArteComandante } from '../../componentes/ArteComandante'
import { CabecalhoPagina, CaixaErro, Campo, Carregando, ErroEmLinha, Janela, SeloSituacaoDeck, Vazio } from '../../componentes/comum'
import { JanelaDeck } from '../../componentes/JanelaDeck'
import { Paginacao } from '../../componentes/Paginacao'
import { usePaginacao } from '../../utilitarios/paginacao'
import { chaveBusca, nomeComApelido, rotuloSituacaoDeck } from '../../utilitarios/formatacao'

/** Todos os decks, com filtros e revisão (aprovar / reprovar). */
export function PaginaDecks() {
  const decks = useDecks()
  const jogadores = useJogadores()
  const excluir = useExcluirDeck()
  const [parametros, definirParametros] = useSearchParams()
  const filtroJogador = Number(parametros.get('jogador')) || 0
  const filtroSituacao = (parametros.get('situacao') ?? '') as SituacaoDeck | ''
  const [filtro, definirFiltro] = useState('')
  const [emEdicao, definirEmEdicao] = useState<Deck | 'novo' | null>(null)
  const [emRevisao, definirEmRevisao] = useState<Deck | null>(null)

  const busca = chaveBusca(filtro)
  const lista = (decks.data ?? [])
    .filter((d) => !filtroJogador || d.idJogador === filtroJogador)
    .filter((d) => !filtroSituacao || d.situacao === filtroSituacao)
    .filter((d) => !busca || chaveBusca(`${d.nome} ${d.nomeJogador} ${d.comandantes.map((c) => c.nome).join(' ')}`).includes(busca))
  const { itensDaPagina, estado } = usePaginacao(lista, `${busca}|${filtroJogador}|${filtroSituacao}`)

  if (decks.isPending || jogadores.isPending) return <Carregando />
  if (decks.error) return <CaixaErro erro={decks.error} aoTentarDeNovo={() => decks.refetch()} />
  if (jogadores.error) return <CaixaErro erro={jogadores.error} />

  function definirFiltroUrl(chave: string, valor: string) {
    const novos = new URLSearchParams(parametros)
    if (valor) novos.set(chave, valor)
    else novos.delete(chave)
    definirParametros(novos, { replace: true })
  }

  async function confirmarExclusao(deck: Deck) {
    if (confirm(`Excluir o deck "${deck.nome}" de ${deck.nomeJogador}?`)) await excluir.mutateAsync(deck.id)
  }

  const pendentes = decks.data.filter((d) => d.situacao === 'Pendente').length

  return (
    <>
      <CabecalhoPagina
        titulo="Decks"
        subtitulo={pendentes ? `${pendentes} aguardando aprovação` : 'Todos revisados'}
        acoes={<button className="botao-primario" onClick={() => definirEmEdicao('novo')}><Plus className="size-4" aria-hidden />Novo deck</button>}
      />
      <div className="mb-3 grid gap-2 sm:grid-cols-3">
        <input type="search" placeholder="Buscar deck, jogador ou comandante" value={filtro} onChange={(e) => definirFiltro(e.target.value)} />
        <select value={filtroJogador || ''} onChange={(e) => definirFiltroUrl('jogador', e.target.value)} aria-label="Filtrar por jogador">
          <option value="">Todos os jogadores</option>
          {jogadores.data.map((j) => <option key={j.id} value={j.id}>{nomeComApelido(j.nome, j.apelido)}</option>)}
        </select>
        <select value={filtroSituacao} onChange={(e) => definirFiltroUrl('situacao', e.target.value)} aria-label="Filtrar por situação">
          <option value="">Todas as situações</option>
          {(['Pendente', 'Aprovado', 'Reprovado'] as const).map((s) => <option key={s} value={s}>{rotuloSituacaoDeck[s]}</option>)}
        </select>
      </div>
      <ErroEmLinha erro={excluir.error} />
      {lista.length === 0 && <Vazio>Nenhum deck encontrado.</Vazio>}
      <ul className="space-y-2">
        {itensDaPagina.map((d) => (
          <li key={d.id} className="cartao p-3">
            <div className="flex flex-wrap items-start gap-x-3 gap-y-1">
              <ArteComandante comandantes={d.comandantes} className="h-14 w-16 shrink-0" arredondamento="rounded-lg" />
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-semibold">{d.nome}</span>
                  <SeloSituacaoDeck situacao={d.situacao} />
                </div>
                <div className="text-sm text-amber-100/80">{d.comandantes.map((c) => c.nome).join(' + ')}</div>
                <div className="text-xs text-zinc-400">{d.nomeJogador}</div>
                {d.observacaoRevisao && <div className="mt-1 text-xs text-zinc-400">Obs.: {d.observacaoRevisao}</div>}
              </div>
              <div className="flex items-center gap-1">
                {d.urlLista && (
                  <a href={d.urlLista} target="_blank" rel="noreferrer noopener" className="botao-discreto botao-pequeno" aria-label="Abrir lista">
                    <ExternalLink className="size-4" />
                  </a>
                )}
                <button className="botao-secundario botao-pequeno" onClick={() => definirEmRevisao(d)}>Revisar</button>
                <button className="botao-discreto botao-pequeno" onClick={() => definirEmEdicao(d)} aria-label="Editar"><Pencil className="size-4" /></button>
                <button className="botao-discreto botao-pequeno text-red-300" onClick={() => confirmarExclusao(d)} aria-label="Excluir"><Trash2 className="size-4" /></button>
              </div>
            </div>
          </li>
        ))}
      </ul>
      <Paginacao estado={estado} rotulo="decks" />
      {emEdicao !== null && (
        <JanelaDeck
          key={`deck-${emEdicao === 'novo' ? `novo-${filtroJogador}` : emEdicao.id}`}
          aberta
          deck={emEdicao === 'novo' ? null : emEdicao}
          idJogador={emEdicao === 'novo' && filtroJogador ? filtroJogador : undefined}
          aoFechar={() => definirEmEdicao(null)}
        />
      )}
      <JanelaRevisao key={`revisao-${emRevisao?.id ?? 'nenhum'}`} deck={emRevisao} aoFechar={() => definirEmRevisao(null)} />
    </>
  )
}

function JanelaRevisao({ deck, aoFechar }: { deck: Deck | null; aoFechar: () => void }) {
  const revisar = useRevisarDeck()
  const [observacao, definirObservacao] = useState(deck?.observacaoRevisao ?? '')

  async function definirSituacao(situacao: SituacaoDeck) {
    if (!deck) return
    await revisar.mutateAsync({ id: deck.id, situacao, observacao: observacao.trim() || null })
    aoFechar()
  }

  return (
    <Janela aberta={deck !== null} aoFechar={aoFechar} titulo={`Revisar: ${deck?.nome ?? ''}`}>
      {deck && (
        <div className="space-y-4">
          <p className="text-sm text-zinc-300">
            {deck.nomeJogador} · {deck.comandantes.map((c) => c.nome).join(' + ')}
            {deck.urlLista && <> · <a href={deck.urlLista} target="_blank" rel="noreferrer noopener" className="text-amber-300 hover:underline">ver lista</a></>}
          </p>
          <p className="text-xs text-zinc-500">O orçamento é conferido fora do sistema. A situação não bloqueia o uso do deck nas rodadas.</p>
          <Campo rotulo="Observação" dica="Obrigatória para reprovar.">
            <textarea rows={3} value={observacao} onChange={(e) => definirObservacao(e.target.value)} className="w-full" />
          </Campo>
          <ErroEmLinha erro={revisar.error} />
          <div className="flex flex-wrap justify-end gap-2">
            <button className="botao-discreto" onClick={() => definirSituacao('Pendente')} disabled={revisar.isPending}>Voltar a pendente</button>
            <button className="botao-perigo" onClick={() => definirSituacao('Reprovado')} disabled={revisar.isPending || !observacao.trim()}>
              <X className="size-4" aria-hidden /> Reprovar
            </button>
            <button className="botao-primario" onClick={() => definirSituacao('Aprovado')} disabled={revisar.isPending}>
              <Check className="size-4" aria-hidden /> Aprovar
            </button>
          </div>
        </div>
      )}
    </Janela>
  )
}
