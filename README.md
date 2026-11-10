# Commander 500 · Fantasy Store

Site da liga de **Commander 500** (Magic: The Gathering) da Fantasy Store: ranking da temporada, estatísticas de comandantes, sorteio de mesas com timer ao vivo e área do lojista para conduzir as rodadas.

- **Regras e escopo:** [SPEC.md](SPEC.md)
- **Convenções, arquitetura e decisões:** [CLAUDE.md](CLAUDE.md)
- **Publicação (Neon + Render + Vercel), para depois:** [docs/DEPLOY.md](docs/DEPLOY.md)

| Parte | Tecnologia | Pasta |
|---|---|---|
| API | ASP.NET Core (.NET 9), EF Core, PostgreSQL | `src/Commander500.Api` |
| Testes | xUnit | `tests/Commander500.Tests` |
| Site | React 19, Vite, TypeScript, Tailwind | `web/` |
| Banco local | Postgres 16 no Docker | `docker-compose.yml` |

---

## 1. Pré-requisitos (uma vez só)

Instale no computador:

| Ferramenta | Versão | Windows (PowerShell) |
|---|---|---|
| Git | qualquer recente | `winget install Git.Git` |
| .NET SDK | **9.x** | `winget install Microsoft.DotNet.SDK.9` |
| Node.js | **22 LTS** (mín. 20.19) | `winget install OpenJS.NodeJS.LTS` |
| Docker Desktop | qualquer recente | `winget install Docker.DockerDesktop` |

Depois de instalar, **feche e abra o terminal** e confira:

```bash
dotnet --version
```

```bash
node --version
```

```bash
docker --version
```

No macOS, use os instaladores dos sites oficiais (ou Homebrew). Editor sugerido: VS Code (com as extensões C# Dev Kit e Tailwind CSS) ou Visual Studio 2022.

---

## 2. Baixar o projeto

```bash
git clone <url-do-repositorio>
```

```bash
cd <pasta-do-repositorio>
```

---

## 3. Rodar tudo com um comando

1. Abra o **Docker Desktop** e espere aparecer "Engine running".
2. Na raiz do projeto:

**Windows:**

```bash
powershell -ExecutionPolicy Bypass -File scripts\dev.ps1
```

**macOS / Linux:**

```bash
./scripts/dev.sh
```

O script sobe o banco, instala as dependências na primeira vez, abre a API e o site e abre o navegador em <http://localhost:5173>. No Windows, a API e o site rodam em duas janelas próprias; para parar, feche-as.

| Endereço | O que é |
|---|---|
| <http://localhost:5173> | Site (público) |
| <http://localhost:5173/admin> | Área do lojista: **admin@fantasystore.local** / **admin123** |
| <http://localhost:5173/ao-vivo> | Mesas e timer da rodada em andamento (para o telão / celular) |
| <http://localhost:5080/swagger> | Documentação interativa da API |

Na primeira vez a API cria as tabelas e os **dados de exemplo**: "Temporada 1" com 10 jogadores, decks (alguns pendentes/reprovados, incluindo parceiros e background) e 3 rodadas encerradas, então ranking e estatísticas já aparecem preenchidos.

---

## 4. Rodar passo a passo (sem o script)

Útil para depurar. Use três terminais, todos na raiz do projeto.

**Terminal 1 — banco:**

```bash
docker compose up -d --wait db
```

**Terminal 2 — API** (aplica migrations e o seed ao iniciar):

```bash
dotnet tool restore
```

```bash
dotnet run --project src/Commander500.Api
```

**Terminal 3 — site** (o Vite repassa `/api` para a API em `:5080`):

```bash
npm install --prefix web
```

```bash
npm run dev --prefix web
```

No Visual Studio / Rider, abra `Commander500.sln` e rode o perfil **http** do projeto `Commander500.Api`.

---

## 5. Comandos do dia a dia

| Para… | Comando |
|---|---|
| Rodar os testes | `dotnet test` |
| Checar o site (tipos + build) | `npm run build --prefix web` |
| Lint do site | `npm run lint --prefix web` |
| Parar o banco (mantém os dados) | `docker compose stop` |
| **Zerar o banco e recriar os exemplos** | `docker compose down -v` e rodar de novo |
| Criar migration após mudar uma entidade | `dotnet ef migrations add NomeDaMudanca --project src/Commander500.Api --output-dir Data/Migrations` |

---

## 6. Roteiro de demonstração para o lojista

1. **Site público** (<http://localhost:5173>): temporada → **Ranking**, **Comandantes** (duplas de parceiros contam como uma linha) e **Rodadas** (histórico com vencedores).
2. **Lojista → Rodadas → Nova rodada.**
3. **Presentes:** marque os jogadores e confira o deck de cada um (aviso para deck pendente/reprovado) → **Salvar e sortear**.
4. **Sorteio:** mesas desenhadas com as 4 cadeiras (cadeira 1 começa). Mostre o **Sortear novamente** e que quem já teve bye na temporada não repete.
5. Abra **Ao vivo** em outra aba ou no celular (mesma rede: `http://<ip-do-pc>:5173/ao-vivo`, veja abaixo) e use "Encontre sua mesa".
6. **Iniciar rodada:** o timer aparece igual em todas as telas, e recarregar a página não reinicia.
7. Toque no vencedor de cada mesa e confirme, ou marque **Empate** → **Encerrar rodada** → o ranking atualiza.
8. **Cadastros:** jogadores, decks (aprovar/reprovar com observação), comandantes e a **Pontuação** da temporada (trava após a 1ª rodada encerrada).

**Abrir no celular durante a demo:** rode o site com `npm run dev --prefix web -- --host` e acesse `http://<ip-do-computador>:5173` com o celular no mesmo Wi-Fi (o IP aparece no terminal do Vite). O Windows pode pedir permissão no firewall.

---

## 7. Problemas comuns

| Sintoma | Solução |
|---|---|
| `O Docker não está rodando` / erro de `dockerDesktopLinuxEngine` | Abra o Docker Desktop e espere iniciar |
| Banco não sobe: porta **5432** em uso | Outro Postgres está rodando na máquina; pare-o ou mude a porta em `docker-compose.yml` **e** em `appsettings.Development.json` |
| Porta **5080** ou **5173** em uso | Feche a janela antiga da API/site (ou finalize o processo `dotnet`/`node`) |
| Script `.ps1` bloqueado ("execução de scripts foi desabilitada") | Use exatamente `powershell -ExecutionPolicy Bypass -File scripts\dev.ps1` |
| Site abre mas mostra "Não foi possível conectar ao servidor" | A API não está rodando; veja a janela/terminal da API |
| Quero os dados de exemplo de volta | `docker compose down -v` e rode de novo |
| `dotnet ef` não encontrado | `dotnet tool restore` |

---

## 8. Trabalhando em dupla

- Crie uma branch por tarefa (`git switch -c feature/nome`) e abra Pull Request para a `main`.
- Antes de subir: `dotnet test` e `npm run build --prefix web`.
- Mudou entidade? Gere a migration e faça commit dela junto.
- Regras de negócio ficam em `src/Commander500.Api/Domain/Services` com teste em `tests/` (ver [CLAUDE.md](CLAUDE.md)).
