# Commander 500 – Fantasy Store

Liga de Commander 500 (MTG). Especificação completa em [SPEC.md](SPEC.md) — leia antes de mudar regras de negócio.

## Nomes em português (regra do projeto)

Classes, métodos, variáveis, propriedades, tabelas, colunas, rotas da Api, campos do JSON, componentes React,
pastas e comentários ficam **em português**, para quem não lê inglês com fluência conseguir debugar. Só fica em inglês
o que é da linguagem/framework: palavras do C#/TS, tipos e métodos do .NET/React/EF (`Task`, `DbSet`, `useState`,
`Include`…), sufixos consagrados (`Exception`, `Async`, `Program`, `Api`), hooks começando com `use` (regra do React)
e o JSON que vem do Scryfall. Estilo igual ao da Gestão Rastreio Wiki (sem o prefixo `TB_` nas tabelas).

| Conceito | Nome no código | Tabela |
|---|---|---|
| Temporada / Etapa / Rodada | `Temporada`, `Etapa`, `Rodada` | `Temporadas`, `Etapas`, `Rodadas` |
| Jogador, Deck, Comandante | `Jogador`, `Deck`, `Comandante`, `DeckComandante` | `Jogadores`, `Decks`, `Comandantes`, `DecksComandantes` |
| Presença na rodada | `Participacao` (com `Bye`) | `Participacoes` |
| Mesa / cadeira | `Mesa`, `Cadeira` | `Mesas`, `Cadeiras` |
| Usuário do lojista | `Administrador` | `Administradores` |
| Situações | `SituacaoRodada` (Rascunho, Sorteada, EmAndamento, Encerrada), `SituacaoDeck` (Pendente, Aprovado, Reprovado), `ResultadoMesa` (Pendente, Vitoria, Empate), `CriterioDesempate` | gravadas como texto |

## Estrutura (arquitetura limpa)

```
Commander500.sln
src/Commander500.Dominio/          não depende de nada
  Entidades/                       Temporada, Jogador, Comandante, Deck, Etapa, Rodada (+ Participacao, Mesa, Cadeira), Administrador
  Enumeradores/                    SituacaoRodada, SituacaoDeck, ResultadoMesa, CriterioDesempate
  Regras/                          regras puras e testáveis: FluxoRodada (máquina de estados), SorteadorMesas,
                                   CalculadoraRanking, CalculadoraRankingEtapa, CalculadoraEstatisticasComandante,
                                   RegrasEtapa, RegrasTemporada, GeradorAleatorio
  Comum/                           Excecoes (RegraNegocio, DadosInvalidos, NaoEncontrado, ServicoIndisponivel), NomeComandante
src/Commander500.Aplicacao/        casos de uso (serviços de aplicação, sem MediatR)
  Servicos/                        ServicoTemporada, ServicoRodada, ServicoJogador, ServicoDeck, ServicoComandante,
                                   ServicoEstatisticas, ServicoAutenticacao
  Modelos/                         Envios.cs (o que o site manda) e Respostas.cs (o que a Api devolve)
  Interfaces/                      IContextoBanco, IClienteScryfall, IServicoSenha, IGeradorToken (implementados na Infraestrutura)
  Comum/                           Mapeamento (entidade → resposta), ConsultasRodada (Includes)
src/Commander500.Infraestrutura/   EF Core + PostgreSQL e serviços técnicos
  Dados/                           ContextoBanco, Configuracoes/, Migracoes/, SemeadorBanco, StringConexao
  Scryfall/                        ClienteScryfall (cache + 100 ms entre chamadas), ServicoEnriquecimentoScryfall (rotina em 2º plano)
  Seguranca/                       OpcoesJwt, GeradorTokenJwt, ServicoSenha
src/Commander500.Api/              Program.cs, Endpoints/ (EndpointsPublicos, EndpointsAdmin), Comum/TratadorErros
tests/Commander500.Testes/         xUnit, testa Dominio/Regras com grafo em memória (MontadorRodada, FabricaDecks)
web/                               React 19 + Vite + TS + Tailwind v4, mobile-first
  src/api/                         cliente.ts (fetch + JWT), tipos.ts (espelha Modelos/), consultas.ts (TanStack Query)
  src/autenticacao/                ContextoAutenticacao (login do admin)
  src/componentes/                 DesenhoMesa (mesa retangular), Cronometro, QuadroRodada, Layout (logo + tema), ArteComandante,
                                   RankingComandantes, SeletorComandante (busca Scryfall), JanelaDeck, JanelaPerfilJogador, comum.tsx
  src/paginas/publico/             PaginaInicial (banner da temporada), PaginaTemporada (ranking/etapas/comandantes),
                                   PaginaComandantes, PaginasAoVivo (Ao vivo + Rodada)
  src/paginas/admin/               PaginaLogin, PaginaEtapasRodadas, PaginaConducaoRodada, PaginaJogadores, PaginaDecks, PaginaTemporadas
  src/utilitarios/                 formatacao, relogio, tema, comandantes
docker-compose.yml                 Postgres local (serviço "banco") + perfil "api" opcional
Dockerfile                         imagem da Api (contexto = raiz do repo)
scripts/dev.ps1, dev.sh            sobe banco + Api + site com um comando (guia completo no README.md)
render.yaml, web/vercel.json, docs/DEPLOY.md   publicação (Render + Neon + Vercel) — preparada, ainda não usada
```

