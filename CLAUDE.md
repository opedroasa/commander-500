# Commander 500 – Fantasy Store

Liga de Commander 500 (MTG). Especificação completa em [SPEC.md](SPEC.md) — leia antes de mudar regras de negócio.

## Estrutura

```
Commander500.sln
src/Commander500.Api/        ASP.NET Core (.NET 9) minimal APIs + EF Core/Npgsql
  Domain/                    entidades, exceções, CommanderName
  Domain/Services/           regras puras e testáveis (sem EF): RankingCalculator, TableDrawer,
                             RoundWorkflow (máquina de estados), CommanderStatsCalculator, SeasonRules
  Application/               serviços que carregam/salvam via DbContext e chamam o Domain
  Endpoints/                 PublicEndpoints (sem login) e AdminEndpoints (JWT, policy "Admin")
  Data/                      AppDbContext, DbSeeder, Migrations/
  Infrastructure/            ErrorHandler (exceção → ProblemDetails), conversão de URI postgres://
tests/Commander500.Tests/    xUnit, só testa Domain/Services (grafo em memória via RoundBuilder)
web/                         React 19 + Vite + TS + Tailwind v4, mobile-first
  src/api/                   client.ts (fetch + JWT), types.ts (espelha Contracts/Dtos.cs), hooks.ts (TanStack Query)
  src/components/            TableView (desenho da mesa), Countdown (timer), RoundBoard, Layout, ui.tsx
  src/pages/public/          Temporadas, Detalhe (ranking/comandantes/rodadas), Ao vivo, Rodada
  src/pages/admin/           Login, Rodadas, Condução da rodada, Jogadores, Decks, Comandantes, Temporadas
docker-compose.yml           Postgres local (+ perfil "api" opcional)
Dockerfile                   imagem da API (contexto = raiz do repo)
scripts/dev.ps1, dev.sh      sobe banco + API + site com um comando (guia completo no README.md)
render.yaml, web/vercel.json, docs/DEPLOY.md   publicação (Render + Neon + Vercel) — preparada, ainda não usada
```

## Comandos

```bash
docker compose up -d --wait db                 # Postgres local (user/senha/db: commander500)
dotnet build
dotnet test
dotnet run --project src/Commander500.Api      # http://localhost:5080/swagger (Development, migra e faz seed)
dotnet ef migrations add <Nome> --project src/Commander500.Api --output-dir Data/Migrations
docker compose --profile api up --build        # API + banco em Docker (porta 8080)
docker compose down -v                         # zera o banco local
```

Frontend (pasta `web/`, precisa da API rodando em :5080 — o Vite faz proxy de `/api`):

```bash
npm install --prefix web
npm run dev --prefix web                       # http://localhost:5173
npm run build --prefix web                     # tsc -b + vite build → web/dist
npm run lint --prefix web                      # oxlint
```

`VITE_API_URL` (ver `web/.env.example`) fica vazio em dev; em produção é a origem da API.

`dotnet ef` é ferramenta local (`.config/dotnet-tools.json`); rode `dotnet tool restore` numa máquina nova.
Swagger: `/swagger`. Health: `/health`. Admin de dev: `admin@fantasystore.local` / `admin123`.

## Configuração (variáveis de ambiente)

| Variável | Uso |
|---|---|
| `ConnectionStrings__Default` ou `DATABASE_URL` | aceita `Host=...` ou `postgres://user:pass@host/db?sslmode=require` |
| `Jwt__Key` | obrigatório, ≥ 32 caracteres (a API não sobe sem) |
| `Admin__Email`, `Admin__Password` | cria o admin no startup se não existir; se existir e a senha mudou, atualiza |
| `Cors__AllowedOrigins` | lista separada por vírgula (`*` libera tudo) |
| `Database__MigrateOnStartup` | padrão `true` |
| `Seed__DemoData` | `true` cria 1 temporada + 10 jogadores/decks + 3 rodadas encerradas se não houver temporadas |
| `Swagger__Enabled` | padrão `true` |
| `PORT` | se definido (Render), a API escuta nessa porta |

## Convenções

- Código em inglês; mensagens ao usuário em português. Enums serializados como string em inglês
  (`Draft/Drawn/InProgress/Closed`, `Pending/Approved/Rejected`, `Pending/Win/Draw`) — o frontend traduz.
- Regra de negócio nova vai em `Domain/Services` como função pura + teste em `tests/`. Serviços de `Application/`
  só carregam, chamam o domínio e salvam.
- Erros: `ValidationException` → 400, `DomainException` → 409, `NotFoundException` → 404 (ProblemDetails com `detail`).
- **Pontos nunca são gravados**: ranking e estatísticas são calculados a cada request a partir das rodadas `Closed`.
- `Season.ConfigLocked` vira `true` ao encerrar a primeira rodada; `SeasonRules.ApplyConfig` recusa mudanças depois disso.
- Datas/hora sempre UTC (`TimeProvider`); o timer é derivado de `StartedAt + DurationMinutes`.
- Sorteio usa `IRandomSource` (`CryptoRandomSource` em produção, `SeededRandomSource` nos testes).
- Mudou entidade? Gere migration e versione-a junto.

## Decisões sobre pontos ambíguos da spec

- `.NET 9` (único SDK instalado; migrar para .NET 10 = trocar TargetFramework e imagens do Dockerfile).
- "Partidas" e "% vitória" **não contam bye**. Bye conta só pontos e a coluna Byes.
- Estatísticas de comandantes: o **conjunto de comandantes do deck é uma linha** ("Thrasios + Tymna" ≠ "Tymna + Kraum").
- **Várias temporadas podem estar ativas**; `GET /api/rounds/current` devolve uma lista (rodadas Sorteadas/Em andamento de todas as ativas).
- Itens [EM ABERTO] viraram config da temporada: `PointsBye2`, `PointsBye3`, `DefaultRoundMinutes`, `Tiebreakers`
  (lista ordenada de `Wins`, `FewerMatches`, `MoreMatches`, `Draws`, `WinRate`; pontos sempre primeiro, nome sempre por último),
  `ExtraTurnsAfterTime` (informativo, regra "acabou o tempo, mais N turnos"), `MinMatchesForCommanderWinRate`.
- Bye: prioridade para quem tem menos byes em outras rodadas já sorteadas da temporada.
- Alterar presentes de uma rodada `Drawn` desfaz o sorteio (volta a `Draft`). Rodada só pode ser excluída antes de iniciar.
- Resultado só pode ser lançado com a rodada `InProgress` (ou corrigido depois de `Closed`, gravando `UpdatedAt/UpdatedBy`).
- Estatísticas de comandante usam os comandantes **atuais** do deck (editar um deck altera o histórico dele).
