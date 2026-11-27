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
| Regulamento (o "tratado") | `Regulamento` (HTML do editor visual) | `Regulamentos` |
| Configurações da loja | `ConfiguracaoLoja` (link do grupo de WhatsApp) | `ConfiguracoesLoja` |
| Situações | `SituacaoRodada` (Rascunho, Sorteada, EmAndamento, Encerrada), `SituacaoDeck` (Pendente, Aprovado, Reprovado), `ResultadoMesa` (Pendente, Vitoria, Empate), `CriterioDesempate`, `ModoPontuacao` (ColocacaoNaEtapa, SomaDasRodadas) | gravadas como texto |

## Estrutura (arquitetura limpa)

```
Commander500.sln                   6 projetos; F5 na Commander500.Api (perfil http) sobe banco + Api + site (SpaProxy)
src/Commander500.Dominio/          não depende de nada
  Entidades/                       Temporada, Jogador, Comandante, Deck, Etapa, Rodada (+ Participacao, Mesa, Cadeira), Administrador, Regulamento, ConfiguracaoLoja
  Enumeradores/                    SituacaoRodada, SituacaoDeck, ResultadoMesa, CriterioDesempate, ModoPontuacao
  Regras/                          regras puras e testáveis: FluxoRodada (máquina de estados, mesas à mão, quem começa),
                                   SorteadorMesas, CalculadoraRanking (soma das rodadas), CalculadoraRankingEtapa,
                                   CalculadoraRankingTemporada (escolhe o modo; colocação + final), CalculadoraEstatisticasComandante,
                                   RegrasEtapa (etapas por data, mesa final), RegrasTemporada, RegrasAdministrador, RegrasFoto, RegrasLoja,
                                   RegrasHallDaFama, RegrasDeckDoJogador (deck reserva), CalculadoraDesempenho (evolução mensal, nêmesis, desempenho por deck),
                                   GeradorAleatorio
  Comum/                           Excecoes (RegraNegocio, DadosInvalidos, NaoEncontrado, ServicoIndisponivel), NomeComandante, FusoLoja
src/Commander500.Aplicacao/        casos de uso (serviços de aplicação, sem MediatR)
  Servicos/                        ServicoTemporada, ServicoRodada, ServicoJogador, ServicoDeck, ServicoComandante,
                                   ServicoEstatisticas, ServicoAutenticacao, ServicoAdministrador, ServicoRegulamento,
                                   ServicoFotos, ServicoConfiguracaoLoja, ServicoHallDaFama, ServicoPaginas (jogadores e comandantes)
  Modelos/                         Envios.cs (o que o site manda) e Respostas.cs (o que a Api devolve)
  Interfaces/                      IContextoBanco, IClienteScryfall, IServicoSenha, IGeradorToken, IArmazenamentoFotos (implementados na Infraestrutura)
  Comum/                           Mapeamento (entidade → resposta), ConsultasRodada (Includes)
src/Commander500.Infraestrutura/   EF Core + PostgreSQL e serviços técnicos
  Dados/                           ContextoBanco, Configuracoes/, Migracoes/, SemeadorBanco, RegulamentoInicial (texto do tratado), StringConexao
  Scryfall/                        ClienteScryfall (cache + 100 ms entre chamadas), ServicoEnriquecimentoScryfall (rotina em 2º plano)
  Seguranca/                       OpcoesJwt, GeradorTokenJwt, ServicoSenha
  Fotos/                           OpcoesFotos, ArmazenamentoFotosSupabase (produção), ArmazenamentoFotosLocal (dev, pasta fotos-local/)
src/Commander500.Api/              Program.cs, Endpoints/ (EndpointsPublicos, EndpointsAdmin), Comum/TratadorErros
tests/Commander500.Testes/         xUnit, testa Dominio/Regras com grafo em memória (MontadorRodada, FabricaDecks)
web/                               React 19 + Vite + TS + Tailwind v4, mobile-first (projeto Commander500.Web.esproj na solução)
  src/api/                         cliente.ts (fetch + JWT), tipos.ts (espelha Modelos/), consultas.ts (TanStack Query)
  src/autenticacao/                ContextoAutenticacao (login do admin)
  src/componentes/                 DesenhoMesa (mesa retangular), Cronometro, QuadroRodada, Layout (logo + tema), ArteComandante,
                                   RankingComandantes, SeletorComandante (busca Scryfall), JanelaDeck, JanelaPerfilJogador,
                                   MontagemMesas (mesas à mão + quem começa), EditorRegulamento (TipTap), Fotos (JanelaFoto, FotoDestaque),
                                   FundoAnimado (brasas e símbolos de mana), GraficoEvolucao (taxa de vitória por mês),
                                   Paginacao, FiltroCores, Numeros, comum.tsx
  src/paginas/publico/             PaginaInicial (banner da temporada + próxima etapa), PaginaTemporada (ranking/etapas/comandantes),
                                   PaginaComandantes, PaginasAoVivo (Ao vivo + Rodada), PaginaRegras, PaginaHallDaFama,
                                   PaginaListaJogadores (/jogadores), PaginaJogador (/jogadores/:id), PaginaComandante (/comandantes/:chave)
  src/paginas/admin/               PaginaLogin, PaginaEtapasRodadas, PaginaConducaoRodada, PaginaJogadores, PaginaDecks,
                                   PaginaTemporadas (pontuação, próxima etapa, mesa final), PaginaEditarRegras, PaginaAdministradores, PaginaConfiguracoes
  src/utilitarios/                 formatacao, relogio, tema, comandantes, fotos (reduz a foto antes de enviar), paginacao (usePaginacao)
docker-compose.yml                 Postgres local (serviço "banco") + perfil "api" opcional
src/Dockerfile                     imagem da Api (contexto = pasta src/; usada pela Vercel, docker-compose e Render)
scripts/dev.ps1, dev.sh            sobe banco + Api (que abre o site) com um comando (guia completo no README.md)
.vscode/                           F5 no VS Code (mesmo fluxo do Visual Studio)
vercel.json, .vercelignore        publicação na Vercel: serviço "site" (web/, Vite) + serviço "api" (src/, container) no mesmo domínio
.github/workflows/publicar.yml     merge na main → dotnet test + build do site → vercel deploy --prod
.github/workflows/manter-supabase-acordado.yml   quarta 14h e sábado 12h (Brasília): GET /api/temporadas para o Supabase não pausar
docs/DEPLOY.md, docs/MANUAL-PEDRO.md   passo a passo da publicação (Vercel + Supabase) e dos acessos
render.yaml                        plano B (Api no Render), não usado
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

`VITE_API_URL` (ver `web/.env.example`) fica vazio em dev e na Vercel (site e Api no mesmo domínio); só é usado se a Api for para outro domínio.
`dotnet ef` é ferramenta local (`.config/dotnet-tools.json`); rode `dotnet tool restore` numa máquina nova.
Swagger: `/swagger` (a raiz `/` redireciona; o perfil **so-api** abre o navegador nele, o perfil **http** abre o site). Saúde: `/saude`.
Admin de dev: `admin@fantasystore.local` / `admin123`.
A porta 5080 também é usada pela Gestão Rastreio Wiki (outro projeto do Pedro): não dá para rodar as duas Apis ao mesmo tempo nela.

## Configuração (variáveis de ambiente)

| Variável | Uso |
|---|---|
| `ConnectionStrings__Padrao` ou `DATABASE_URL` | aceita `Host=...` ou `postgres://usuario:senha@host/banco?sslmode=require` |
| `Jwt__Chave` | obrigatório, ≥ 32 caracteres (a Api não sobe sem) |
| `Admin__Email`, `Admin__Senha` | só no primeiro acesso: cria esse admin quando a tabela `Administradores` está vazia (depois, cadastro pela tela) |
| `Cors__OrigensPermitidas` | lista separada por vírgula (`*` libera tudo) |
| `Banco__MigrarAoIniciar` | padrão `true` |
| `Semente__DadosDemonstracao` | `true` cria 1 temporada + 10 jogadores/decks + 3 rodadas encerradas se não houver temporadas |
| `Swagger__Habilitado` | padrão `true` |
| `Scryfall__Habilitado` | padrão `true`: rotina em 2º plano que busca arte/cores dos comandantes salvos só com o nome |
| `Fotos__SupabaseUrl`, `Fotos__SupabaseChave`, `Fotos__Bucket` | fotos no Supabase Storage (bucket público `fotos`); sem elas, pasta local `fotos-local/` (só dev) |
| `PORT` | se definido, a Api escuta nessa porta (Dockerfile e Vercel: `8080`) |

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
  Ranking da etapa (`CalculadoraRankingEtapa`): pontos das rodadas ↓ (vitória, empate, derrota e bye com os valores da
  temporada) → byes ↑ (quem teve bye fica abaixo de quem venceu a mesa) → confronto direto ↓ (vitórias em mesas contra os
  outros empatados) → persistindo, **dividem a posição**. Só rodadas encerradas contam.