Caminho de uma requisição: tela React → `consultas.ts` → endpoint (Api) → serviço (Aplicação) → regras (Domínio) → `IContextoBanco` (EF).

## Comandos

```bash
docker compose up -d --wait banco              # Postgres local (usuário/senha/banco: commander500)
dotnet build
dotnet test
dotnet run --project src/Commander500.Api      # http://localhost:5080/swagger (Development, migra e faz seed)
dotnet ef migrations add <Nome> --project src/Commander500.Infraestrutura --output-dir Dados/Migracoes
docker compose --profile api up --build        # Api + banco em Docker (porta 8080)
docker compose down -v                         # zera o banco local
```

Frontend (pasta `web/`, precisa da Api rodando em :5080 — o Vite repassa `/api` e `/saude`):

```bash
npm install --prefix web
npm run dev --prefix web                       # http://localhost:5173  (Api em outra porta: ALVO_API=http://localhost:5081)
npm run build --prefix web                     # tsc -b + vite build → web/dist
npm run lint --prefix web                      # oxlint
```

`VITE_API_URL` (ver `web/.env.example`) fica vazio em dev; em produção é o endereço da Api.
`dotnet ef` é ferramenta local (`.config/dotnet-tools.json`); rode `dotnet tool restore` numa máquina nova.
Swagger: `/swagger` (a raiz `/` redireciona; o perfil **http** do Visual Studio abre o navegador nele). Saúde: `/saude`.
Admin de dev: `admin@fantasystore.local` / `admin123`.
A porta 5080 também é usada pela Gestão Rastreio Wiki (outro projeto do Pedro): não dá para rodar as duas Apis ao mesmo tempo nela.

## Configuração (variáveis de ambiente)

| Variável | Uso |
|---|---|
| `ConnectionStrings__Padrao` ou `DATABASE_URL` | aceita `Host=...` ou `postgres://usuario:senha@host/banco?sslmode=require` |
| `Jwt__Chave` | obrigatório, ≥ 32 caracteres (a Api não sobe sem) |
| `Admin__Email`, `Admin__Senha` | cria o admin ao iniciar se não existir; se existir e a senha mudou, atualiza |
| `Cors__OrigensPermitidas` | lista separada por vírgula (`*` libera tudo) |
| `Banco__MigrarAoIniciar` | padrão `true` |
| `Semente__DadosDemonstracao` | `true` cria 1 temporada + 10 jogadores/decks + 3 rodadas encerradas se não houver temporadas |
| `Swagger__Habilitado` | padrão `true` |
| `Scryfall__Habilitado` | padrão `true`: rotina em 2º plano que busca arte/cores dos comandantes salvos só com o nome |
| `PORT` | se definido (Render), a Api escuta nessa porta |

## Convenções

- Regra de negócio nova vai em `Dominio/Regras` como função pura + teste em `tests/`. Os serviços da Aplicação só
  carregam (via `IContextoBanco`), chamam o domínio e salvam. A Api só recebe a requisição e chama o serviço.
- A Aplicação usa só o EF Core genérico (`Include`, `ToListAsync`, `AsSplitQuery`); o PostgreSQL fica na Infraestrutura.
- Erros: `DadosInvalidosException` → 400, `NaoEncontradoException` → 404, `RegraNegocioException` → 409,
  `ServicoIndisponivelException` → 503 (ProblemDetails com a mensagem em `detail`).
- **Pontos nunca são gravados**: ranking e estatísticas são calculados a cada requisição a partir das rodadas `Encerrada`.
- `Temporada.ConfiguracaoTravada` vira `true` ao encerrar a primeira rodada; `RegrasTemporada.AplicarConfiguracao` recusa mudanças depois disso.
- Datas/horas sempre UTC (`TimeProvider`); o timer é calculado por `IniciadaEm + DuracaoMinutos`.
- Sorteio usa `IGeradorAleatorio` (`GeradorAleatorioCriptografico` em produção, `GeradorAleatorioComSemente` nos testes e no seed).
- Consultas só de leitura que usam `ConsultasRodada.ComTudo()` precisam de `AsNoTrackingWithIdentityResolution` (há o ciclo Rodada → Etapa → Rodadas).
- Mudou entidade? Gere migration (comando acima) e versione-a junto.
- CSS: as classes próprias são `botao`, `botao-primario`, `botao-secundario`, `botao-perigo`, `botao-discreto`, `botao-pequeno`,
  `cartao`, `rotulo` (em `web/src/index.css`). Tema: `html[data-tema="claro"|"escuro"]`.

