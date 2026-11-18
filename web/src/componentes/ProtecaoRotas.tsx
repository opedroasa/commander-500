import { Navigate, Outlet, useLocation } from 'react-router'
import { ProvedorAutenticacao, useAutenticacao } from '../autenticacao/ContextoAutenticacao'

/** Manda para o login (guardando para onde voltar) quando o admin não está logado. */
export function ExigirAdmin() {
  const { sessao } = useAutenticacao()
  const local = useLocation()
  if (!sessao) return <Navigate to="/admin/login" replace state={{ voltarPara: local.pathname + local.search }} />
  return <Outlet />
}

/** Rota raiz: o ProvedorAutenticacao precisa do cache de consultas e tem que ficar dentro do roteador. */
export function Raiz() {
  return (
    <ProvedorAutenticacao>
      <Outlet />
    </ProvedorAutenticacao>
  )
}
