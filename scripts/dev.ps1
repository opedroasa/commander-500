# Sobe o ambiente local completo: banco (Docker) + API (:5080) + site (:5173).
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File scripts\dev.ps1
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Yellow }
function Fail($msg) { Write-Host "`nERRO: $msg" -ForegroundColor Red; exit 1 }

Step 'Verificando ferramentas'
foreach ($tool in 'docker', 'dotnet', 'npm') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { Fail "'$tool' não encontrado. Veja os pré-requisitos no README." }
}
docker info --format '{{.ServerVersion}}' *> $null
if ($LASTEXITCODE -ne 0) { Fail 'O Docker não está rodando. Abra o Docker Desktop, espere ele iniciar e rode de novo.' }

Step 'Subindo o banco (Postgres no Docker)'
docker compose up -d --wait db
if ($LASTEXITCODE -ne 0) { Fail 'Não foi possível subir o banco. A porta 5432 pode estar em uso por outro Postgres.' }

Step 'Restaurando ferramentas .NET'
dotnet tool restore | Out-Null

if (-not (Test-Path (Join-Path $root 'web\node_modules'))) {
    Step 'Instalando dependências do site (primeira vez, pode demorar)'
    npm install --prefix web
    if ($LASTEXITCODE -ne 0) { Fail 'npm install falhou.' }
}

Step 'Abrindo a API e o site em janelas separadas'
Start-Process powershell -ArgumentList '-NoExit', '-Command', "`$host.UI.RawUI.WindowTitle='Commander 500 - API'; Set-Location '$root'; dotnet run --project src/Commander500.Api"
Start-Process powershell -ArgumentList '-NoExit', '-Command', "`$host.UI.RawUI.WindowTitle='Commander 500 - Site'; Set-Location '$root'; npm run dev --prefix web"

Step 'Aguardando a API ficar pronta'
$ready = $false
for ($i = 0; $i -lt 90; $i++) {
    Start-Sleep -Seconds 1
    try { Invoke-RestMethod http://localhost:5080/health -TimeoutSec 2 | Out-Null; $ready = $true; break } catch { }
}
if (-not $ready) { Fail 'A API não respondeu em 90 s. Veja a janela "Commander 500 - API".' }

Start-Process 'http://localhost:5173'
Write-Host "`nPronto!" -ForegroundColor Green
Write-Host '  Site:     http://localhost:5173'
Write-Host '  Admin:    http://localhost:5173/admin  (admin@fantasystore.local / admin123)'
Write-Host '  Swagger:  http://localhost:5080/swagger'
Write-Host "`nPara parar: feche as duas janelas. O banco continua no Docker (docker compose stop)."