## Decisões sobre pontos ambíguos da spec

- `.NET 9` (único SDK instalado; migrar para .NET 10 = trocar TargetFramework nos 5 `.csproj` e as imagens do Dockerfile).
- "Partidas" e "% vitória" **não contam bye**. Bye conta só pontos e a coluna Byes.
- Estatísticas de comandantes: o **conjunto de comandantes do deck é uma linha** ("Thrasios + Tymna" ≠ "Tymna + Kraum").
- **Várias temporadas podem estar ativas**; `GET /api/rodadas/atuais` devolve uma lista (rodadas Sorteadas/Em andamento de todas as ativas).
- Itens [EM ABERTO] viraram configuração da temporada: `PontosBye2`, `PontosBye3`, `MinutosPadraoRodada`, `CriteriosDesempate`
  (lista ordenada de `Vitorias`, `MenosPartidas`, `MaisPartidas`, `Empates`, `PercentualVitoria`; pontos sempre primeiro, nome sempre por último),
  `TurnosExtrasAposTempo` (informativo, regra "acabou o tempo, mais N turnos"), `MinimoPartidasPercentualComandante`.
- Bye: prioridade para quem tem menos byes em outras rodadas já sorteadas da temporada.
- Alterar presentes de uma rodada `Sorteada` desfaz o sorteio (volta a `Rascunho`). Rodada só pode ser excluída antes de iniciar.
- Resultado só pode ser lançado com a rodada `EmAndamento` (ou corrigido depois de `Encerrada`, gravando `AlteradaEm/AlteradaPor`).
- Estatísticas de comandante usam os comandantes **atuais** do deck (editar um deck altera o histórico dele).
- **Etapa** = um dia de liga dentro da temporada, com 1+ rodadas. É criada automaticamente pela data da rodada
  (`RegrasEtapa`), numerada por data; apagar a última rodada de uma etapa apaga a etapa.
  Ranking da etapa (`CalculadoraRankingEtapa`, prêmio do dia): vitórias ↓ (**bye conta como vitória**) → byes ↑
  (quem teve bye fica abaixo) → confronto direto ↓ (vitórias em mesas contra os outros empatados) → persistindo,
  **dividem a posição**. Só rodadas encerradas contam.
- **Comandantes vêm do Scryfall** (API pública gratuita, sem chave; respeitar User-Agent e ~10 req/s). Não há
  cadastro manual: o comandante é criado ao salvar um deck (pelo id do Scryfall, ou só pelo nome se o Scryfall
  estiver fora — o `ServicoEnriquecimentoScryfall` completa a arte depois). As imagens são do CDN do Scryfall.
- Nome do deck é opcional: o padrão é o(s) nome(s) do(s) comandante(s). Estatísticas usam sempre os comandantes.
- Página pública de comandantes inclui "mais decks cadastrados" (conta decks cadastrados, mesmo sem partidas).
- **Campeão da temporada** = 1º do ranking de uma temporada encerrada (inativa). Campeão da etapa = posição 1
  (empatados dividem) de uma etapa com todas as rodadas encerradas.
- Mini perfil do jogador é público (sem a observação de revisão dos decks); no admin mostra a situação e permite cadastrar deck.
- Tema claro/escuro: as escalas `zinc` (azul-marinho) e `amber` (laranja da logo) do Tailwind são redefinidas em
  `web/src/index.css` e invertidas em `html[data-tema="claro"]`; use essas escalas nos componentes para funcionar
  nos dois temas. Cores fixas da logo: `brand-red`, `brand-orange`, `brand-navy`.
- Nova rodada numa etapa que já tem rodadas **copia presentes e decks da rodada anterior**; o lojista só desmarca
  quem foi embora (os resultados dele na etapa continuam valendo).
- Sorteio dentro da etapa (`HistoricoEtapa` + `SorteadorMesas`): tenta 300 arranjos aleatórios e fica com o que menos
  repete adversários das rodadas anteriores da etapa (peso alto) e cadeiras da última rodada (peso baixo).
  Nem sempre dá zero: com poucos jogadores algumas repetições são inevitáveis. A prioridade de bye não muda.
- Arquitetura limpa com **serviços de aplicação** (sem MediatR) e banco **PostgreSQL** com nomes de tabela = nome do DbSet
  (sem `TB_`). Decisão do Pedro em 18/11/2026; as migrations recomeçaram do zero (`Inicial`).
