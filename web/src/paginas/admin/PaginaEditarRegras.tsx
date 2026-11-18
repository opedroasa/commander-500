import { Eye, Save } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { useRegulamento, useSalvarRegulamento } from '../../api/consultas'
import { CabecalhoPagina, CaixaErro, Carregando, ErroEmLinha } from '../../componentes/comum'
import { EditorRegulamento } from '../../componentes/EditorRegulamento'
import { formatarDataHora } from '../../utilitarios/formatacao'

/** Edição do regulamento (tratado) no editor visual. Os jogadores veem o resultado em /regras. */
export function PaginaEditarRegras() {
  const regulamento = useRegulamento()
  if (regulamento.isPending) return <Carregando />
  if (regulamento.error) return <CaixaErro erro={regulamento.error} aoTentarDeNovo={() => regulamento.refetch()} />
  return <Formulario conteudoInicial={regulamento.data.conteudo} alteradoEm={regulamento.data.alteradoEm} alteradoPor={regulamento.data.alteradoPor} />
}

function Formulario({ conteudoInicial, alteradoEm, alteradoPor }: { conteudoInicial: string; alteradoEm: string | null; alteradoPor: string | null }) {
  const salvar = useSalvarRegulamento()
  const [conteudo, definirConteudo] = useState(conteudoInicial)
  const [salvoAgora, definirSalvoAgora] = useState(false)
  const alterado = conteudo !== conteudoInicial

  async function gravar() {
    await salvar.mutateAsync(conteudo)
    definirSalvoAgora(true)
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Regras"
        subtitulo={alteradoEm ? `Última alteração: ${formatarDataHora(alteradoEm)}${alteradoPor ? ` · ${alteradoPor}` : ''}` : 'Ainda não publicado'}
        acoes={
          <>
            <Link to="/regras" target="_blank" className="botao-secundario"><Eye className="size-4" aria-hidden />Ver página</Link>
            <button className="botao-primario" onClick={gravar} disabled={salvar.isPending || !alterado}>
              <Save className="size-4" aria-hidden />{salvar.isPending ? 'Salvando…' : 'Salvar'}
            </button>
          </>
        }
      />
      <ErroEmLinha erro={salvar.error} />
      {salvoAgora && !alterado && <p className="mb-3 text-sm text-emerald-300">Salvo! Os jogadores já veem a versão nova.</p>}
      <EditorRegulamento conteudoInicial={conteudoInicial} aoMudar={(html) => { definirConteudo(html); definirSalvoAgora(false) }} />
      <p className="mt-2 text-xs text-zinc-500">Dica: clique dentro de uma tabela para aparecerem os botões de linha e coluna.</p>
    </>
  )
}
