import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { carregarSessao, definirAoExpirarSessao, definirToken, requisitar, salvarSessao, type SessaoSalva } from '../api/cliente'
import type { LoginResposta } from '../api/tipos'

interface EstadoAutenticacao {
  sessao: SessaoSalva | null
  entrar: (email: string, senha: string) => Promise<void>
  sair: () => void
}

const ContextoAutenticacao = createContext<EstadoAutenticacao | null>(null)

/** Guarda o login do admin (token JWT) e o compartilha com as telas. */
export function ProvedorAutenticacao({ children }: { children: ReactNode }) {
  const [sessao, definirSessao] = useState<SessaoSalva | null>(() => carregarSessao())
  const cache = useQueryClient()

  const sair = useCallback(() => {
    definirToken(null)
    salvarSessao(null)
    definirSessao(null)
    cache.removeQueries({ queryKey: ['jogadores'] })
    cache.removeQueries({ queryKey: ['decks'] })
  }, [cache])

  const entrar = useCallback(async (email: string, senha: string) => {
    const resposta = await requisitar<LoginResposta>('/api/autenticacao/login', { metodo: 'POST', corpo: { email, senha } })
    const nova = { token: resposta.token, email: resposta.email, expiraEm: resposta.expiraEm }
    definirToken(nova.token)
    salvarSessao(nova)
    definirSessao(nova)
  }, [])

  // Token vencido/inválido em qualquer chamada do admin → encerra a sessão.
  useEffect(() => {
    definirAoExpirarSessao(sair)
    return () => definirAoExpirarSessao(null)
  }, [sair])

  // Sai sozinho quando o token vence com a página aberta.
  useEffect(() => {
    if (!sessao) return
    const ms = new Date(sessao.expiraEm).getTime() - Date.now()
    const temporizador = window.setTimeout(sair, Math.max(0, Math.min(ms, 2 ** 31 - 1)))
    return () => window.clearTimeout(temporizador)
  }, [sessao, sair])

  const valor = useMemo(() => ({ sessao, entrar, sair }), [sessao, entrar, sair])
  return <ContextoAutenticacao.Provider value={valor}>{children}</ContextoAutenticacao.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAutenticacao() {
  const contexto = useContext(ContextoAutenticacao)
  if (!contexto) throw new Error('useAutenticacao precisa estar dentro do ProvedorAutenticacao')
  return contexto
}
