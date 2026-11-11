import { Home, LogOut, Moon, Radio, Shield, Sparkles, Sun } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { useTheme } from '../lib/theme'

const navLink = ({ isActive }: { isActive: boolean }) =>
  `inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg px-2.5 py-2 text-sm font-medium transition sm:px-3 ${
    isActive ? 'bg-zinc-800 text-amber-300' : 'text-zinc-300 hover:bg-zinc-900 hover:text-zinc-100'
  }`

/** Store logo on a white badge (the original has a white background). */
export function StoreLogo({ className = 'size-11' }: { className?: string }) {
  return (
    <span className={`inline-flex shrink-0 items-center justify-center overflow-hidden rounded-xl bg-white p-0.5 shadow-md ring-1 ring-black/10 ${className}`}>
      <img src="/logo-fantasy-store.png" alt="Fantasy Store" className="size-full object-contain" />
    </span>
  )
}

function ThemeToggle() {
  const { theme, toggle } = useTheme()
  const label = theme === 'dark' ? 'Usar tema claro' : 'Usar tema escuro'
  return (
    <button type="button" onClick={toggle} className="rounded-lg p-2 text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-100" aria-label={label} title={label}>
      {theme === 'dark' ? <Sun className="size-5" /> : <Moon className="size-5" />}
    </button>
  )
}

export function PublicLayout() {
  const { auth } = useAuth()
  return (
    <div className="flex min-h-dvh flex-col">
      {/* brand stripe in the logo colors */}
      <div className="h-1 bg-[linear-gradient(90deg,var(--color-brand-red),var(--color-brand-orange)_50%,var(--color-brand-navy))]" aria-hidden />
      <header className="sticky top-0 z-20 border-b border-zinc-800/80 bg-zinc-950/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center gap-1 px-4 py-2 sm:gap-2">
          <NavLink to="/" className="mr-auto flex min-w-0 items-center gap-2.5 py-0.5">
            <StoreLogo />
            <span className="hidden leading-tight min-[420px]:block">
              <span className="block whitespace-nowrap font-display text-sm font-bold tracking-wide text-amber-100">Commander 500</span>
              <span className="block text-[0.7rem] font-semibold uppercase tracking-widest text-amber-400">Fantasy Store</span>
            </span>
          </NavLink>
          <nav className="flex gap-0.5 sm:gap-1">
            <NavLink to="/" end className={navLink} aria-label="Início"><Home className="size-4" aria-hidden /><span className="hidden md:inline">Início</span></NavLink>
            <NavLink to="/comandantes" className={navLink} aria-label="Comandantes"><Sparkles className="size-4" aria-hidden /><span className="hidden md:inline">Comandantes</span></NavLink>
            <NavLink to="/ao-vivo" className={navLink}><Radio className="size-4" aria-hidden /><span className="hidden sm:inline">Ao vivo</span></NavLink>
            <NavLink to="/admin" className={navLink} aria-label={auth ? 'Área do admin' : 'Entrar como admin'}>
              <Shield className="size-4" aria-hidden /><span className="hidden md:inline">Admin</span>
            </NavLink>
          </nav>
          <ThemeToggle />
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t border-zinc-900 py-5 text-center text-xs text-zinc-500">
        <div className="mb-1 flex items-center justify-center gap-2">
          <StoreLogo className="size-7" />
          <span>Liga Commander 500 · Fantasy Store</span>
        </div>
        Dados e imagens de cartas: <a href="https://scryfall.com" target="_blank" rel="noreferrer noopener" className="hover:text-zinc-300 hover:underline">Scryfall</a>.
        Magic: The Gathering é marca da Wizards of the Coast.
      </footer>
    </div>
  )
}

const adminLinks = [
  { to: '/admin', label: 'Rodadas', end: true },
  { to: '/admin/jogadores', label: 'Jogadores' },
  { to: '/admin/decks', label: 'Decks' },
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