- **Comandantes vêm do Scryfall** (API pública gratuita, sem chave; respeitar User-Agent e ~10 req/s). Não há
  cadastro manual: o comandante é criado ao salvar um deck (pelo id do Scryfall, ou só pelo nome se o Scryfall
  estiver fora — o `ServicoEnriquecimentoScryfall` completa a arte depois). As imagens são do CDN do Scryfall.
- Nome do deck é opcional: o padrão é o(s) nome(s) do(s) comandante(s). Estatísticas usam sempre os comandantes.
- Página pública de comandantes inclui "mais decks cadastrados" (conta decks cadastrados, mesmo sem partidas).
- **Campeão da temporada** = 1º do ranking de uma temporada encerrada (inativa). Campeão da etapa = posição 1
  (empatados dividem) de uma etapa com todas as rodadas encerradas.
- Mini perfil do jogador é público (sem a observação de revisão dos decks); no admin mostra a situação e permite cadastrar deck.
- **Arte do jogador sem partidas** (27/11/2026): na lista de jogadores a arte é o deck que ele mais jogou; quem ainda não
  jogou usa o deck reserva (`RegrasDeckDoJogador.DeckReserva`): aprovado → pendente → reprovado, com lista antes, depois o
  cadastrado mais recente, depois o nome.
