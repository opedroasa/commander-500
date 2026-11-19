# Publicação – Commander 500

Tudo no plano gratuito, e **o site e a Api ficam no mesmo endereço** (**`https://fantasy500.vercel.app`**):

| Parte | Serviço | Arquivo |
|---|---|---|
| Site (React) | **Vercel**, serviço `site` | [`vercel.json`](../vercel.json) |
| Api (.NET 9 em container Docker) | **Vercel**, serviço `api` (*Container Runtime*, em Beta) | [`vercel.json`](../vercel.json), [`src/Dockerfile`](../src/Dockerfile) |
| Banco PostgreSQL | **Supabase** | — |
| Publicação automática | **GitHub Actions** (a cada merge na `main`) | [`.github/workflows/publicar.yml`](../.github/workflows/publicar.yml) |

```
navegador ──► https://fantasy500.vercel.app
                 ├─ /api/*, /saude, /swagger ──► serviço "api" (container .NET) ──► Supabase (Postgres)
                 └─ todo o resto ───────────────► serviço "site" (arquivos do Vite)
```

Como site e Api estão no mesmo endereço, **não precisa de CORS nem de `VITE_API_URL`**.

Ordem: **Supabase → Vercel (projeto + variáveis) → primeira publicação → domínio → publicação automática**.

---

## 1. Supabase (banco)

1. Entre em <https://supabase.com/dashboard> → **New project**.
   - **Name:** `commander500`
   - **Database Password:** clique em **Generate a password** e **guarde** num gerenciador de senhas (ela não aparece de novo).
   - **Region:** **South America (São Paulo)** — `sa-east-1`.
   - **Plan:** Free.
   - Em **Security options**, **desmarque "Enable Data API"**. Nós não usamos a API REST do Supabase; deixá-la ligada
     exporia as tabelas (inclusive a de administradores) para quem tiver a chave pública do projeto.
     > Se o projeto já foi criado com ela ligada: **Project Settings → Data API** → desligue.
2. Espere o projeto ficar verde (1–2 min).
3. Clique em **Connect** (barra do topo) → aba **Connection String** → **Type: .NET** → **Method: Session pooler**.
   - Use o **Session pooler** (porta 5432 no host `...pooler.supabase.com`): a conexão direta do Supabase é só IPv6,
     e a Vercel precisa de IPv4. O *Transaction pooler* (porta 6543) não serve, porque o EF Core usa um *lock* de sessão
     ao aplicar as migrations.
4. Monte a connection string assim (troque `<ref>` e `<senha>`):

   ```
   Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<senha>;SSL Mode=Require;Maximum Pool Size=5
   ```

   Ela vai na variável `ConnectionStrings__Padrao` da Vercel. Não precisa criar tabelas: a Api roda as migrations ao ligar.

   > No projeto atual o host é `aws-1-sa-east-1.pooler.supabase.com` e o usuário `postgres.zduclzhcdivazxhfkrmr`.

5. **Depois da primeira publicação** (quando as tabelas já existem), ligue o RLS no **SQL Editor** do Supabase.
   O RLS bloqueia as tabelas para a API pública do Supabase; a nossa Api não é afetada, porque o usuário `postgres`
   tem permissão para ignorar o RLS (`BYPASSRLS`). O gatilho no fim faz as tabelas criadas por migrations futuras
   já nascerem com RLS. *(No projeto atual isso já foi feito em 19/11/2026.)*

   ```sql
   do $$
   declare t record;
   begin
     for t in select tablename from pg_tables where schemaname = 'public' loop
       execute format('alter table public.%I enable row level security', t.tablename);
     end loop;
   end $$;

   create or replace function public.ligar_rls_tabelas_novas()
   returns event_trigger language plpgsql set search_path = '' as $$
   declare objeto record;
   begin
     for objeto in select * from pg_event_trigger_ddl_commands()
                   where command_tag = 'CREATE TABLE' and schema_name = 'public' loop
       execute format('alter table %s enable row level security', objeto.object_identity);
     end loop;
   end; $$;

   create event trigger ligar_rls_tabelas_novas on ddl_command_end
     when tag in ('CREATE TABLE') execute function public.ligar_rls_tabelas_novas();
   ```

   O painel do Supabase passa a mostrar o aviso informativo "RLS Enabled No Policy": é o esperado (sem políticas, ninguém de fora acessa).

