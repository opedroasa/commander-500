import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { api, loadAuth, saveAuth, setToken, setUnauthorizedHandler, type StoredAuth } from '../api/client'
import type { LoginResponse } from '../api/types'

interface AuthState {
  auth: StoredAuth | null
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<StoredAuth | null>(() => loadAuth())
  const qc = useQueryClient()

  const logout = useCallback(() => {
    setToken(null)
    saveAuth(null)
    setAuth(null)
    qc.removeQueries({ queryKey: ['players'] })
    qc.removeQueries({ queryKey: ['decks'] })
    qc.removeQueries({ queryKey: ['commanders'] })
  }, [qc])

  const login = useCallback(async (email: string, password: string) => {
    const res = await api<LoginResponse>('/api/auth/login', { method: 'POST', body: { email, password } })
    const next = { token: res.token, email: res.email, expiresAt: res.expiresAt }
    setToken(next.token)
    saveAuth(next)
    setAuth(next)
  }, [])

  // Expired/invalid token on any admin call → drop the session.
  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  // Auto-logout when the token expires while the page is open.
  useEffect(() => {
    if (!auth) return
    const ms = new Date(auth.expiresAt).getTime() - Date.now()
    const t = window.setTimeout(logout, Math.max(0, Math.min(ms, 2 ** 31 - 1)))
    return () => window.clearTimeout(t)
  }, [auth, logout])

  const value = useMemo(() => ({ auth, login, logout }), [auth, login, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