- **Lista do deck na página do jogador** (27/11/2026): cada card mostra "Sem lista" ou um selo que é o link da lista, com a
  revisão do lojista (Lista aprovada / pendente / recusada, cores de `estiloSituacaoDeck`). A situação é pública; a
  observação da revisão continua só no admin.
- Tema claro/escuro: as escalas `zinc` (azul-marinho) e `amber` (laranja da logo) do Tailwind são redefinidas em
  `web/src/index.css` e invertidas em `html[data-tema="claro"]`; use essas escalas nos componentes para funcionar
  nos dois temas. Cores fixas da logo: `brand-red`, `brand-orange`, `brand-navy`.
- Nova rodada numa etapa que já tem rodadas **copia presentes e decks da rodada anterior**; o lojista só desmarca
  quem foi embora (os resultados dele na etapa continuam valendo).
- Sorteio dentro da etapa (`HistoricoEtapa` + `SorteadorMesas`): tenta 300 arranjos aleatórios e fica com o que menos
  repete adversários das rodadas anteriores da etapa (peso alto) e cadeiras da última rodada (peso baixo).
  Nem sempre dá zero: com poucos jogadores algumas repetições são inevitáveis. A prioridade de bye não muda.
- **Dois modos de pontuação** (`Temporada.ModoPontuacao`, decisão de 18/11/2026, tratado Art. 5º e 6º):
  - `ColocacaoNaEtapa` (**padrão**): cada etapa tem ranking pelos pontos das rodadas (vitória/empate/derrota/bye); a colocação
    vira pontos na temporada pela tabela `PontosPorColocacao` ("5,4,3,2") + `PontosDemaisColocados` (1). **Empatados ganham os
    pontos da melhor posição.** Só etapa concluída (todas as rodadas encerradas) pontua. Desempate da temporada: mais 1º lugares →
    mais etapas disputadas → melhor colocação na última etapa que os empatados disputaram juntos → persistindo, mesma posição.
  - `SomaDasRodadas`: os pontos das rodadas somam direto (desempate pelos `CriteriosDesempate`).
  - `PontosDerrota` = presença de quem jogou e perdeu (vale nos dois modos); quem faltou fica com 0.
- **Mesa final** (só no modo por colocação): `Etapa.Final = true` + `RodadasPrevistas`. O lojista escolhe os finalistas (sugestão =
  top 4), a data e quantas rodadas; a colocação na final soma pela mesma tabela. **Campeão = 1º no total depois da final**
  (não o vencedor da mesa final). A final pode cair no mesmo dia de uma etapa (índice único é `IdTemporada, Data, Final`).