> **Atenção (plano Free):** o Supabase **pausa projetos sem nenhum acesso por 7 dias**. Se o site mostrar erro de banco
> depois de um tempo parado, abra o projeto no painel do Supabase e clique em **Restore** (grátis, leva ~1 min).

---

## 2. Vercel (projeto e variáveis)

1. Entre em <https://vercel.com> com o GitHub.
2. Instale a Vercel CLI só na hora, com `npx` (não precisa instalar nada globalmente). Na **raiz do projeto**:

   ```bash
   npx vercel@65.11.0 login
   ```

   ```bash
   npx vercel@65.11.0 link --yes --project commander500
   ```

   Isso cria o projeto `commander500` na sua conta e grava a ligação na pasta `.vercel/` (ela fica fora do Git).
   A Vercel lê o [`vercel.json`](../vercel.json) da raiz e já entende que há dois serviços (`site` e `api`).
3. **Settings → Functions → Function Region:** escolha **São Paulo, Brazil (gru1)**, perto do banco.
4. **Settings → Environment Variables** (ambiente **Production**):

   | Variável | Valor |
   |---|---|
   | `PORT` | `8080` (porta em que o container da Api escuta) |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__Padrao` | a connection string do passo 1.4 |
   | `Jwt__Chave` | segredo aleatório com 32+ caracteres (comando abaixo) |
   | `Admin__Email` | e-mail do **primeiro** administrador (só é usado quando ainda não há nenhum) |
   | `Admin__Senha` | senha desse primeiro administrador |
   | `Semente__DadosDemonstracao` | `true` para a apresentação (cria dados de exemplo **só se o banco estiver vazio**); `false` depois |
   | `Swagger__Habilitado` | `true` |

   Gerar a `Jwt__Chave` no PowerShell:

   ```powershell
   $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
   ```

---

## 3. Primeira publicação

Na raiz do projeto, com a `main` atualizada (`git switch main` e `git pull`):

```bash
npx vercel@65.11.0 deploy --prod
```

A primeira vez demora (a Vercel compila o site e a imagem Docker da Api). No fim aparece o endereço. Confira:

| Endereço | Esperado |
|---|---|
| `https://fantasy500.vercel.app/saude` | `{"situacao":"ok"}` |
| `https://fantasy500.vercel.app/swagger` | documentação da Api |
| `https://fantasy500.vercel.app` | site com o ranking |
| `https://fantasy500.vercel.app/admin` | login com `Admin__Email` / `Admin__Senha` |

> A Api **desliga depois de ~5 min sem acesso** e liga de novo sozinha na próxima visita, com a requisição normal
> (medido: **~5 s** para a primeira resposta, depois ~0,5 s). O site tenta de novo automaticamente se precisar.
> Durante a liga, com gente olhando o "Ao vivo", ela fica ligada. Para manter ligada num dia de liga, dá para agendar
> um ping gratuito em <https://cron-job.org> para `/saude` a cada 4 min **só nos horários da liga** (24 h por dia pode
> passar da cota de memória do plano grátis).

---

## 4. Domínio

Endereço atual: **`fantasy500.vercel.app`**. O antigo `commander500.vercel.app` redireciona para ele.

- **Trocar por outro `*.vercel.app` (grátis):** projeto → **Settings → Domains → Add Domain** → digite o novo nome
  (ex.: `nome.vercel.app`) → **Save**. Se estiver livre, já funciona. Para o antigo apontar para o novo: no domínio antigo,
  **Edit** → **Redirect to** o novo (308) → **Save**.
- **Domínio próprio (pago, ~R$ 40/ano):** registre em <https://registro.br> (ex.: `commander500.com.br`), adicione em
  **Settings → Domains → Add** e crie no registro.br os registros DNS que a Vercel mostrar. O HTTPS é automático.
  Como site e Api ficam no mesmo endereço, não precisa mexer em CORS nem no código.

---

## 5. Publicação automática (GitHub Actions)

Depois disso, **todo merge na `main` publica sozinho**: o workflow [`publicar.yml`](../.github/workflows/publicar.yml)
roda `dotnet test` e o build do site e, se passar, envia para a Vercel.

Por que pelo GitHub Actions e não pela integração Git da Vercel? O repositório é **privado e está na conta do Pedro**, e o
plano Hobby da Vercel não aceita deploy automático de commits de outra pessoa em repositório privado. Com o token,
qualquer merge na `main` publica, não importa quem fez.

