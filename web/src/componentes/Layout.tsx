import { Home, LogOut, Moon, Radio, ScrollText, Shield, Sparkles, Sun } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { useAutenticacao } from '../autenticacao/ContextoAutenticacao'
import { useTema } from '../utilitarios/tema'

const estiloLinkMenu = ({ isActive }: { isActive: boolean }) =>
  `inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg px-2.5 py-2 text-sm font-medium transition sm:px-3 ${
    isActive ? 'bg-zinc-800 text-amber-300' : 'text-zinc-300 hover:bg-zinc-900 hover:text-zinc-100'
  }`

/** Logo da loja num selo branco (a imagem original tem fundo branco). */
export function LogoLoja({ className = 'size-11' }: { className?: string }) {
  return (
    <span className={`inline-flex shrink-0 items-center justify-center overflow-hidden rounded-xl bg-white p-0.5 shadow-md ring-1 ring-black/10 ${className}`}>
      <img src="/logo-fantasy-store.png" alt="Fantasy Store" className="size-full object-contain" />
    </span>
  )
}

function BotaoTema() {
  const { tema, alternar } = useTema()
  const descricao = tema === 'escuro' ? 'Usar tema claro' : 'Usar tema escuro'
  return (
    <button type="button" onClick={alternar} className="rounded-lg p-2 text-zinc-300 transition hover:bg-zinc-800 hover:text-zinc-100" aria-label={descricao} title={descricao}>
      {tema === 'escuro' ? <Sun className="size-5" /> : <Moon className="size-5" />}
    </button>
  )
}

/** Moldura do site: faixa nas cores da logo, cabeçalho com menu, conteúdo e rodapé. */
export function LayoutPublico() {
  const { sessao } = useAutenticacao()
  return (
    <div className="flex min-h-dvh flex-col">
      <div className="h-1 bg-[linear-gradient(90deg,var(--color-brand-red),var(--color-brand-orange)_50%,var(--color-brand-navy))]" aria-hidden />
      <header className="sticky top-0 z-20 border-b border-zinc-800/80 bg-zinc-950/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center gap-1 px-4 py-2 sm:gap-2">
          <NavLink to="/" className="mr-auto flex min-w-0 items-center gap-2.5 py-0.5">
            <LogoLoja />
            <span className="hidden leading-tight min-[420px]:block">
              <span className="block whitespace-nowrap font-display text-sm font-bold tracking-wide text-amber-100">Commander 500</span>
              <span className="block text-[0.7rem] font-semibold uppercase tracking-widest text-amber-400">Fantasy Store</span>
            </span>
          </NavLink>
          <nav className="flex gap-0.5 sm:gap-1">
            <NavLink to="/" end className={estiloLinkMenu} aria-label="Início"><Home className="size-4" aria-hidden /><span className="hidden md:inline">Início</span></NavLink>
            <NavLink to="/comandantes" className={estiloLinkMenu} aria-label="Comandantes"><Sparkles className="size-4" aria-hidden /><span className="hidden md:inline">Comandantes</span></NavLink>
            <NavLink to="/regras" className={estiloLinkMenu} aria-label="Regras"><ScrollText className="size-4" aria-hidden /><span className="hidden md:inline">Regras</span></NavLink>
            <NavLink to="/ao-vivo" className={estiloLinkMenu}><Radio className="size-4" aria-hidden /><span className="hidden sm:inline">Ao vivo</span></NavLink>
            <NavLink to="/admin" className={estiloLinkMenu} aria-label={sessao ? 'Área do admin' : 'Entrar como admin'}>
              <Shield className="size-4" aria-hidden /><span className="hidden md:inline">Admin</span>
            </NavLink>
          </nav>
          <BotaoTema />
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t border-zinc-900 py-5 text-center text-xs text-zinc-500">
        <div className="mb-1 flex items-center justify-center gap-2">
          <LogoLoja className="size-7" />
          <span>Liga Commander 500 · Fantasy Store</span>
        </div>
        Dados e imagens de cartas: <a href="https://scryfall.com" target="_blank" rel="noreferrer noopener" className="hover:text-zinc-300 hover:underline">Scryfall</a>.
        Magic: The Gathering é marca da Wizards of the Coast.
      </footer>
    </div>
  )
}

const linksAdmin = [
  { para: '/admin', rotulo: 'Rodadas', exato: true },
  { para: '/admin/jogadores', rotulo: 'Jogadores' },
  { para: '/admin/decks', rotulo: 'Decks' },
  { para: '/admin/temporadas', rotulo: 'Temporadas' },
  { para: '/admin/regras', rotulo: 'Regras' },
  { para: '/admin/administradores', rotulo: 'Administradores' },
]

/** Menu da área do admin (dentro do LayoutPublico). */
export function LayoutAdmin() {
  const { sessao, sair } = useAutenticacao()
  const navegar = useNavigate()
  return (
    <div>
      <div className="mb-5 flex items-center gap-2 border-b border-zinc-800 pb-3">
        <nav className="-mx-1 flex flex-1 gap-1 overflow-x-auto px-1">
          {linksAdmin.map((link) => (
            <NavLink key={link.para} to={link.para} end={link.exato} className={estiloLinkMenu}>{link.rotulo}</NavLink>
          ))}
        </nav>
        <button className="botao-discreto botao-pequeno shrink-0" title={`Sair (${sessao?.email})`} onClick={() => { sair(); navegar('/') }}>
          <LogOut className="size-4" aria-hidden /><span className="hidden sm:inline">Sair</span>
        </button>
      </div>
      <Outlet />
    </div>
  )
}
