// Cliente HTTP: todas as chamadas para a API passam por aqui.
// - manda o token de login (Authorization: Bearer) quando o admin está logado
// - transforma o erro da API (ProblemDetails com "detail") em ErroApi com a mensagem em português

/** Endereço da API. Vazio em desenvolvimento (o Vite repassa /api) e na Vercel (site e Api no mesmo endereço). Só preencha VITE_API_URL se a Api ficar em outro domínio. */
const ENDERECO_BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

const CHAVE_SESSAO = 'c500.sessao'

export interface SessaoSalva {
  token: string
  email: string
  expiraEm: string
}

export function carregarSessao(): SessaoSalva | null {
  try {
    const texto = localStorage.getItem(CHAVE_SESSAO)
    if (!texto) return null
    const sessao = JSON.parse(texto) as SessaoSalva
    return new Date(sessao.expiraEm).getTime() > Date.now() ? sessao : null
  } catch {
    return null
  }
}

export function salvarSessao(sessao: SessaoSalva | null) {
  try {
    if (sessao) localStorage.setItem(CHAVE_SESSAO, JSON.stringify(sessao))
    else localStorage.removeItem(CHAVE_SESSAO)
  } catch {
    /* navegador sem armazenamento: o login vale só enquanto a página estiver aberta */
  }
}

export class ErroApi extends Error {
  readonly status: number
  constructor(status: number, mensagem: string) {
    super(mensagem)
    this.status = status
  }
}

/** Chamado quando uma requisição autenticada volta 401 (token vencido). */
let aoExpirarSessao: (() => void) | null = null
export function definirAoExpirarSessao(funcao: (() => void) | null) {
  aoExpirarSessao = funcao
}

let tokenAtual: string | null = carregarSessao()?.token ?? null
export function definirToken(token: string | null) {
  tokenAtual = token
}

const ROTA_LOGIN = '/api/autenticacao/login'

export async function requisitar<T>(caminho: string, opcoes: { metodo?: string; corpo?: unknown } = {}): Promise<T> {
  const cabecalhos: Record<string, string> = { Accept: 'application/json' }
  // FormData (envio de foto): o navegador monta o Content-Type com o "boundary" sozinho.
  const ehFormulario = opcoes.corpo instanceof FormData
  if (opcoes.corpo !== undefined && !ehFormulario) cabecalhos['Content-Type'] = 'application/json'
  if (tokenAtual) cabecalhos.Authorization = `Bearer ${tokenAtual}`

  let resposta: Response
  try {
    resposta = await fetch(`${ENDERECO_BASE}${caminho}`, {
      method: opcoes.metodo ?? 'GET',
      headers: cabecalhos,
      body: opcoes.corpo === undefined ? undefined : ehFormulario ? (opcoes.corpo as FormData) : JSON.stringify(opcoes.corpo),
    })
  } catch {
    throw new ErroApi(0, 'Não foi possível conectar ao servidor. Ele pode estar acordando — tente de novo em alguns segundos.')
  }

  if (resposta.status === 401 && tokenAtual && caminho !== ROTA_LOGIN) aoExpirarSessao?.()

  if (!resposta.ok) {
    let mensagem = `Erro ${resposta.status}`
    try {
      const problema = await resposta.json()
      mensagem = problema.detail || problema.title || mensagem
    } catch {
      /* resposta sem JSON */
    }
    if (resposta.status === 401 && caminho !== ROTA_LOGIN) mensagem = 'Sessão expirada. Entre novamente.'
    throw new ErroApi(resposta.status, mensagem)
  }

  if (resposta.status === 204) return undefined as T
  return (await resposta.json()) as T
}
