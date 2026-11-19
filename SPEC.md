# Commander 500 – Fantasy Store · Especificação do MVP

> Documento de referência para implementação. Itens marcados **[EM ABERTO]** ainda dependem de decisão do lojista; implemente-os como configuração, com o valor padrão indicado.

## 1. Contexto

Site para a liga de **Commander 500** (Magic: The Gathering) da loja **Fantasy Store**. A liga é organizada em **temporadas**; cada temporada tem várias **rodadas** (normalmente um fim de semana a cada 1–2 semanas). Em cada rodada os jogadores presentes são sorteados em **mesas de 4**, jogam, e o lojista registra o resultado. Pontos acumulam na temporada; no fim, o lojista premia conforme o ranking.

O orçamento do deck (teto pelo menor preço da Liga Magic) é validado **fora do sistema**. O sistema só guarda o link da lista e um status de aprovação para gestão.

## 2. Stack e hospedagem

| Camada | Tecnologia | Hospedagem (free) |
|---|---|---|
| Backend | ASP.NET Core (.NET 8 LTS ou mais recente), EF Core, Npgsql | Vercel (container Docker) |
| Banco | PostgreSQL | Supabase (prod) · Docker local (dev) |
| Frontend | React + Vite + TypeScript + Tailwind, mobile-first | Vercel (mesmo projeto e domínio da API) |
| Auth | JWT para o(s) admin(s) – usuário do lojista via seed/variável de ambiente | — |

Requisitos de portabilidade: Dockerfile para a API, connection string e segredos via variável de ambiente, migrations do EF versionadas, CORS configurável. Endpoint `GET /saude` para verificar se a API está no ar.

## 3. Glossário

- **Temporada (Season)**: um circuito da liga, com início, fim e configuração de pontuação própria.
- **Rodada (Round)**: um dia/evento de jogo dentro da temporada. Se num mesmo dia houver mais de um jogo por jogador, cada jogo é uma rodada separada.
- **Mesa (Table/Pod)**: grupo de 4 jogadores com posições (cadeiras 1–4) sorteadas.
- **Bye**: jogador presente que ficou fora das mesas porque o número de presentes não fecha múltiplo de 4. (O termo correto é *bye*; "buyback" é uma mecânica de carta do Magic.)
- **Deck**: pertence a um jogador; tem 1 ou 2 comandantes (parceiros/background) e link opcional da lista.

## 4. Regras de negócio

### 4.1 Pontuação (configurável por temporada)
| Configuração | Padrão |
|---|---|
| Pontos por vitória (só o vencedor da mesa pontua) | 3 |
| Pontos por empate / tempo esgotado (cada jogador da mesa) | 1 |
| Pontos por bye quando 1 jogador fica de fora | 3 |
| Pontos por bye quando 2 jogadores ficam de fora | **[EM ABERTO]** padrão 3 |
| Pontos por bye quando 3 jogadores ficam de fora | **[EM ABERTO]** padrão 3 |
| Duração padrão do timer da rodada (min) | **[EM ABERTO]** padrão 60 |

- A configuração **trava** quando a primeira rodada da temporada é encerrada (pode ser editada antes disso).
- **Os pontos não são gravados**: o ranking é sempre calculado a partir dos resultados (vitória/empate/derrota/bye) + configuração da temporada. Corrigir um resultado recalcula tudo.
- Resultado de uma mesa: **um vencedor** OU **empate** (todos os 4 recebem pontos de empate).

### 4.2 Bye
- Com N presentes: mesas = ⌊N/4⌋, byes = N mod 4.
- O sorteio deve **priorizar para bye quem ainda não recebeu bye na temporada** (sorteio aleatório entre os de menor número de byes). Isso evita que o mesmo jogador ganhe pontos sem jogar duas vezes.
- Bye conta **pontos** no ranking de jogadores e aparece na coluna "Byes", mas **NÃO conta como vitória de comandante** nem como partida jogada nas estatísticas de comandantes.
- Se houver menos de 4 presentes, não é possível iniciar a rodada.
- **[EM ABERTO]** Alternativa a avaliar com o lojista: permitir mesas de 3 em vez de 2–3 byes. Fora do MVP; não implementar agora.

### 4.3 Sorteio de mesas
- Embaralhamento aleatório (Fisher–Yates com RNG criptográfico) dos presentes não-bye, distribuídos em mesas de 4 com cadeiras 1–4 (cadeira 1 = começa jogando).
- O lojista pode **sortear novamente** enquanto a rodada não foi iniciada (timer não começou).
- Exibição visual: desenho de uma mesa com 4 cadeiras e o nome do jogador + comandante em cada uma.

### 4.4 Timer
- Ao iniciar, grava `StartedAt` (UTC) e `DurationMinutes` na rodada. O tempo restante é calculado a partir disso (recarregar a página não reinicia o timer; qualquer dispositivo mostra o mesmo tempo).
- Lojista pode pausar/estender **[opcional, fase 2]**.

