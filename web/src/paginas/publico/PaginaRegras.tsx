import DOMPurify from 'dompurify'
import { Pencil } from 'lucide-react'
import { Link } from 'react-router'
import { useRegulamento } from '../../api/consultas'
import { useAutenticacao } from '../../autenticacao/ContextoAutenticacao'
import { CaixaErro, Carregando, Vazio } from '../../componentes/comum'
import { formatarDataHora } from '../../utilitarios/formatacao'

/** Regulamento da liga (o "tratado"), público. O lojista edita em /admin/regras. */
export function PaginaRegras() {
  const regulamento = useRegulamento()
  const { sessao } = useAutenticacao()
  if (regulamento.isPending) return <Carregando />
  if (regulamento.error) return <CaixaErro erro={regulamento.error} aoTentarDeNovo={() => regulamento.refetch()} />
  const { conteudo, alteradoEm } = regulamento.data

  return (
    <article className="mx-auto max-w-3xl">
      {sessao && (
        <div className="mb-4 flex justify-end">
          <Link to="/admin/regras" className="botao-secundario botao-pequeno"><Pencil className="size-4" aria-hidden />Editar regras</Link>
        </div>
      )}
      {conteudo.trim()
        // O HTML vem do editor do admin; mesmo assim passa pelo DOMPurify antes de entrar na página.
        ? <div className="texto-regulamento" dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(conteudo) }} />
        : <Vazio>O regulamento ainda não foi publicado.</Vazio>}
      {alteradoEm && <p className="mt-8 text-xs text-zinc-500">Atualizado em {formatarDataHora(alteradoEm)}.</p>}
    </article>
  )
}
