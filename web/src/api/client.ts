/** API origin. Empty in dev (Vite proxies /api). In production: VITE_API_URL=https://<api>.onrender.com */
const BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

const TOKEN_KEY = 'c500.auth'

export interface StoredAuth {
  token: string
  email: string
  expiresAt: string
}

export function loadAuth(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(TOKEN_KEY)
    if (!raw) return null
    const auth = JSON.parse(raw) as StoredAuth
    return new Date(auth.expiresAt).getTime() > Date.now() ? auth : null
  } catch {
    return null
  }
}

export function saveAuth(auth: StoredAuth | null) {
  try {
    if (auth) localStorage.setItem(TOKEN_KEY, JSON.stringify(auth))
    else localStorage.removeItem(TOKEN_KEY)
  } catch {
    /* storage unavailable: session-only login */
  }
}

export class ApiError extends Error {
  readonly status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/** Called when an authenticated request returns 401 (expired token). */
let onUnauthorized: (() => void) | null = null
export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler
}

let currentToken: string | null = loadAuth()?.token ?? null
export function setToken(token: string | null) {
  currentToken = token
}

export async function api<T>(path: string, init: { method?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (init.body !== undefined) headers['Content-Type'] = 'application/json'
  if (currentToken) headers.Authorization = `Bearer ${currentToken}`

  let res: Response
  try {
    res = await fetch(`${BASE}${path}`, {
      method: init.method ?? 'GET',
      headers,
      body: init.body === undefined ? undefined : JSON.stringify(init.body),
    })
  } catch {
    throw new ApiError(0, 'Não foi possível conectar ao servidor. Ele pode estar acordando — tente de novo em alguns segundos.')
  }

  if (res.status === 401 && currentToken && path !== '/api/auth/login') onUnauthorized?.()

  if (!res.ok) {
    let message = `Erro ${res.status}`
    try {
      const problem = await res.json()
      message = problem.detail || problem.title || message
    } catch {
      /* not JSON */
    }
    if (res.status === 401 && path !== '/api/auth/login') message = 'Sessão expirada. Entre novamente.'
    throw new ApiError(res.status, message)
  }

  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}
