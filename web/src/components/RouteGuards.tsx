import { Navigate, Outlet, useLocation } from 'react-router'
import { AuthProvider, useAuth } from '../auth/AuthContext'

/** Redirects to the login page (remembering where to come back) when not signed in. */
export function RequireAdmin() {
  const { auth } = useAuth()
  const location = useLocation()
  if (!auth) return <Navigate to="/admin/login" replace state={{ from: location.pathname + location.search }} />
  return <Outlet />
}

/** Root route: AuthProvider needs the query client and must sit inside the router. */
export function Root() {
  return (
    <AuthProvider>
      <Outlet />
    </AuthProvider>
  )
}
