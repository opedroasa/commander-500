using Commander500.Aplicacao.Interfaces;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commander500.Infraestrutura.Dados;

/// <summary>
/// Dados criados na inicialização da Api.
/// - Administrador: vem da configuração (variáveis Admin__Email / Admin__Senha). É criado se não existir; se existir e a
///   senha da configuração mudou, a senha é atualizada (a configuração manda).
/// - Regulamento: o texto do tratado, se ainda não houver (depois o lojista edita pela tela).
/// - Demonstração (só com Semente:DadosDemonstracao=true e banco sem temporadas): 1 temporada, 10 jogadores com decks
///   e 3 rodadas encerradas em 2 etapas.
/// </summary>
public static class SemeadorBanco
{
    public static async Task SemearAsync(IServiceProvider servicos, ILogger log)
    {
        var banco = servicos.GetRequiredService<ContextoBanco>();
        var configuracao = servicos.GetRequiredService<IConfiguration>();
        var servicoSenha = servicos.GetRequiredService<IServicoSenha>();
        var agora = servicos.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        var email = configuracao["Admin:Email"]?.Trim().ToLowerInvariant();
        var senha = configuracao["Admin:Senha"];
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(senha))
        {
            var administrador = await banco.Administradores.FirstOrDefaultAsync(a => a.Email == email);
            if (administrador is null)
            {
                banco.Administradores.Add(new Administrador { Email = email, HashSenha = servicoSenha.GerarHash(senha) });
                await banco.SaveChangesAsync();
                log.LogInformation("Admin {Email} criado.", email);
            }
            else if (servicoSenha.Verificar(administrador.HashSenha, senha) == VerificacaoSenha.Invalida)
            {
                administrador.HashSenha = servicoSenha.GerarHash(senha);
                await banco.SaveChangesAsync();
                log.LogInformation("Senha do admin {Email} atualizada a partir da configuração.", email);
            }
        }
        else if (!await banco.Administradores.AnyAsync())
        {
            log.LogWarning("Nenhum admin cadastrado. Defina Admin__Email e Admin__Senha.");
        }

        if (!await banco.Regulamentos.AnyAsync())
        {
            banco.Regulamentos.Add(new Regulamento { Conteudo = RegulamentoInicial.Html.Trim(), AlteradoEm = agora, AlteradoPor = "inicial" });
            await banco.SaveChangesAsync();
            log.LogInformation("Regulamento inicial (Tratado dos Quinhentos) criado.");
        }

