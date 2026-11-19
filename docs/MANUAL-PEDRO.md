# Manual do Pedro: acessos e como continuar o projeto

Pedro, este é o guia do que mudou, de como você acessa cada serviço e de como as atualizações chegam ao site no ar.

## O que mudou (resumo)

| Antes | Agora |
|---|---|
| Api e site rodavam separados (2 janelas / 2 comandos) | **Uma solução só**: o site virou o projeto `Commander500.Web` dentro da `Commander500.sln`. **F5 na Api sobe banco + Api + site** |
| Deploy planejado em Neon + Render + Vercel | **Vercel** (site **e** Api, no mesmo endereço) + **Supabase** (banco) |
| Deploy manual | **Merge na `main` publica sozinho** (GitHub Actions roda os testes antes) |
| `Dockerfile` na raiz | `src/Dockerfile` (a pasta `src/` é a raiz do serviço `api` na Vercel) |
| `web/vercel.json` | `vercel.json` na raiz, com os dois serviços (`site` e `api`) |

Arquivos novos: `web/Commander500.Web.esproj`, `vercel.json`, `.vercelignore`, `.github/workflows/publicar.yml`,
`.vscode/` (F5 no VS Code), `docs/MANUAL-PEDRO.md`. O `render.yaml` ficou como **plano B** (não é usado).

---

## 1. Seus acessos

| Serviço | Para quê | Como você entra |
|---|---|---|
| **GitHub** (`opedroasa/commander-500`) | código, Pull Requests, publicação automática | você é o dono; nada muda |
| **Supabase** | banco de produção (ver tabelas, rodar SQL, backup) | o Murilo te convida para a organização: aceite o e-mail **"You have been invited..."** e entre em <https://supabase.com/dashboard> |
| **Vercel** | ver publicações, logs e variáveis | o plano grátis (Hobby) **não tem membros de equipe**: o projeto fica na conta do Murilo. Veja abaixo |
| **Site no ar** | `https://commander500.vercel.app` | admin: e-mail/senha definidos em `Admin__Email` / `Admin__Senha` (combinem por mensagem privada, nunca no Git) |

### Vercel: como fica para você

- **Para publicar, você não precisa entrar na Vercel.** O merge na `main` dispara o GitHub Actions, que publica com
  um token. Você acompanha tudo pela aba **Actions** do repositório.
- **Para ver logs ou trocar variáveis**, há três caminhos:
  1. pedir ao Murilo (mais simples hoje);
  2. **transferir o projeto para a sua conta** da Vercel: o Murilo vai em *Settings → General → Transfer Project* e você
     aceita. Depois disso **você** gera um token novo e troca o segredo `VERCEL_TOKEN` (e `VERCEL_ORG_ID`) no GitHub;
  3. plano Pro da Vercel (pago), que permite membros.

### Configuração única que só você pode fazer (dono do repositório)

O GitHub só deixa o dono cadastrar segredos. O Murilo te passa os 3 valores por mensagem privada e você cadastra em
**GitHub → repositório → Settings → Secrets and variables → Actions → New repository secret**:

| Secret | O que é |
|---|---|
| `VERCEL_TOKEN` | token da conta da Vercel onde está o projeto |
| `VERCEL_ORG_ID` | id da conta (arquivo `.vercel/project.json`, campo `orgId`) |
| `VERCEL_PROJECT_ID` | id do projeto (mesmo arquivo, campo `projectId`) |

Depois teste: aba **Actions → Publicar na Vercel → Run workflow**. Ficando verde, está tudo ligado.

---

## 2. Preparar o seu computador (uma vez)