### 4.5 Decks e aprovação
- Status: `Pendente` → `Aprovado` | `Reprovado` (com observação do lojista).
- O status **não bloqueia** o uso do deck na rodada; serve para gestão. A tela da rodada mostra um aviso visual quando o deck usado não está aprovado.
- Deck tem 1 ou 2 comandantes. Comandantes são entidades próprias (nome único, normalizado) para que as estatísticas não se dividam por grafias diferentes ("Atraxa" vs "Atraxa, Praetors' Voice"). Fase 2: autocomplete de nomes pela API pública do Scryfall (apenas nome/imagem; sem integração de deckbuilding).
- No MVP, o **lojista** cadastra os decks. Fase 2: formulário público em que o jogador envia o deck, que entra como `Pendente`.

### 4.6 Fluxo da rodada (estados)
`Rascunho` (selecionando presentes e decks) → `Sorteada` (mesas definidas, pode re-sortear) → `EmAndamento` (timer rodando) → `Encerrada` (resultados de todas as mesas informados).

- Cada presente na rodada joga com **um deck** escolhido entre os decks dele.
- Para encerrar, toda mesa precisa de resultado (vencedor ou empate).
- Resultados podem ser corrigidos depois do encerramento (registrar `UpdatedAt`/`UpdatedBy`).

## 5. Modelo de dados (proposta)

```
Season(Id, Name, StartDate, EndDate?, IsActive, ConfigLocked,
       PointsWin, PointsDraw, PointsBye1, PointsBye2, PointsBye3, DefaultRoundMinutes)
Player(Id, Name, Nickname?, IsActive, CreatedAt)
Commander(Id, Name [unique, normalized])
Deck(Id, PlayerId, Name, ListUrl?, Status[Pendente|Aprovado|Reprovado], ReviewNote?, CreatedAt)
DeckCommander(DeckId, CommanderId)            -- 1 ou 2 por deck
Round(Id, SeasonId, Number, Date, Status, StartedAt?, DurationMinutes, ClosedAt?)
RoundEntry(Id, RoundId, PlayerId, DeckId, IsBye)   -- presença do jogador na rodada
Table(Id, RoundId, Number, Result[Pendente|Vitoria|Empate], WinnerEntryId?)
TableSeat(TableId, RoundEntryId, Seat[1..4])
AdminUser(Id, Email, PasswordHash)
```

## 6. Telas

**Públicas (sem login, mobile-first):**
1. **Temporadas** – lista de temporadas (vigente em destaque + passadas).
2. **Detalhe da temporada** – abas:
   - *Ranking de jogadores*: posição, jogador, pontos, vitórias, empates, byes, partidas, % vitória.
   - *Comandantes*: mais jogados (nº de partidas), mais vitórias, % vitória (exibir % só com mínimo de partidas configurável; padrão 3).
   - *Rodadas*: histórico das rodadas e mesas.
3. **Rodada ao vivo** (sugestão de alto valor e baixo custo): mesas sorteadas com cadeiras + timer, para os jogadores acharem seu lugar pelo celular.

**Admin (com login):**
4. **Condução da rodada** – criar rodada → marcar presentes e escolher o deck de cada um → sortear (visual da mesa) → iniciar timer → informar vencedor/empate por mesa → encerrar.
5. **Cadastros e configuração** – jogadores, decks (comandantes, link, aprovação), temporadas (criar/encerrar, pontuação, bye, timer padrão).

### Desempate do ranking
pontos ↓ → vitórias ↓ → partidas jogadas ↑ (menos partidas = melhor aproveitamento) → nome. **[EM ABERTO]** confirmar com o lojista.

## 7. API (rascunho)

Público:
- `GET /api/seasons` · `GET /api/seasons/{id}`
- `GET /api/seasons/{id}/ranking`
- `GET /api/seasons/{id}/commanders`
- `GET /api/seasons/{id}/rounds` · `GET /api/rounds/{id}` (inclui mesas, cadeiras e timer)
- `GET /api/rounds/current`
- `GET /health`

Admin (JWT):
- `POST /api/auth/login`
- CRUD `players`, `decks` (+ `PATCH /decks/{id}/review`), `commanders`, `seasons` (+ config)
- `POST /api/seasons/{id}/rounds` · `PUT /api/rounds/{id}/entries`
- `POST /api/rounds/{id}/draw` (sorteio/re-sorteio) · `POST /api/rounds/{id}/start`
- `PUT /api/tables/{id}/result` · `POST /api/rounds/{id}/close`

## 8. Fora do MVP
Login de jogadores, envio de deck pelo jogador, autocomplete Scryfall, mesas de 3, pausa/extensão de timer, prevenção de confrontos repetidos no sorteio, validação de preço do deck.

## 9. Pendências com o lojista
1. Pontos de bye quando 2 ou 3 ficam de fora (ou mesas de 3?).
2. Critério de desempate do ranking.
3. Duração padrão do timer e regra de fim de tempo (ex.: "acabou o tempo, mais N turnos" – evita jogador enrolar para forçar empate).
4. Mais de um jogo por jogador no mesmo dia? (Se sim, cada jogo é uma rodada.)
