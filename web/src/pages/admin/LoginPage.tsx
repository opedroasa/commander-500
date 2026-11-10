import { LogIn } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { useAuth } from '../../auth/AuthContext'
import { Field, InlineError } from '../../components/ui'

export function LoginPage() {
  const { auth, login } = useAuth()
  const navigate = useNavigate()
  const from = (useLocation().state as { from?: string } | null)?.from ?? '/admin'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  if (auth) return <Navigate to={from} replace />

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(email, password)
      navigate(from, { replace: true })
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mx-auto max-w-sm pt-6">
      <h1 className="mb-1 font-display text-2xl font-bold text-amber-100">Área do lojista</h1>
      <p className="mb-6 text-sm text-zinc-400">Entre para conduzir rodadas e gerenciar cadastros.</p>
      <form onSubmit={submit} className="card space-y-4 p-5">
        <Field label="E-mail">
          <input type="email" autoComplete="username" required value={email} onChange={(e) => setEmail(e.target.value)} className="w-full" />
        </Field>
        <Field label="Senha">
          <input type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} className="w-full" />
        </Field>
        <InlineError error={error} />
        <button className="btn-primary w-full" disabled={busy}>
          <LogIn className="size-4" aria-hidden /> {busy ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </div>
  )
}
