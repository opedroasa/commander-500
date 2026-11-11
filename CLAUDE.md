# Commander 500 – Fantasy Store

Liga de Commander 500 (MTG). Especificação completa em [SPEC.md](SPEC.md) — leia antes de mudar regras de negócio.

## Estrutura

```
Commander500.sln
src/Commander500.Api/        ASP.NET Core (.NET 9) minimal APIs + EF Core/Npgsql
  Domain/                    entidades, exceções, CommanderName
  Domain/Services/           regras puras e testáveis (sem EF): RankingCalculator, StageRankingCalculator,
                             TableDrawer, RoundWorkflow (máquina de estados), StageRules,
                             CommanderStatsCalculator, SeasonRules
  Application/               serviços que carregam/salvam via DbContext e chamam o Domain
  Endpoints/                 PublicEndpoints (sem login) e AdminEndpoints (JWT, policy "Admin")
  Data/                      AppDbContext, DbSeeder, Migrations/
  Infrastructure/            ErrorHandler (exceção → ProblemDetails), conversão de URI postgres://,
                             Scryfall/ (cliente HTTP com cache + job que completa arte dos comandantes)
tests/Commander500.Tests/    xUnit, só testa Domain/Services (grafo em memória via RoundBuilder)
web/                         React 19 + Vite + TS + Tailwind v4, mobile-first
  src/api/                   client.ts (fetch + JWT), types.ts (espelha Contracts/Dtos.cs), hooks.ts (TanStack Query)
  src/components/            TableView (mesa retangular), Countdown, RoundBoard, Layout (logo + tema), CommanderArt,
                             CommanderRankings, CommanderPicker (busca Scryfall), DeckFormModal, PlayerProfileModal, ui.tsx
  src/pages/public/          Início (banner da temporada), Detalhe (ranking/etapas/comandantes), Comandantes, Ao vivo, Rodada
  src/pages/admin/           Login, Etapas e rodadas, Condução da rodada, Jogadores (perfil + decks), Decks, Temporadas
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
Swagger: `/swagger` (a raiz `/` redireciona; o perfil **http** do Visual Studio abre o navegador nele). Health: `/health`. Admin de dev: `admin@fantasystore.local` / `admin123`.

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
| `Scryfall__Enabled` | padrão `true`: job em segundo plano que busca arte/cores dos comandantes salvos só com o nome |
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
- **Etapa** = um dia de liga dentro da temporada, com 1+ rodadas. É criada automaticamente pela data da rodada
  (`StageRules`), numerada por data; apagar a última rodada de uma etapa apaga a etapa.
  Ranking da etapa (`StageRankingCalculator`, prêmio do dia): vitórias ↓ (**bye conta como vitória**) → byes ↑
  (quem teve bye fica abaixo) → confronto direto ↓ (vitórias em mesas contra os outros empatados) → persistindo,
  **dividem a posição**. Só rodadas encerradas contam.
- **Comandantes vêm do Scryfall** (API pública gratuita, sem chave; respeitar User-Agent e ~10 req/s). Não há
  cadastro manual: o comandante é criado ao salvar um deck (por id do Scryfall ou só pelo nome, se o Scryfall
  estiver fora — o `ScryfallEnrichmentService` completa a arte depois). As imagens são do CDN do Scryfall.
- Nome do deck é opcional: o padrão é o(s) nome(s) do(s) comandante(s). Estatísticas usam sempre os comandantes.
- Página pública de comandantes inclui "mais decks cadastrados" (conta decks registrados, mesmo sem partidas).
- **Campeão da temporada** = 1º do ranking de uma temporada encerrada (inativa). Campeão da etapa = posição 1
  (empatados dividem) de uma etapa com todas as rodadas encerradas.
- Mini perfil do jogador é público (sem observações de revisão dos decks); no admin mostra status e permite cadastrar deck.
- Tema claro/escuro: as escalas `zinc` (azul-marinho) e `amber` (laranja da logo) do Tailwind são redefinidas em
  `web/src/index.css` e invertidas em `html[data-theme="light"]`; use essas escalas nos componentes para funcionar
  nos dois temas. Cores fixas da logo: `brand-red`, `brand-orange`, `brand-navy`.
- Nova rodada numa etapa que já tem rodadas **copia presentes e decks da rodada anterior**; o lojista só desmarca
  quem foi embora (os resultados dele na etapa continuam valendo).
- Sorteio dentro da etapa (`StageHistory` + `TableDrawer`): tenta 300 arranjos aleatórios e fica com o que menos
  repete adversários das rodadas anteriores da etapa (peso alto) e cadeiras da última rodada (peso baixo).
  Nem sempre dá zero: com poucos jogadores algumas repetições são inevitáveis. A prioridade de bye não muda.
