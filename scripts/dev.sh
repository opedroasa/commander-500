#!/usr/bin/env bash
# Sobe o ambiente local completo (macOS/Linux): banco (Docker) + API (:5080) + site (:5173).
# Uso (na raiz do projeto):  ./scripts/dev.sh      — Ctrl+C para parar API e site.
set -euo pipefail
cd "$(dirname "$0")/.."

for tool in docker dotnet npm; do
  command -v "$tool" >/dev/null || { echo "ERRO: '$tool' não encontrado. Veja os pré-requisitos no README."; exit 1; }
done
docker info >/dev/null 2>&1 || { echo "ERRO: o Docker não está rodando."; exit 1; }

echo "==> Subindo o banco"
docker compose up -d --wait db
dotnet tool restore >/dev/null
[ -d web/node_modules ] || { echo "==> Instalando dependências do site"; npm install --prefix web; }

echo "==> Iniciando API e site"
dotnet run --project src/Commander500.Api &
API_PID=$!
npm run dev --prefix web &
WEB_PID=$!
trap 'kill $API_PID $WEB_PID 2>/dev/null' EXIT INT TERM

for _ in $(seq 1 90); do curl -sf http://localhost:5080/health >/dev/null && break; sleep 1; done
echo
echo "Pronto!  Site: http://localhost:5173   Admin: /admin (admin@fantasystore.local / admin123)   Swagger: http://localhost:5080/swagger"
wait
