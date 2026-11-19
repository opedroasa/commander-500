# Sobe o ambiente local completo: banco (Docker) + Api (:5080) + site (:5173).
# A Api abre o site sozinha (SpaProxy, perfil "http"), igual ao F5 do Visual Studio.
# Uso (na raiz do projeto):  powershell -ExecutionPolicy Bypass -File scripts\dev.ps1
$ErrorActionPreference = 'Continue'
$raiz = Split-Path $PSScriptRoot -Parent
Set-Location $raiz

function Passo($mensagem) { Write-Host "`n==> $mensagem" -ForegroundColor Yellow }
function Falhar($mensagem) { Write-Host "`nERRO: $mensagem" -ForegroundColor Red; exit 1 }

Passo 'Verificando ferramentas'
foreach ($ferramenta in 'docker', 'dotnet', 'npm') {
    if (-not (Get-Command $ferramenta -ErrorAction SilentlyContinue)) { Falhar "'$ferramenta' não encontrado. Veja os pré-requisitos no README." }
}
docker info --format '{{.ServerVersion}}' *> $null
if ($LASTEXITCODE -ne 0) { Falhar 'O Docker não está rodando. Abra o Docker Desktop, espere ele iniciar e rode de novo.' }

# A porta 5080 também é usada por outros projetos (ex.: Gestão Rastreio Wiki). Se estiver ocupada, a Api não sobe.
$ocupaPorta = Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($ocupaPorta) {
    $processo = (Get-Process -Id $ocupaPorta.OwningProcess -ErrorAction SilentlyContinue).ProcessName
    Falhar "A porta 5080 já está em uso por '$processo'. Feche esse programa (ou a outra Api) e rode de novo."
}

Passo 'Subindo o banco (Postgres no Docker)'
docker compose up -d --wait banco
if ($LASTEXITCODE -ne 0) { Falhar 'Não foi possível subir o banco. A porta 5432 pode estar em uso por outro Postgres.' }

Passo 'Restaurando ferramentas .NET'
dotnet tool restore | Out-Null

if (-not (Test-Path (Join-Path $raiz 'web\node_modules'))) {
    Passo 'Instalando dependências do site (primeira vez, pode demorar)'
    npm install --prefix web
    if ($LASTEXITCODE -ne 0) { Falhar 'npm install falhou.' }
}

Passo 'Abrindo a Api (ela abre o site sozinha)'
Start-Process powershell -ArgumentList '-NoExit', '-Command', "`$host.UI.RawUI.WindowTitle='Commander 500 - Api + Site'; Set-Location '$raiz'; dotnet run --project src/Commander500.Api --launch-profile http"

Passo 'Aguardando a Api ficar pronta'
$pronta = $false
for ($i = 0; $i -lt 90; $i++) {
    Start-Sleep -Seconds 1
    try { Invoke-RestMethod http://localhost:5080/saude -TimeoutSec 2 | Out-Null; $pronta = $true; break } catch { }
}
if (-not $pronta) { Falhar 'A Api não respondeu em 90 s. Veja a janela "Commander 500 - Api".' }

Start-Process 'http://localhost:5173'
Write-Host "`nPronto!" -ForegroundColor Green
Write-Host '  Site:     http://localhost:5173'
Write-Host '  Admin:    http://localhost:5173/admin  (admin@fantasystore.local / admin123)'
Write-Host '  Swagger:  http://localhost:5080/swagger'
Write-Host "`nPara parar: feche a janela 'Commander 500 - Api + Site' (o site para junto). O banco continua no Docker (docker compose stop)."