Configuração (uma vez só, **feita pelo dono do repositório**):

1. **Token:** na Vercel → avatar → **Account Settings → Tokens → Create** (nome `github-actions`, sem validade ou 1 ano).
2. **IDs:** abra `.vercel/project.json` (criado no passo 2.2): tem o `orgId` e o `projectId`.
3. No GitHub: **repositório → Settings → Secrets and variables → Actions → New repository secret**:

   | Secret | Valor |
   |---|---|
   | `VERCEL_TOKEN` | o token do passo 1 |
   | `VERCEL_ORG_ID` | `orgId` |
   | `VERCEL_PROJECT_ID` | `projectId` |

4. Teste: aba **Actions → Publicar na Vercel → Run workflow**.

Cada execução gasta ~3–5 min da cota grátis de Actions (2.000 min/mês em repositório privado).

---

## 6. Primeiro acesso em produção

1. Abra o site → **Admin** → entre com `Admin__Email` / `Admin__Senha`.
2. **Temporadas:** crie a temporada e ajuste **Pontuação** **antes** de encerrar a primeira rodada (depois ela trava).
3. **Jogadores** e **Decks**, depois **Rodadas → Nova rodada** no dia da liga. Para os jogadores, compartilhe `/ao-vivo`.

### Tirar os dados de exemplo depois da apresentação

Os dados de exemplo são criados **toda vez que a Api liga e o banco não tem nenhuma temporada**. Então a ordem importa:

1. Na Vercel, troque `Semente__DadosDemonstracao` para `false` e faça **Deployments → ⋯ → Redeploy** no último deploy.
   Espere ficar *Ready*.
2. No Supabase → **SQL Editor**, rode (apaga temporadas, jogadores, decks, comandantes e rodadas; **mantém** os
   administradores, o regulamento e o RLS):

   ```sql
   truncate table "Cadeiras", "Mesas", "Participacoes", "Rodadas", "Etapas",
                  "DecksComandantes", "Decks", "Jogadores", "Comandantes", "Temporadas"
     restart identity cascade;
   ```

3. Abra o site: ele aparece vazio, pronto para cadastrar a temporada de verdade.

---

## Administradores, variáveis etc.

- **Cadastrar, excluir e trocar senha de administradores:** no site, **Admin → Administradores**. Ninguém exclui o
  próprio acesso e o último administrador não pode ser excluído. Quem é excluído perde o acesso na hora.
- `Admin__Email` / `Admin__Senha` só valem para criar o **primeiro** administrador (banco sem nenhum). Mudar essas
  variáveis depois **não** altera ninguém.
- **Todo mundo perdeu a senha?** No Supabase → SQL Editor: `delete from "Administradores";` → na Vercel, confira
  `Admin__Email`/`Admin__Senha` e faça **Redeploy**. A Api recria o primeiro administrador com esses dados.
- Toda mudança de variável na Vercel só vale depois de um **Redeploy**.
- **Logs da Api:** Vercel → projeto → **Logs** (o que a Api escreve no console aparece lá).

## Problemas comuns

| Sintoma | Causa provável |
|---|---|
| `/saude` dá erro 502/504 | Api não subiu: veja **Logs**. Confira `PORT=8080`, `Jwt__Chave` (32+ caracteres) e a connection string |
| Erro de conexão com o banco nos logs | connection string errada, senha errada ou projeto do Supabase **pausado** (clique em Restore) |
| `Tenant or user not found` | o usuário precisa ser `postgres.<ref>` (com o ref do projeto) no Session pooler |
| Login retorna 401 | e-mail ou senha errados (o e-mail é comparado em minúsculas). Peça a outro admin para trocar a senha em **Admin → Administradores** |
| Rota como `/ao-vivo` dá 404 ao recarregar | o `vercel.json` da raiz não foi lido: publique a partir da raiz do repositório |
| Action falha em "Enviar para a Vercel" | segredos `VERCEL_*` ausentes ou token vencido |

## Plano B: Api no Render

O Container Runtime da Vercel ainda está em **Beta**. Se um dia ele sair do plano grátis ou der problema, a mesma imagem
roda no Render: [`render.yaml`](../render.yaml) (New → Blueprint). Nesse caso o site continua na Vercel, mas aí precisa
de `VITE_API_URL` = endereço do Render no build do site, `Cors__OrigensPermitidas` = endereço do site, e de tirar as
rotas `api`/`saude`/`swagger` do `vercel.json`.
