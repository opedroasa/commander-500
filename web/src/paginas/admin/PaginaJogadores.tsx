import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useExcluirJogador, useJogadores, useSalvarJogador } from '../../api/consultas'
import type { Jogador } from '../../api/tipos'
import { CabecalhoPagina, CaixaErro, Campo, Carregando, ErroEmLinha, Janela, Vazio } from '../../componentes/comum'
import { JanelaPerfilJogador } from '../../componentes/JanelaPerfilJogador'
import { Paginacao } from '../../componentes/Paginacao'
import { usePaginacao } from '../../utilitarios/paginacao'
import { chaveBusca } from '../../utilitarios/formatacao'

/** Cadastro de jogadores. Clicar no jogador abre o mini perfil, onde se cadastram os decks. */
export function PaginaJogadores() {
  const jogadores = useJogadores()
  const excluir = useExcluirJogador()
  const [emEdicao, definirEmEdicao] = useState<Jogador | 'novo' | null>(null)
  const [filtro, definirFiltro] = useState('')
  const [mostrarInativos, definirMostrarInativos] = useState(false)
  const [idPerfil, definirIdPerfil] = useState<number | null>(null)

  const busca = chaveBusca(filtro)
  const lista = (jogadores.data ?? [])
    .filter((j) => mostrarInativos || j.ativo)
    .filter((j) => !busca || chaveBusca(`${j.nome} ${j.apelido ?? ''}`).includes(busca))
  const { itensDaPagina, estado } = usePaginacao(lista, `${busca}|${mostrarInativos}`)

  if (jogadores.isPending) return <Carregando />
  if (jogadores.error) return <CaixaErro erro={jogadores.error} aoTentarDeNovo={() => jogadores.refetch()} />

  async function confirmarExclusao(jogador: Jogador) {
    if (confirm(`Excluir ${jogador.nome}? Os decks dele também serão excluídos.`)) await excluir.mutateAsync(jogador.id)
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Jogadores"
        subtitulo={`${jogadores.data.filter((j) => j.ativo).length} ativos`}
        acoes={<button className="botao-primario" onClick={() => definirEmEdicao('novo')}><Plus className="size-4" aria-hidden />Novo jogador</button>}
      />
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <input type="search" placeholder="Buscar" value={filtro} onChange={(e) => definirFiltro(e.target.value)} className="min-w-0 flex-1" />
        <label className="flex items-center gap-2 text-sm text-zinc-300">
          <input type="checkbox" checked={mostrarInativos} onChange={(e) => definirMostrarInativos(e.target.checked)} /> Mostrar inativos
        </label>
      </div>
      <ErroEmLinha erro={excluir.error} />
      {lista.length === 0 && <Vazio>Nenhum jogador.</Vazio>}
      <ul className="space-y-1.5">
        {itensDaPagina.map((j) => (
          <li key={j.id} className={`cartao flex items-center gap-3 p-1.5 pl-3 ${j.ativo ? '' : 'opacity-60'}`}>
            <button className="min-w-0 flex-1 rounded-lg py-1.5 text-left hover:text-amber-300" onClick={() => definirIdPerfil(j.id)}
              aria-label={`Abrir perfil de ${j.nome}`}>
              <div className="truncate font-medium">
                {j.nome}
                {j.apelido && <span className="text-zinc-400"> ({j.apelido})</span>}
                {!j.ativo && <span className="ml-2 rounded bg-zinc-800 px-1.5 py-0.5 text-xs text-zinc-400">inativo</span>}
              </div>
              <div className="text-xs text-zinc-400">
                {j.quantidadeDecks} deck{j.quantidadeDecks === 1 ? '' : 's'} · ver perfil e decks
              </div>
            </button>
            <button className="botao-discreto botao-pequeno" onClick={() => definirEmEdicao(j)} aria-label={`Editar ${j.nome}`}><Pencil className="size-4" /></button>
            <button className="botao-discreto botao-pequeno text-red-300" onClick={() => confirmarExclusao(j)} aria-label={`Excluir ${j.nome}`}><Trash2 className="size-4" /></button>
          </li>
        ))}
      </ul>
      <Paginacao estado={estado} rotulo="jogadores" />
      <JanelaJogador key={`jogador-${emEdicao === 'novo' ? 'novo' : emEdicao?.id ?? 'nenhum'}`} jogador={emEdicao} aoFechar={() => definirEmEdicao(null)} />
      <JanelaPerfilJogador idJogador={idPerfil} admin aoFechar={() => definirIdPerfil(null)} />
    </>
  )
}

function JanelaJogador({ jogador, aoFechar }: { jogador: Jogador | 'novo' | null; aoFechar: () => void }) {
  const salvar = useSalvarJogador()
  const existente = jogador && jogador !== 'novo' ? jogador : null
  const [nome, definirNome] = useState(existente?.nome ?? '')
  const [apelido, definirApelido] = useState(existente?.apelido ?? '')
  const [ativo, definirAtivo] = useState(existente?.ativo ?? true)

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    await salvar.mutateAsync({ id: existente?.id, dados: { nome, apelido: apelido || null, ativo } })
    aoFechar()
  }

  return (
    <Janela aberta={jogador !== null} aoFechar={aoFechar} titulo={existente ? 'Editar jogador' : 'Novo jogador'}>
      <form onSubmit={enviar} className="space-y-4">
        <Campo rotulo="Nome"><input required value={nome} onChange={(e) => definirNome(e.target.value)} className="w-full" autoFocus /></Campo>
        <Campo rotulo="Apelido (opcional)"><input value={apelido} onChange={(e) => definirApelido(e.target.value)} className="w-full" /></Campo>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={ativo} onChange={(e) => definirAtivo(e.target.checked)} /> Ativo (aparece na lista de presentes)
        </label>
        <ErroEmLinha erro={salvar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={salvar.isPending}>Salvar</button>
        </div>
      </form>
    </Janela>
  )
}
