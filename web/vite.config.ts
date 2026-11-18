import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Em desenvolvimento, /api e /saude são repassados para a Api local (dotnet run → :5080),
// então VITE_API_URL pode ficar vazio. Em produção, VITE_API_URL = endereço da Api.
// Se a Api estiver em outra porta (ex.: 5080 ocupada por outro projeto), rode com ALVO_API=http://localhost:5081.
const alvoApi = process.env.ALVO_API ?? 'http://localhost:5080'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': alvoApi,
      '/saude': alvoApi,
    },
  },
})