1. Instale o que está em **Pré-requisitos** no [README](../README.md#1-pré-requisitos-uma-vez-só). No Visual Studio
   Installer, marque **Desenvolvimento para ASP.NET e Web** e **Desenvolvimento de Node.js**.
2. Atualize o código:

   ```bash
   git switch main
   ```

   ```bash
   git pull
   ```

   ```bash
   dotnet tool restore
   ```

3. Abra o **Docker Desktop**, abra `Commander500.sln` e aperte **F5** (projeto `Commander500.Api`, perfil **http**).
   Na primeira vez demora por causa do `npm install`. O navegador abre no site com os dados de exemplo locais.

---

## 3. Como uma atualização vai do código até o site

```
branch feature/xxx ──► Pull Request ──► você revisa ──► merge na main ──► Actions: testes ──► Vercel publica
```

1. Quem for mexer cria uma branch: `git switch -c feature/nome-curto`.
2. Faz as mudanças, roda `dotnet test` e `npm run build --prefix web`, faz commit e `git push -u origin feature/nome-curto`.
3. Abre o **Pull Request** para a `main` no GitHub.
4. Você revisa. Para aprovar: **Merge pull request** (deixe o padrão **Create a merge commit**).
5. A aba **Actions** mostra "Publicar na Vercel" rodando (~3–5 min). Verde = está no ar.
6. Confira `https://commander500.vercel.app/saude` → `{"situacao":"ok"}` e dê uma olhada no site.

**Se a Action ficar vermelha**, nada foi publicado (o site continua na versão anterior). Clique nela para ver o erro:
em "Testes e build" é problema no código; em "Enviar para a Vercel" é token/segredo ou erro de build lá na Vercel.

### Mudanças no banco (migrations)

Quando uma entidade muda, o PR traz uma migration em `src/Commander500.Infraestrutura/Dados/Migracoes/`.
**Ela é aplicada no Supabase automaticamente** quando a Api nova liga. Antes de aprovar um PR com migration, confira se
ela não apaga colunas ou tabelas com dados que importam.

### Desfazer uma publicação

- **Rápido:** no GitHub, abra o PR que entrou e clique em **Revert** → gera outro PR que desfaz → merge.
- **Na hora (quem tem acesso à Vercel):** *Deployments* → publicação anterior → **⋯ → Instant Rollback**.
- Atenção: nenhum dos dois desfaz uma migration que já rodou no banco.

---

## 4. Banco de produção (Supabase)

- **Ver dados:** Supabase → projeto `commander500` → **Table Editor** (tabelas com nomes em português: `Temporadas`, `Jogadores`...).
- **Rodar SQL:** **SQL Editor**. Cuidado: ali não tem "desfazer".
- **Projeto pausado:** no plano grátis, 7 dias sem nenhum acesso pausam o projeto. Abra o painel e clique em **Restore**.
- **Backup manual** (recomendado antes de mudanças grandes): o plano Free não guarda backups que você possa baixar,
  então use o `pg_dump` com a connection string (peça ao Murilo) ou exporte as tabelas em CSV pelo Table Editor.
- **Senha do banco:** só na variável `ConnectionStrings__Padrao` da Vercel e no gerenciador de senhas. Se vazar:
  Supabase → **Project Settings → Database → Reset database password**, e atualize a variável na Vercel (+ Redeploy).

---

## 5. Regras combinadas

- **Nunca commitar direto na `main`** (ela publica). Sempre branch + PR.
- **Nunca colocar senha, token ou connection string no código.** Eles ficam nas variáveis da Vercel e nos segredos do GitHub.
- Regras de negócio em `src/Commander500.Dominio/Regras` com teste em `tests/`; nomes em português (ver [CLAUDE.md](../CLAUDE.md)).
- Detalhes técnicos da publicação: [docs/DEPLOY.md](DEPLOY.md).

## Checklist de hoje

- [ ] Aceitar o convite do Supabase
- [ ] Cadastrar os 3 segredos `VERCEL_*` no GitHub e rodar a Action uma vez
- [ ] `git pull` na `main`, abrir a solução e testar o F5
- [ ] Abrir `https://commander500.vercel.app` e entrar no admin
