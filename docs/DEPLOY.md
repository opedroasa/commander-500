# Deploy – Commander 500

Tudo no plano gratuito:

| Parte | Serviço | Arquivo |
|---|---|---|
| Banco PostgreSQL | **Neon** | — |
| API (.NET 9, Docker) | **Render** | [`render.yaml`](../render.yaml), [`Dockerfile`](../Dockerfile) |
| Frontend (React) | **Vercel** | [`web/vercel.json`](../web/vercel.json) |
| Keep-alive da API | **cron-job.org** (ou UptimeRobot) | — |

Ordem: **GitHub → Neon → Render → Vercel → CORS no Render → keep-alive**.

> Região: o Render não tem datacenter no Brasil. Coloque API e banco **na mesma região** (Render *Virginia* + Neon *AWS US East 1*). A latência API↔banco pesa mais que a do usuário↔API, porque cada tela faz várias consultas.

---

## 0. Código no GitHub

Render e Vercel fazem deploy a partir do repositório. Crie um repositório **privado** vazio e envie:

```bash
git remote add origin https://github.com/<usuario>/<repo>.git
```

```bash
git push -u origin main
```

---

## 1. Neon (banco)

1. Crie uma conta em <https://neon.tech> e um projeto: **Region = AWS US East 1 (N. Virginia)**, Postgres 16+.
2. Em **Connect**, deixe **Connection pooling desligado** (conexão direta; o EF Core aplica migrations no startup e o tráfego é baixo).
3. Copie a connection string, no formato:
   `postgresql://<user>:<senha>@ep-xxxx.us-east-1.aws.neon.tech/neondb?sslmode=require`
   Ela vai na variável `DATABASE_URL` do Render. A API aceita esse formato de URI direto.

Não é preciso criar tabelas: a API roda as migrations ao subir (`Database__MigrateOnStartup=true`).

---

## 2. Render (API)

1. Crie uma conta em <https://render.com> e conecte o GitHub.
2. **New → Blueprint** → selecione o repositório. O Render lê o [`render.yaml`](../render.yaml) e cria o serviço `commander500-api` (Docker, plano free, health check em `/health`).
3. Preencha as variáveis pedidas:

   | Variável | Valor |
   |---|---|
   | `DATABASE_URL` | a connection string do Neon |
   | `Admin__Email` | e-mail de login do lojista |
   | `Admin__Password` | senha forte (é a senha de login do admin) |
   | `Cors__AllowedOrigins` | por enquanto `*`; troque no passo 4 |

   `Jwt__Key` é gerada automaticamente pelo Render (`generateValue`). `Seed__DemoData=false`: produção começa vazia.
4. Aguarde o build (5–10 min na primeira vez) e confira:
   - `https://commander500-api.onrender.com/health` → `{"status":"ok"}`
   - `https://commander500-api.onrender.com/swagger`

   (O nome real do host aparece no painel do serviço.)

**Trocar a senha do admin:** altere `Admin__Password` no Render e salve. O serviço reinicia e a senha é atualizada no banco. Trocar `Admin__Email` cria um novo admin (o antigo continua existindo).

---

## 3. Vercel (frontend)

1. Crie uma conta em <https://vercel.com> e conecte o GitHub.
2. **Add New → Project** → importe o repositório.
3. Configure:
   - **Root Directory:** `web`
   - **Framework Preset:** Vite (detectado automaticamente; build `npm run build`, saída `dist`)
   - **Environment Variable:** `VITE_API_URL` = `https://commander500-api.onrender.com` (sem barra no final)
4. **Deploy.** Anote a URL de produção (ex.: `https://commander500.vercel.app`).

O [`web/vercel.json`](../web/vercel.json) faz o fallback de rotas para `index.html`, então links como `/temporadas/1` e `/ao-vivo` funcionam ao recarregar a página.

> `VITE_API_URL` é embutida no build. Se mudar o endereço da API, faça **Redeploy** na Vercel.

---

## 4. CORS (de volta ao Render)

No serviço `commander500-api` → **Environment**, troque `Cors__AllowedOrigins` pela URL da Vercel:

```
https://commander500.vercel.app
```

Para mais de uma origem (ex.: domínio próprio), separe por vírgula. Salvar reinicia o serviço.

Deploys de *preview* da Vercel têm outra URL e serão bloqueados pelo CORS; isso é esperado. Use `*` só temporariamente se precisar testar um preview.

---

## 5. Keep-alive (evitar cold start)

O plano free do Render **desliga a API após 15 min sem requisições**; a primeira visita depois disso leva ~30–60 s (o frontend mostra a mensagem "o servidor pode estar acordando" e tenta de novo sozinho).

Para manter a API acordada, crie um ping gratuito em <https://cron-job.org> (ou UptimeRobot):

- **URL:** `https://commander500-api.onrender.com/health`
- **Intervalo:** a cada **10 minutos**

O plano free tem 750 h/mês de instância, suficiente para 1 serviço ligado 24/7. O `/health` não consulta o banco, então o Neon pode hibernar normalmente (acorda em ~1 s na primeira consulta).

> Não use GitHub Actions para o ping: em repositório privado cada execução consome no mínimo 1 minuto da cota gratuita (2.000 min/mês), e um ping a cada 10 min gasta ~4.300 min/mês.

Se preferir economizar horas, ative o ping só nos dias de liga.

---

## 6. Primeiro acesso em produção

1. Abra o site → **Lojista** → entre com `Admin__Email` / `Admin__Password`.
2. **Temporadas:** crie a temporada e ajuste **Pontuação** (bye com 2/3 de fora, timer padrão, desempate) **antes** de encerrar a primeira rodada — depois disso a configuração trava.
3. **Jogadores** e **Decks** (o comandante é criado automaticamente na primeira vez em que é digitado).
4. **Rodadas → Nova rodada** no dia da liga. Para os jogadores, compartilhe o link `/ao-vivo`.

---

## Atualizações

- `git push` na `main` → Render (só se mudar `src/`, `Dockerfile` ou a `.sln`) e Vercel fazem deploy automático.
- Mudou entidade? Gere a migration localmente (ver [CLAUDE.md](../CLAUDE.md)), faça commit; ela é aplicada no próximo startup da API.

## Problemas comuns

| Sintoma | Causa provável |
|---|---|
| Front mostra "Não foi possível conectar ao servidor" | API dormindo (aguarde ~1 min) ou `VITE_API_URL` errada (refaça o deploy da Vercel após corrigir) |
| Erro de CORS no console do navegador | `Cors__AllowedOrigins` não contém exatamente a origem do site (sem barra final) |
| API não sobe: "Jwt:Key ausente" | variável `Jwt__Key` removida — gere um valor com 32+ caracteres |
| API não sobe: erro de conexão com o banco | `DATABASE_URL` errada ou sem `sslmode=require` |
| Login retorna 401 | e-mail/senha diferentes de `Admin__Email`/`Admin__Password` (o e-mail é comparado em minúsculas) |
| Rota como `/ao-vivo` dá 404 ao recarregar | Root Directory da Vercel não é `web` (o `vercel.json` não foi lido) |
