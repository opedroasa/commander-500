import { Home, LogOut, Menu, Moon, Radio, ScrollText, Shield, Sparkles, Sun, Trophy, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { useConfiguracaoLoja } from '../api/consultas'
import { useAutenticacao } from '../autenticacao/ContextoAutenticacao'
import { useTema } from '../utilitarios/tema'
import { FundoAnimado } from './FundoAnimado'
import { IconeWhatsapp } from './IconeWhatsapp'

/** Quem fez o site (aparece no rodapé). */
const desenvolvedores = [
  { nome: 'Pedro Alexandre', linkedin: 'https://www.linkedin.com/in/pedro-antonio-alexandre/' },
  { nome: 'Murilo Reis', linkedin: 'https://www.linkedin.com/in/murilo-silva-reis-98b806230/' },
  { nome: 'Gabriel NB', linkedin: 'https://www.linkedin.com/in/gabriel-nascimento-656225292/' },
]

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

/** Itens do menu principal (no celular aparecem no menu lateral). */
const itensMenu = [
  { para: '/', rotulo: 'Início', icone: Home, exato: true },
  { para: '/comandantes', rotulo: 'Comandantes', icone: Sparkles },
  { para: '/hall-da-fama', rotulo: 'Hall da Fama', icone: Trophy },
  { para: '/regras', rotulo: 'Regras', icone: ScrollText },
  { para: '/ao-vivo', rotulo: 'Ao vivo', icone: Radio },
]

/** Moldura do site: faixa nas cores da logo, cabeçalho com menu, conteúdo e rodapé. */
export function LayoutPublico() {
  const { sessao } = useAutenticacao()
  const linkGrupo = useConfiguracaoLoja().data?.linkGrupoWhatsapp
  const [menuAberto, definirMenuAberto] = useState(false)
  const rotuloAdmin = sessao ? 'Área do admin' : 'Entrar como admin'
  return (
    <div className="flex min-h-dvh flex-col">
      <FundoAnimado />
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

          {/* Computador e tablet: menu em linha (só ícones até telas largas, com o nome no balão). */}
          <nav className="hidden gap-1 md:flex">
            {itensMenu.map(({ para, rotulo, icone: Icone, exato }) => (
              <NavLink key={para} to={para} end={exato} className={estiloLinkMenu} aria-label={rotulo} title={rotulo}>
                <Icone className="size-4" aria-hidden /><span className="hidden xl:inline">{rotulo}</span>
              </NavLink>
            ))}
            {linkGrupo && (
              <a href={linkGrupo} target="_blank" rel="noreferrer noopener" title="Grupo do Commander 500 no WhatsApp" aria-label="Grupo do WhatsApp"
                className="inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium text-zinc-300 transition hover:bg-zinc-900 hover:text-zinc-100">
                <IconeWhatsapp /><span className="hidden xl:inline">Grupo</span>
              </a>
            )}
            <NavLink to="/admin" className={estiloLinkMenu} aria-label={rotuloAdmin} title={rotuloAdmin}>
              <Shield className="size-4" aria-hidden /><span className="hidden xl:inline">Admin</span>
            </NavLink>
          </nav>
          <span className="hidden md:inline-flex"><BotaoTema /></span>

          {/* Celular: atalho do "Ao vivo" e o botão que abre o menu lateral com tudo. */}
          <NavLink to="/ao-vivo" className={(estado) => `${estiloLinkMenu(estado)} md:hidden`} aria-label="Ao vivo" title="Ao vivo">
            <Radio className="size-4" aria-hidden />
          </NavLink>
          <button type="button" className="rounded-lg p-2 text-zinc-200 transition hover:bg-zinc-800 md:hidden" aria-label="Abrir menu"
            aria-expanded={menuAberto} onClick={() => definirMenuAberto(true)}>
            <Menu className="size-6" />
          </button>
        </div>
      </header>
      {menuAberto && (
        <MenuLateral linkGrupo={linkGrupo ?? null} rotuloAdmin={rotuloAdmin} aoFechar={() => definirMenuAberto(false)} />
      )}
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="space-y-2 border-t border-zinc-900 bg-zinc-950/80 px-4 py-5 text-center text-xs text-zinc-500">
        <div className="flex items-center justify-center gap-2">
          <LogoLoja className="size-7" />
          <span>Liga Commander 500 · Fantasy Store</span>
        </div>
        {linkGrupo && (
          <a href={linkGrupo} target="_blank" rel="noreferrer noopener"
            className="inline-flex items-center gap-1.5 rounded-full border border-zinc-800 px-3 py-1 text-sm text-zinc-200 hover:border-zinc-600">
            <IconeWhatsapp />Entre no grupo do Commander 500 no WhatsApp
          </a>
        )}
        <p>
          Desenvolvido por{' '}
          {desenvolvedores.map((pessoa, indice) => (
            <span key={pessoa.nome}>
              <a href={pessoa.linkedin} target="_blank" rel="noreferrer noopener" title={`LinkedIn de ${pessoa.nome}`}
                className="font-medium text-zinc-300 hover:text-amber-300 hover:underline">{pessoa.nome}</a>
              {indice < desenvolvedores.length - 2 ? ', ' : indice === desenvolvedores.length - 2 ? ' e ' : ''}
            </span>
          ))}
        </p>
        <p>
          Dados e imagens de cartas: <a href="https://scryfall.com" target="_blank" rel="noreferrer noopener" className="hover:text-zinc-300 hover:underline">Scryfall</a>.
          Magic: The Gathering é marca da Wizards of the Coast.
        </p>
      </footer>
    </div>
  )
}

/** Menu lateral do celular: todos os links com nome, o grupo do WhatsApp e o tema. Fecha ao escolher, no X, no fundo ou com Esc. */
function MenuLateral({ linkGrupo, rotuloAdmin, aoFechar }: { linkGrupo: string | null; rotuloAdmin: string; aoFechar: () => void }) {
  useEffect(() => {
    const aoTeclar = (evento: KeyboardEvent) => { if (evento.key === 'Escape') aoFechar() }
    document.addEventListener('keydown', aoTeclar)
    // Trava a rolagem da página enquanto o menu está aberto.
    const rolagemAnterior = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => { document.removeEventListener('keydown', aoTeclar); document.body.style.overflow = rolagemAnterior }
  }, [aoFechar])

  const estiloItem = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-3 rounded-xl px-3 py-3 text-base font-medium transition ${
      isActive ? 'bg-zinc-800 text-amber-300' : 'text-zinc-200 hover:bg-zinc-900'
    }`

  return (
    <div className="fixed inset-0 z-40 md:hidden" role="dialog" aria-modal="true" aria-label="Menu">
      <button type="button" className="absolute inset-0 bg-black/60 backdrop-blur-sm" aria-label="Fechar menu" onClick={aoFechar} />
      <nav className="menu-lateral absolute inset-y-0 right-0 flex w-72 max-w-[85vw] flex-col gap-1 overflow-y-auto border-l border-zinc-800 bg-zinc-950 p-4 shadow-2xl">
        <div className="mb-2 flex items-center gap-2">
          <LogoLoja className="size-9" />
          <span className="font-display text-sm font-bold text-amber-100">Commander 500</span>
          <button type="button" className="ml-auto rounded-lg p-2 text-zinc-300 hover:bg-zinc-800" aria-label="Fechar menu" onClick={aoFechar}>
            <X className="size-5" />
          </button>
        </div>
        {itensMenu.map(({ para, rotulo, icone: Icone, exato }) => (
          <NavLink key={para} to={para} end={exato} className={estiloItem} onClick={aoFechar}>
            <Icone className="size-5 text-amber-400" aria-hidden />{rotulo}
          </NavLink>
        ))}
        {linkGrupo && (
          <a href={linkGrupo} target="_blank" rel="noreferrer noopener" onClick={aoFechar}
            className="flex items-center gap-3 rounded-xl px-3 py-3 text-base font-medium text-zinc-200 transition hover:bg-zinc-900">
            <IconeWhatsapp className="size-5" />Grupo no WhatsApp
          </a>
        )}
        <NavLink to="/admin" className={estiloItem} onClick={aoFechar}>
          <Shield className="size-5 text-amber-400" aria-hidden />{rotuloAdmin}
        </NavLink>
        <div className="mt-auto flex items-center justify-between border-t border-zinc-800 pt-3 text-sm text-zinc-400">
          Tema <BotaoTema />
        </div>
      </nav>
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
  { para: '/admin/configuracoes', rotulo: 'Configurações' },
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
