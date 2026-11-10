import { LogOut, Radio, Shield, Trophy } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'

const navLink = ({ isActive }: { isActive: boolean }) =>
  `inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium transition ${
    isActive ? 'bg-zinc-800 text-amber-300' : 'text-zinc-300 hover:bg-zinc-900 hover:text-zinc-100'
  }`

export function PublicLayout() {
  const { auth } = useAuth()
  return (
    <div className="flex min-h-dvh flex-col">
      <header className="sticky top-0 z-20 border-b border-zinc-800/80 bg-zinc-950/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center gap-2 px-4 py-2">
          <NavLink to="/" className="mr-auto flex items-center gap-2 py-1">
            <span className="flex size-8 items-center justify-center rounded-lg bg-amber-500 font-display text-sm font-bold text-zinc-950">500</span>
            <span className="leading-tight">
              <span className="block whitespace-nowrap font-display text-sm font-bold tracking-wide text-amber-100">Commander 500</span>
              <span className="block text-[0.7rem] text-zinc-400">Fantasy Store</span>
            </span>
          </NavLink>
          <nav className="flex gap-1">
            <NavLink to="/" end className={navLink}><Trophy className="size-4" aria-hidden /><span className="hidden sm:inline">Temporadas</span></NavLink>
            <NavLink to="/ao-vivo" className={navLink}><Radio className="size-4" aria-hidden />Ao vivo</NavLink>
            <NavLink to="/admin" className={navLink} aria-label="Área do lojista">
              <Shield className="size-4" aria-hidden /><span className="hidden sm:inline">{auth ? 'Admin' : 'Lojista'}</span>
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t border-zinc-900 py-4 text-center text-xs text-zinc-500">
        Liga Commander 500 · Fantasy Store
      </footer>
    </div>
  )
}

const adminLinks = [
  { to: '/admin', label: 'Rodadas', end: true },
  { to: '/admin/jogadores', label: 'Jogadores' },
  { to: '/admin/decks', label: 'Decks' },
  { to: '/admin/comandantes', label: 'Comandantes' },
  { to: '/admin/temporadas', label: 'Temporadas' },
]

export function AdminLayout() {
  const { auth, logout } = useAuth()
  const navigate = useNavigate()
  return (
    <div>
      <div className="mb-5 flex items-center gap-2 border-b border-zinc-800 pb-3">
        <nav className="-mx-1 flex flex-1 gap-1 overflow-x-auto px-1">
          {adminLinks.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end} className={navLink}>{l.label}</NavLink>
          ))}
        </nav>
        <button
          className="btn-ghost btn-sm shrink-0"
          title={`Sair (${auth?.email})`}
          onClick={() => { logout(); navigate('/') }}
        >
          <LogOut className="size-4" aria-hidden /><span className="hidden sm:inline">Sair</span>
        </button>
      </div>
      <Outlet />
    </div>
  )
}
