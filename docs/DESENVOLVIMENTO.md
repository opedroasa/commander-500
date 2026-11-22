# Commander 500 · Guia de desenvolvimento

> Manual de quem mantém o projeto: como instalar, rodar, depurar, publicar e trabalhar em dupla. A apresentação do projeto está no [README](../README.md).

Site da liga de **Commander 500** (Magic: The Gathering) da Fantasy Store: ranking da temporada, estatísticas de comandantes, sorteio de mesas com timer ao vivo e área do lojista para conduzir as rodadas.

- **Regras e escopo:** [SPEC.md](../SPEC.md)
- **Convenções, arquitetura e decisões:** [CLAUDE.md](../CLAUDE.md)
- **Publicação (Vercel + Supabase):** [docs/DEPLOY.md](DEPLOY.md)
- **Manual para continuar o projeto (acessos e dia a dia):** [docs/MANUAL-PEDRO.md](MANUAL-PEDRO.md)

| Parte | Tecnologia | Pasta | Em produção |
|---|---|---|---|
| Api (arquitetura limpa) | ASP.NET Core (.NET 9), EF Core, PostgreSQL | `src/Commander500.Dominio`, `.Aplicacao`, `.Infraestrutura`, `.Api` | Vercel (container Docker) |
| Site | React 19, Vite, TypeScript, Tailwind | `web/` (projeto `Commander500.Web` na solução) | Vercel |
| Testes | xUnit | `tests/Commander500.Testes` | GitHub Actions |
| Banco | PostgreSQL | `docker-compose.yml` (local) | Supabase |

### Uma solução só

Abrindo `Commander500.sln` no Visual Studio você vê tudo junto, como na Gestão Rastreio Wiki:

```
Solução 'Commander500' (6 projetos)
└─ src
   ├─ Commander500.Api              ← projeto de inicialização (F5)
   ├─ Commander500.Aplicacao
   ├─ Commander500.Dominio
   ├─ Commander500.Infraestrutura
   ├─ Commander500.Testes
   └─ Commander500.Web              ← o site (pasta web/)
```

**Um play só:** ao iniciar a `Commander500.Api` (perfil **http**), ela sobe o banco no Docker, roda `npm run dev` na pasta
`web/` e abre o navegador direto no site. Quem faz isso é o pacote **SpaProxy** (configurado no `Commander500.Api.csproj`).
Ao parar a Api, o site para junto.

---

## 1. Pré-requisitos (uma vez só)

| Ferramenta | Versão | Windows (PowerShell) |
|---|---|---|
| Git | qualquer recente | `winget install Git.Git` |
| .NET SDK | **9.x** | `winget install Microsoft.DotNet.SDK.9` |
| Node.js | **22 LTS** ou mais novo (mín. 20.19) | `winget install OpenJS.NodeJS.LTS` |
| Docker Desktop | qualquer recente | `winget install Docker.DockerDesktop` |
| Visual Studio 2022 (17.14+) | Community serve | cargas de trabalho **Desenvolvimento para ASP.NET e Web** e **Desenvolvimento de Node.js** |

No VS Code, instale as extensões recomendadas (o VS Code sugere sozinho ao abrir a pasta): **C# Dev Kit** e **Tailwind CSS**.

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

---

## 2. Baixar o projeto

```bash
git clone https://github.com/opedroasa/commander-500.git
```

```bash
cd commander-500
```

```bash
dotnet tool restore
```

---

## 3. Rodar

Antes de tudo, abra o **Docker Desktop** e espere aparecer "Engine running".

### Visual Studio (recomendado)

1. Abra `Commander500.sln`.
2. Confira que **Commander500.Api** é o projeto de inicialização (nome em negrito; se não for: botão direito → *Definir como Projeto de Inicialização*) e que o perfil ao lado do ▶ é **http**.
3. Aperte **F5**.

Na primeira vez o Visual Studio roda o `npm install` do site (pode demorar alguns minutos). Depois abre o navegador
numa página "Launching the SPA proxy..." que vira o site em poucos segundos.

> Quer só a Api, sem o site? Use o perfil **so-api** (abre o Swagger).

### VS Code

Abra a pasta do projeto e aperte **F5** (configuração "Commander 500 (Api + Site)"). Ele sobe o banco, instala as
dependências do site, compila e inicia a Api, que abre o site.

### Script (sem editor)

**Windows:**

```bash
powershell -ExecutionPolicy Bypass -File scripts\dev.ps1
```

**macOS / Linux:**

```bash
./scripts/dev.sh
```

### Endereços locais

| Endereço | O que é |
|---|---|
| <http://localhost:5173> | Site (público) |
| <http://localhost:5173/admin> | Área do admin: **admin@fantasystore.local** / **admin123** |
| <http://localhost:5173/ao-vivo> | Mesas e timer da rodada em andamento (para o telão / celular) |
| <http://localhost:5080/swagger> | Documentação interativa da API |

Na primeira vez a API cria as tabelas e os **dados de exemplo**: "Temporada 1" com 10 jogadores, decks (alguns pendentes/reprovados, incluindo parceiros e background) e 3 rodadas encerradas, então ranking e estatísticas já aparecem preenchidos.

---

## 4. Rodar passo a passo (para depurar)

Três terminais, todos na raiz do projeto.

**Terminal 1 — banco:**

```bash
docker compose up -d --wait banco
```

**Terminal 2 — site:**

```bash
npm install --prefix web
```