        if (configuracao.GetValue<bool>("Semente:DadosDemonstracao") && !await banco.Temporadas.AnyAsync())
        {
            await SemearDemonstracaoAsync(banco, agora);
            log.LogInformation("Dados de demonstração criados.");
        }
    }

    private static async Task SemearDemonstracaoAsync(ContextoBanco banco, DateTime agora)
    {
        var temporada = new Temporada
        {
            Nome = "Temporada 1",
            DataInicio = DateOnly.FromDateTime(agora.AddDays(-21)),
            Ativa = true,
            // Aviso da página inicial: próxima etapa daqui a uma semana, 14h em Brasília (17h UTC); listas até a véspera.
            ProximaEtapaEm = DateOnly.FromDateTime(agora.AddDays(7)).ToDateTime(new TimeOnly(17, 0), DateTimeKind.Utc),
            PrazoListas = DateOnly.FromDateTime(agora.AddDays(6)),
        };
        banco.Temporadas.Add(temporada);

        var comandantes = new Dictionary<string, Comandante>();
        Comandante ObterComandante(string nome)
        {
            var nomeNormalizado = NomeComandante.Normalizar(nome);
            if (!comandantes.TryGetValue(nomeNormalizado, out var comandante))
                comandantes[nomeNormalizado] = comandante = new Comandante { Nome = nome, NomeNormalizado = nomeNormalizado };
            return comandante;
        }

        (string Jogador, string? Apelido, (string Deck, string[] Comandantes, SituacaoDeck Situacao)[] Decks)[] dados =
        [
            ("Ana Souza", "Aninha", [("Elfos", ["Lathril, Blade of the Elves"], SituacaoDeck.Aprovado)]),
            ("Bruno Lima", null, [("Dragões", ["The Ur-Dragon"], SituacaoDeck.Aprovado), ("Zumbis", ["Wilhelt, the Rotcleaver"], SituacaoDeck.Pendente)]),
            ("Carla Mendes", null, [("Tokens", ["Rhys the Redeemed"], SituacaoDeck.Aprovado)]),
            ("Diego Rocha", "Didi", [("Parceiros", ["Tymna the Weaver", "Thrasios, Triton Hero"], SituacaoDeck.Aprovado)]),
            ("Eduardo Alves", null, [("Artefatos", ["Urza, Lord High Artificer"], SituacaoDeck.Reprovado)]),
            ("Fernanda Costa", null, [("Counters", ["Atraxa, Praetors' Voice"], SituacaoDeck.Aprovado)]),
            ("Gabriel Pereira", null, [("Voltron", ["Light-Paws, Emperor's Voice"], SituacaoDeck.Pendente)]),
            ("Helena Martins", "Lena", [("Aristocratas", ["Teysa Karlov"], SituacaoDeck.Aprovado)]),
            ("Igor Santos", null, [("Background", ["Wilson, Refined Grizzly", "Raised by Giants"], SituacaoDeck.Aprovado)]),
            ("Julia Ferreira", null, [("Spellslinger", ["Kalamax, the Stormsire"], SituacaoDeck.Aprovado), ("Landfall", ["Omnath, Locus of Creation"], SituacaoDeck.Pendente)]),
        ];

        foreach (var (nomeJogador, apelido, decks) in dados)
        {
            var jogador = new Jogador { Nome = nomeJogador, Apelido = apelido, DataCadastro = agora };
            foreach (var (nomeDeck, nomesComandantes, situacao) in decks)
            {
                var deck = new Deck
                {
                    Nome = nomeDeck,
                    Situacao = situacao,
                    ObservacaoRevisao = situacao == SituacaoDeck.Reprovado ? "Acima do teto de preço." : null,
                    DataCadastro = agora,
                };
                foreach (var nome in nomesComandantes) deck.Comandantes.Add(new DeckComandante { Deck = deck, Comandante = ObterComandante(nome) });
                jogador.Decks.Add(deck);
            }
            banco.Jogadores.Add(jogador);
        }

        await banco.SaveChangesAsync();
        await SemearRodadasDemonstracaoAsync(banco, temporada);
    }

    /// <summary>
    /// Três rodadas encerradas, jogadas pelo fluxo real do domínio (sorteio com prioridade de bye, resultados, encerramento),
    /// para o ranking, as estatísticas e o histórico terem dados. Gerador com semente: sempre o mesmo resultado.
    /// </summary>
    private static async Task SemearRodadasDemonstracaoAsync(ContextoBanco banco, Temporada temporada)
    {
        var gerador = new GeradorAleatorioComSemente(500);
        var jogadores = await banco.Jogadores.Include(j => j.Decks).OrderBy(j => j.Id).ToListAsync();
        var rodadas = new List<Rodada>();
        var etapas = new List<Etapa>();

        // Etapa 1 (primeiro dia): rodadas 1 e 2. Etapa 2 (uma semana depois): rodada 3.
        for (var numero = 1; numero <= 3; numero++)
        {
            var data = temporada.DataInicio.AddDays(numero == 3 ? 7 : 0);
            var iniciadaEm = data.ToDateTime(new TimeOnly(numero == 2 ? 23 : 22, 15), DateTimeKind.Utc); // ~19h/20h em Brasília
            var etapa = RegrasEtapa.Resolver(temporada, etapas, data);

            // Na rodada 2 falta um jogador: fica 1 bye em vez de 2.
            var presentes = numero == 2 ? jogadores.Where(j => j != jogadores[^1]).ToList() : jogadores;
            var rodada = new Rodada { Temporada = temporada, IdTemporada = temporada.Id, Etapa = etapa, Numero = numero, Data = data, DuracaoMinutos = temporada.MinutosPadraoRodada };
            FluxoRodada.DefinirParticipantes(rodada, presentes.Select(j => new ParticipanteInformado(j.Id, j.Decks.OrderBy(d => d.Id).First().Id)).ToList());
            FluxoRodada.Sortear(rodada, FluxoRodada.ContarByesAnteriores(rodadas, idRodadaIgnorada: -1), gerador);
            FluxoRodada.Iniciar(rodada, iniciadaEm);
            banco.Rodadas.Add(rodada);
            await banco.SaveChangesAsync(); // gera os Ids das participações usados nos resultados

            foreach (var mesa in rodada.Mesas)
            {
                var sorteio = gerador.Proximo(5); // 0..3 = cadeira do vencedor, 4 = empate
                var vencedor = sorteio < 4 ? mesa.Cadeiras.OrderBy(c => c.Numero).ElementAt(sorteio).Participacao!.Id : (int?)null;
                FluxoRodada.RegistrarResultado(rodada, mesa, vencedor is null ? ResultadoMesa.Empate : ResultadoMesa.Vitoria, vencedor, iniciadaEm.AddMinutes(55), "seed");
            }
            FluxoRodada.Encerrar(rodada, iniciadaEm.AddMinutes(60));
            await banco.SaveChangesAsync();
            rodadas.Add(rodada);
        }
    }
}
