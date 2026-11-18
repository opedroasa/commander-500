#!/usr/bin/env bash
# Sobe o ambiente local completo (macOS/Linux): banco (Docker) + Api (:5080) + site (:5173).
# Uso (na raiz do projeto):  ./scripts/dev.sh      — Ctrl+C para parar Api e site.
set -euo pipefail
cd "$(dirname "$0")/.."

for ferramenta in docker dotnet npm; do
  command -v "$ferramenta" >/dev/null || { echo "ERRO: '$ferramenta' não encontrado. Veja os pré-requisitos no README."; exit 1; }
done
docker info >/dev/null 2>&1 || { echo "ERRO: o Docker não está rodando."; exit 1; }

echo "==> Subindo o banco"
docker compose up -d --wait banco
dotnet tool restore >/dev/null
[ -d web/node_modules ] || { echo "==> Instalando dependências do site"; npm install --prefix web; }

echo "==> Iniciando Api e site"
dotnet run --project src/Commander500.Api &
PID_API=$!
npm run dev --prefix web &
PID_SITE=$!
trap 'kill $PID_API $PID_SITE 2>/dev/null' EXIT INT TERM

for _ in $(seq 1 90); do curl -sf http://localhost:5080/saude >/dev/null && break; sleep 1; done
echo
echo "Pronto!  Site: http://localhost:5173   Admin: /admin (admin@fantasystore.local / admin123)   Swagger: http://localhost:5080/swagger"
wait