- **Rodada retroativa** = data anterior a hoje no fuso da loja (`FusoLoja`, Brasília). Aceita resultado e encerramento direto
  de `Sorteada`, sem timer. Mesas podem ser montadas à mão (2 a 6 cadeiras; quem fica de fora é bye) em qualquer rodada.
- **Quem começa**: trocar o jogador que começa gira as cadeiras mantendo a ordem de jogo (o escolhido vira a cadeira 1).
- **Bye no mesmo dia**: o sorteio prefere quem ainda não ficou de bye na etapa; só depois olha os byes da temporada.
- Depois de encerrar uma rodada aparece "Registrar rodada N" (mesma etapa, mesmos presentes); na final para nas rodadas previstas.
- **Próxima etapa**: `Temporada.ProximaEtapaEm` (UTC) e `PrazoListas` aparecem no banner da página inicial; não dependem da trava.
- **Regulamento**: um registro com HTML do editor visual (TipTap), mostrado em `/regras` (sanitizado com DOMPurify) e editado
  em `/admin/regras`. O semeador grava o texto do tratado quando o banco ainda não tem regulamento.
- **Publicação** (decisão de 19/11/2026): Vercel (site + Api no mesmo projeto, Api pelo *Container Runtime*, em Beta) +
  Supabase (Postgres, Session pooler, Data API desligada). Sem CORS/`VITE_API_URL` em produção. Deploy pelo GitHub
  Actions com token porque o repositório é privado de outra conta (o Hobby da Vercel não publica commits de terceiros).
- **Administradores** (19/11/2026): cadastrados pela tela `/admin/administradores` (qualquer admin cria, troca senha e
  exclui; ninguém exclui a si mesmo nem o último). A configuração só cria o primeiro. O `OnTokenValidated` do JWT confere
  se o admin ainda existe, então quem é excluído perde o acesso na hora (1 consulta por requisição do admin).
- **Supabase com RLS ligado** em todas as tabelas, sem políticas (a Api usa `postgres`, que tem BYPASSRLS) + gatilho
  `ligar_rls_tabelas_novas` para as tabelas criadas por migrations. Produção: `fantasy500.vercel.app`.
- **Fotos do top** (21/11/2026): etapa e temporada têm `CaminhoFoto` + `LegendaFoto`. O site reduz a foto para 1600 px
  em JPEG antes de enviar; a Api valida (JPG/PNG/WEBP, até 5 MB) e grava pelo `IArmazenamentoFotos`. O nome do arquivo
  leva a data (`etapas/12-20261121143005.jpg`) para nunca vir do cache; trocar a foto apaga a antiga.
- **Hall da Fama** (`/hall-da-fama`): só temporadas encerradas; galeria com o pódio + ranking final completo da escolhida.
  O deck de cada jogador é o que ele mais usou na temporada (empate: o mais recente).
- **Grupo de WhatsApp**: link em Admin → Configurações (só aceita https de chat.whatsapp.com / wa.me); aparece no menu e no rodapé.
- **Correções em rodada encerrada** (resultado e deck do jogador) só enquanto a temporada estiver aberta.
- **Empate parcial** (27/11/2026): `Cadeira.Empatou` marca quem ficou no empate (ex.: 2 sobreviventes de 4); os outros da
  mesa contam como derrota (pontos de derrota/presença). Empate sem ninguém marcado = entre todos (`Mesa.Empatou(id)`).
- **Páginas do jogador e do comandante**: `/jogadores/:id` e `/comandantes/:chave` (chave = ids dos comandantes em ordem,
  "12-34" para dupla). Título do comandante = temporada encerrada em que o campeão jogou mais com ele. Melhor deck = maior
  % com 3+ partidas. Nêmesis = quem mais venceu mesas com o jogador junto. No ranking/mesas o nome abre a janela do jogador,
  com "Ver perfil completo" abrindo a página em outra aba.
- **Filtro de cor dos comandantes** = exatamente as cores marcadas (C = incolor). **Paginação** no navegador: 50 por página
  (100/250/500) em comandantes, jogadores, ranking da temporada, Hall da Fama e admin (jogadores e decks).
- Arquitetura limpa com **serviços de aplicação** (sem MediatR) e banco **PostgreSQL** com nomes de tabela = nome do DbSet
  (sem `TB_`). Decisão do Pedro em 18/11/2026; as migrations recomeçaram do zero (`Inicial`).
