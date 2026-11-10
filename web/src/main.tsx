import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { ApiError } from './api/client'
import { AdminLayout, PublicLayout } from './components/Layout'
import { RequireAdmin, Root } from './components/RouteGuards'
import { Empty } from './components/ui'
import './index.css'
import { AdminHomePage } from './pages/admin/AdminHomePage'
import { CommandersPage } from './pages/admin/CommandersPage'
import { DecksPage } from './pages/admin/DecksPage'
import { LoginPage } from './pages/admin/LoginPage'
import { PlayersPage } from './pages/admin/PlayersPage'
import { RoundConductPage } from './pages/admin/RoundConductPage'
import { SeasonsAdminPage } from './pages/admin/SeasonsAdminPage'
import { LivePage, RoundPage } from './pages/public/LivePages'
import { SeasonDetailPage } from './pages/public/SeasonDetailPage'
import { SeasonsPage } from './pages/public/SeasonsPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 10_000,
      // Render's free tier cold-starts: retry network errors a few times, never 4xx.
      retry: (count, err) => (err instanceof ApiError && err.status >= 400 && err.status < 500 ? false : count < 3),
      retryDelay: (n) => Math.min(1000 * 2 ** n, 8000),
    },
  },
})

const router = createBrowserRouter([
  {
    element: <Root />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          { path: '/', element: <SeasonsPage /> },
          { path: '/temporadas/:id', element: <SeasonDetailPage /> },
          { path: '/ao-vivo', element: <LivePage /> },
          { path: '/rodadas/:id', element: <RoundPage /> },
          { path: '/admin/login', element: <LoginPage /> },
          {
            element: <RequireAdmin />,
            children: [
              {
                element: <AdminLayout />,
                children: [
                  { path: '/admin', element: <AdminHomePage /> },
                  { path: '/admin/rodadas/:id', element: <RoundConductPage /> },
                  { path: '/admin/jogadores', element: <PlayersPage /> },
                  { path: '/admin/decks', element: <DecksPage /> },
                  { path: '/admin/comandantes', element: <CommandersPage /> },
                  { path: '/admin/temporadas', element: <SeasonsAdminPage /> },
                ],
              },
            ],
          },
          { path: '*', element: <Empty>Página não encontrada.</Empty> },
        ],
      },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
)