```bash
npm run dev --prefix web
```

**Terminal 3 — API** (perfil `so-api`, para não abrir outro site):

```bash
dotnet run --project src/Commander500.Api --launch-profile so-api
```

---

## 5. Comandos do dia a dia

| Para… | Comando |
|---|---|
| Rodar os testes | `dotnet test` |
| Checar o site (tipos + build) | `npm run build --prefix web` |
| Lint do site | `npm run lint --prefix web` |
| Parar o banco (mantém os dados) | `docker compose stop` |
| **Zerar o banco local e recriar os exemplos** | `docker compose down -v` e rodar de novo |
| Criar migration após mudar uma entidade | `dotnet ef migrations add NomeDaMudanca --project src/Commander500.Infraestrutura --output-dir Dados/Migracoes` |

---

## 6. Publicação

| | |
|---|---|
| Endereço | `https://fantasy500.vercel.app` (site em `/`, Api em `/api`, Swagger em `/swagger`) |
| Site + Api | Vercel, configurados no [`vercel.json`](../vercel.json) da raiz (serviços `site` e `api`) |
| Banco | Supabase (PostgreSQL, região São Paulo) |
| Como publicar | **merge na `main`** → GitHub Actions roda os testes e publica ([`publicar.yml`](../.github/workflows/publicar.yml)) |

Passo a passo completo (criar os projetos, variáveis, domínio): [docs/DEPLOY.md](DEPLOY.md).

---

## 7. Roteiro de demonstração para o lojista

1. **Site público**: temporada → **Ranking** (toque no nome para abrir o perfil), **Etapas** (ranking do dia) e **Comandantes** (artes do Scryfall; duplas de parceiros contam como uma linha).
2. **Admin → Rodadas → Nova etapa (hoje).** Uma segunda rodada no mesmo dia já vem com os presentes da anterior.
3. **Presentes:** marque os jogadores e confira o deck de cada um (aviso para deck pendente/reprovado) → **Salvar e sortear**.
4. **Sorteio:** mesas desenhadas com as 4 cadeiras (cadeira 1 começa). Mostre o **Sortear novamente** e que quem já teve bye na temporada não repete.
5. Abra **Ao vivo** em outra aba ou no celular e use "Encontre sua mesa".
6. **Iniciar rodada:** o timer aparece igual em todas as telas, e recarregar a página não reinicia.
7. Toque no vencedor de cada mesa e confirme, ou marque **Empate** → **Encerrar rodada** → o ranking atualiza.
8. **Administradores:** quem pode entrar na área do lojista (cadastrar, trocar senha, excluir).
9. **Cadastros:** jogadores (perfil com **Novo deck**, comandante buscado no Scryfall), decks (aprovar/reprovar com observação) e a **Pontuação** da temporada (trava após a 1ª rodada encerrada).

**No celular:** use o endereço publicado. Para testar a versão local no celular, rode o site com `npm run dev --prefix web -- --host` e acesse `http://<ip-do-computador>:5173` no mesmo Wi-Fi.

---

## 8. Problemas comuns

| Sintoma | Solução |
|---|---|
| Aviso "Não consegui subir o banco local" ao compilar | Abra o Docker Desktop, espere iniciar e aperte F5 de novo |
| `O Docker não está rodando` / erro de `dockerDesktopLinuxEngine` | Abra o Docker Desktop e espere iniciar |
| `Failed to connect to 127.0.0.1:5432` | O banco não está rodando: `docker compose up -d --wait banco` |
| Banco não sobe: porta **5432** em uso | Outro Postgres está rodando na máquina; pare-o ou mude a porta em `docker-compose.yml` **e** em `appsettings.Development.json` |
| Porta **5080** ou **5173** em uso | Feche a Api/site antigos (ou finalize o processo `dotnet`/`node`). A 5080 também é usada pela Gestão Rastreio Wiki: rode um projeto de cada vez |
| Fica parado em "Launching the SPA proxy..." | Veja a janela do `npm run dev` que abriu. Se faltar pacote (`Could not resolve ...`), apague `web\node_modules` e rode `npm install --prefix web` sem interromper |
| `Commander500.Web` aparece como "não carregado" no Visual Studio | Instale a carga **Desenvolvimento de Node.js** no Visual Studio Installer |
| Script `.ps1` bloqueado ("execução de scripts foi desabilitada") | Use exatamente `powershell -ExecutionPolicy Bypass -File scripts\dev.ps1` |
| Site abre mas mostra "Não foi possível conectar ao servidor" | A API não está rodando; veja a saída da API (`ECONNREFUSED` no terminal do Vite = API parada) |
| `dotnet ef` não encontrado | `dotnet tool restore` |

---

## 9. Trabalhando em dupla

- Crie uma branch por tarefa (`git switch -c feature/nome`) e abra Pull Request para a `main`. **Merge na `main` = publicação no ar.**
- Antes de subir: `dotnet test` e `npm run build --prefix web` (o GitHub Actions roda os dois de novo antes de publicar).
- Mudou entidade? Gere a migration e faça commit dela junto (ela é aplicada no banco de produção quando a Api liga).
- Regras de negócio ficam em `src/Commander500.Dominio/Regras` com teste em `tests/` (ver [CLAUDE.md](../CLAUDE.md)).
- Nomes em português (classes, métodos, variáveis, tabelas, rotas e JSON); só o que é da linguagem/framework fica em inglês. Glossário no [CLAUDE.md](../CLAUDE.md).
