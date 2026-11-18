import { LogIn } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { useAutenticacao } from '../../autenticacao/ContextoAutenticacao'
import { Campo, ErroEmLinha } from '../../componentes/comum'

export function PaginaLogin() {
  const { sessao, entrar } = useAutenticacao()
  const navegar = useNavigate()
  const voltarPara = (useLocation().state as { voltarPara?: string } | null)?.voltarPara ?? '/admin'
  const [email, definirEmail] = useState('')
  const [senha, definirSenha] = useState('')
  const [erro, definirErro] = useState<unknown>(null)
  const [enviando, definirEnviando] = useState(false)

  if (sessao) return <Navigate to={voltarPara} replace />

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    definirEnviando(true)
    definirErro(null)
    try {
      await entrar(email, senha)
      navegar(voltarPara, { replace: true })
    } catch (e) {
      definirErro(e)
    } finally {
      definirEnviando(false)
    }
  }

  return (
    <div className="mx-auto max-w-sm pt-6">
      <h1 className="mb-1 font-display text-2xl font-bold text-amber-100">Área do admin</h1>
      <p className="mb-6 text-sm text-zinc-400">Entre para conduzir rodadas e gerenciar cadastros.</p>
      <form onSubmit={enviar} className="cartao space-y-4 p-5">
        <Campo rotulo="E-mail">
          <input type="email" autoComplete="username" required value={email} onChange={(e) => definirEmail(e.target.value)} className="w-full" />
        </Campo>
        <Campo rotulo="Senha">
          <input type="password" autoComplete="current-password" required value={senha} onChange={(e) => definirSenha(e.target.value)} className="w-full" />
        </Campo>
        <ErroEmLinha erro={erro} />
        <button className="botao-primario w-full" disabled={enviando}>
          <LogIn className="size-4" aria-hidden /> {enviando ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </div>
  )
}
