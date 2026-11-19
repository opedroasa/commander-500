// Ponto de entrada do site: cache de consultas, rotas e telas.
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { ErroApi } from './api/cliente'
import { Vazio } from './componentes/comum'
import { LayoutAdmin, LayoutPublico } from './componentes/Layout'
import { ExigirAdmin, Raiz } from './componentes/ProtecaoRotas'
import './index.css'
import { PaginaConducaoRodada } from './paginas/admin/PaginaConducaoRodada'
import { PaginaDecks } from './paginas/admin/PaginaDecks'
import { PaginaEtapasRodadas } from './paginas/admin/PaginaEtapasRodadas'
import { PaginaJogadores } from './paginas/admin/PaginaJogadores'
import { PaginaLogin } from './paginas/admin/PaginaLogin'
import { PaginaTemporadas } from './paginas/admin/PaginaTemporadas'
import { PaginaComandantes } from './paginas/publico/PaginaComandantes'
import { PaginaInicial } from './paginas/publico/PaginaInicial'
import { PaginaAoVivo, PaginaRodada } from './paginas/publico/PaginasAoVivo'
import { PaginaRegras } from './paginas/publico/PaginaRegras'
import { PaginaTemporada } from './paginas/publico/PaginaTemporada'

const cacheConsultas = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 10_000,
      // A Api no plano grátis (Vercel) desliga quando fica parada e leva alguns segundos para ligar: tenta de novo em erro de rede, nunca em erro 4xx.
      retry: (tentativas, erro) => (erro instanceof ErroApi && erro.status >= 400 && erro.status < 500 ? false : tentativas < 3),
      retryDelay: (tentativa) => Math.min(1000 * 2 ** tentativa, 8000),
    },
  },
})

const rotas = createBrowserRouter([
  {
    element: <Raiz />,
    children: [
      {
        element: <LayoutPublico />,
        children: [
          { path: '/', element: <PaginaInicial /> },
          { path: '/temporadas/:id', element: <PaginaTemporada /> },
          { path: '/comandantes', element: <PaginaComandantes /> },
          { path: '/ao-vivo', element: <PaginaAoVivo /> },
          { path: '/rodadas/:id', element: <PaginaRodada /> },
          { path: '/regras', element: <PaginaRegras /> },
          { path: '/admin/login', element: <PaginaLogin /> },
          {
            element: <ExigirAdmin />,
            children: [
              {
                element: <LayoutAdmin />,
                children: [
                  { path: '/admin', element: <PaginaEtapasRodadas /> },
                  { path: '/admin/rodadas/:id', element: <PaginaConducaoRodada /> },
                  { path: '/admin/jogadores', element: <PaginaJogadores /> },
                  { path: '/admin/decks', element: <PaginaDecks /> },
                  { path: '/admin/temporadas', element: <PaginaTemporadas /> },
                  // O editor visual (TipTap) é pesado: só baixa quando o admin abre a tela de regras.
                  { path: '/admin/regras', lazy: async () => ({ Component: (await import('./paginas/admin/PaginaEditarRegras')).PaginaEditarRegras }) },
                ],
              },
            ],
          },
          { path: '*', element: <Vazio>Página não encontrada.</Vazio> },
        ],
      },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={cacheConsultas}>
      <RouterProvider router={rotas} />
    </QueryClientProvider>
  </StrictMode>,
)
