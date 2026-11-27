using System.Globalization;
using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>
/// Lista pública de jogadores e as páginas completas do jogador e do comandante. Tudo é calculado na hora a partir das
/// rodadas encerradas (nada de pontos gravados), como o resto do site.
/// </summary>
public class ServicoPaginas(IContextoBanco banco)
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>Tudo o que as páginas precisam, carregado uma vez por requisição.</summary>
    private sealed class Dados
    {
        public required List<Temporada> Temporadas { get; init; }
        public required List<Etapa> Etapas { get; init; }
        public required List<Rodada> Rodadas { get; init; }
        public required List<Jogador> Jogadores { get; init; }
        public required Dictionary<int, Deck> Decks { get; init; }
        public required Dictionary<int, string> Nomes { get; init; }

        public string Nome(int idJogador) => Nomes.GetValueOrDefault(idJogador) ?? $"#{idJogador}";

        /// <summary>Ranking de cada temporada (no modo dela).</summary>
        public Dictionary<int, IReadOnlyList<LinhaRanking>> Rankings => _rankings ??= Temporadas.ToDictionary(t => t.Id,
            t => CalculadoraRankingTemporada.Calcular(t, Etapas.Where(e => e.IdTemporada == t.Id), Rodadas.Where(r => r.IdTemporada == t.Id), Nomes));
        private Dictionary<int, IReadOnlyList<LinhaRanking>>? _rankings;

        /// <summary>Etapas concluídas (sem a final) com os jogadores em 1º e o deck que cada um mais usou nelas.</summary>
        public List<(Etapa Etapa, List<int> Lideres, Dictionary<int, int> DeckDoJogador)> EtapasConcluidas => _etapas ??= Etapas
            .Where(e => !e.Final)
            .Select(e => (Etapa: e, Rodadas: Rodadas.Where(r => r.IdEtapa == e.Id).ToList()))
            .Where(x => RegrasEtapa.EstaConcluida(x.Rodadas))
            .Select(x => (x.Etapa,
                CalculadoraRankingEtapa.Calcular(x.Rodadas, Nomes, Temporadas.First(t => t.Id == x.Etapa.IdTemporada))
                    .Where(l => l.Posicao == 1).Select(l => l.IdJogador).ToList(),
                RegrasHallDaFama.DeckMaisUsado(x.Rodadas)))
            .ToList();
        private List<(Etapa, List<int>, Dictionary<int, int>)>? _etapas;

        public string ChaveDoDeck(int idDeck) =>
            Decks.TryGetValue(idDeck, out var deck) ? CalculadoraDesempenho.ChaveComandantes(deck.Comandantes.Select(dc => dc.IdComandante)) : "";
    }

    private async Task<Dados> Carregar()
    {
        var jogadores = await banco.Jogadores.AsNoTracking().ToListAsync();
        return new Dados
        {
            Temporadas = await banco.Temporadas.AsNoTracking().ToListAsync(),
            Etapas = await banco.Etapas.AsNoTracking().ToListAsync(),
            Rodadas = await banco.Rodadas.AsNoTracking().ComResultados().ToListAsync(),
            Jogadores = jogadores,
            Decks = await banco.Decks.AsNoTracking().Include(d => d.Comandantes).ThenInclude(dc => dc.Comandante).ToDictionaryAsync(d => d.Id),
            Nomes = jogadores.ToDictionary(j => j.Id, j => j.Apelido == null ? j.Nome : $"{j.Nome} ({j.Apelido})"),
        };
    }

    private static IReadOnlyList<ComandanteResposta> ComandantesDo(Deck? deck) =>
        deck is null ? [] : deck.Comandantes.Select(dc => dc.Comandante!).OrderBy(c => c.Nome).Select(c => c.ParaResposta()).ToList();

    private static IReadOnlyList<PontoMensalResposta> ParaResposta(IReadOnlyList<PontoMensal> pontos) => pontos
        .Select(p => new PontoMensalResposta(new DateOnly(p.Ano, p.Mes, 1).ToString("MMM/yy", PtBr).Replace(".", ""),
            p.Partidas, p.Vitorias, p.Empates, p.Derrotas, p.PercentualMes, p.PercentualAcumulado))
        .ToList();

    // ------------------------------------------------------------------ Jogadores

    /// <summary>Jogadores ativos ou que já jogaram, com o resumo de cada um (lista pública).</summary>
    public async Task<IReadOnlyList<JogadorListaResposta>> ListarJogadores()
    {
        var dados = await Carregar();
        var encerradas = dados.Rodadas.Where(r => r.Situacao == SituacaoRodada.Encerrada).ToList();
        var geral = CalculadoraRanking.Calcular(new Temporada(), encerradas, dados.Nomes).ToDictionary(l => l.IdJogador);
        var deckPrincipal = RegrasHallDaFama.DeckMaisUsado(encerradas);
        var temporadasDoJogador = encerradas.SelectMany(r => r.Participacoes.Select(p => (p.IdJogador, r.IdTemporada)))
            .Distinct().GroupBy(x => x.IdJogador).ToDictionary(g => g.Key, g => g.Count());
        var titulos = dados.Temporadas.Where(t => !t.Ativa)
            .SelectMany(t => dados.Rankings[t.Id].Where(l => l.Posicao == 1).Select(l => l.IdJogador))
            .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var etapasVencidas = dados.EtapasConcluidas.SelectMany(e => e.Lideres)
            .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var decksPorJogador = dados.Decks.Values.ToLookup(d => d.IdJogador);

        // Arte do card: o deck que ele mais jogou; quem ainda não jogou usa o deck reserva (RegrasDeckDoJogador).
        Deck? DeckDaArte(int idJogador) =>
            deckPrincipal.TryGetValue(idJogador, out var idDeck) && dados.Decks.TryGetValue(idDeck, out var maisJogado)
                ? maisJogado
                : RegrasDeckDoJogador.DeckReserva(decksPorJogador[idJogador]);

        return dados.Jogadores
            .Where(j => j.Ativo || geral.ContainsKey(j.Id))
            .Select(j =>
            {
                var linha = geral.GetValueOrDefault(j.Id);
                return new JogadorListaResposta(j.Id, j.Nome, j.Apelido, j.Ativo, temporadasDoJogador.GetValueOrDefault(j.Id),
                    linha?.Partidas ?? 0, linha?.Vitorias ?? 0, linha?.Empates ?? 0, linha?.PercentualVitoria,
                    titulos.GetValueOrDefault(j.Id), etapasVencidas.GetValueOrDefault(j.Id),
                    ComandantesDo(DeckDaArte(j.Id)));
            })
            .OrderByDescending(j => j.Titulos).ThenByDescending(j => j.Vitorias).ThenBy(j => j.Nome, StringComparer.Create(PtBr, true))
            .ToList();
    }

    /// <summary>Página completa do jogador: temporadas, títulos, decks, evolução mês a mês e confrontos.</summary>
    public async Task<PaginaJogadorResposta> PaginaJogador(int id)
    {
        var dados = await Carregar();
        var jogador = dados.Jogadores.FirstOrDefault(j => j.Id == id) ?? throw new NaoEncontradoException($"Jogador {id} não encontrado.");
        var encerradas = dados.Rodadas.Where(r => r.Situacao == SituacaoRodada.Encerrada).ToList();
        var linhaGeral = CalculadoraRanking.Calcular(new Temporada(), encerradas, dados.Nomes).FirstOrDefault(l => l.IdJogador == id);

        var temporadas = dados.Temporadas
            .Where(t => dados.Rodadas.Any(r => r.IdTemporada == t.Id && r.Participacoes.Any(p => p.IdJogador == id)))
            .OrderByDescending(t => t.DataInicio)
            .Select(t =>
            {
                var linha = dados.Rankings[t.Id].FirstOrDefault(l => l.IdJogador == id);
                return new TemporadaDoJogadorResposta(t.Id, t.Nome, t.Ativa, linha?.Posicao, linha?.Pontos ?? 0,
                    linha?.Partidas ?? 0, linha?.Vitorias ?? 0, !t.Ativa && linha?.Posicao == 1);
            }).ToList();

        var etapasVencidas = dados.EtapasConcluidas.Where(e => e.Lideres.Contains(id))
            .OrderByDescending(e => e.Etapa.Data)
            .Select(e => new ReferenciaEtapaResposta(e.Etapa.Id, e.Etapa.IdTemporada,
                dados.Temporadas.First(t => t.Id == e.Etapa.IdTemporada).Nome, e.Etapa.Numero, e.Etapa.Data))
            .ToList();

        var desempenho = CalculadoraDesempenho.PorDeck(encerradas, p => p.IdJogador == id).ToDictionary(d => d.IdDeck);
        var melhor = CalculadoraDesempenho.MelhorDeck(desempenho.Values.ToList());
        var decks = dados.Decks.Values.Where(d => d.IdJogador == id || desempenho.ContainsKey(d.Id))
            .Select(d =>
            {
                var x = desempenho.GetValueOrDefault(d.Id);
                return new DeckDoJogadorResposta(d.Id, d.Nome, d.UrlLista, d.Situacao, dados.ChaveDoDeck(d.Id), ComandantesDo(d),
                    x?.Partidas ?? 0, x?.Vitorias ?? 0, x?.Empates ?? 0, x?.PercentualVitoria);
            })
            .OrderByDescending(d => d.Partidas).ThenBy(d => d.Nome).ToList();

        var confrontos = CalculadoraDesempenho.Confrontos(encerradas, id);
        ConfrontoResposta? Resposta(LinhaConfronto? c) => c is null ? null
            : new ConfrontoResposta(c.IdAdversario, dados.Nome(c.IdAdversario), c.Mesas, c.VitoriasDoJogador, c.VitoriasDoAdversario);

        return new PaginaJogadorResposta(
            jogador.Id, jogador.Nome, jogador.Apelido, jogador.Ativo, jogador.DataCadastro,
            linhaGeral is null ? new EstatisticasJogadorResposta(0, 0, 0, 0, 0, 0, null)
                : new EstatisticasJogadorResposta(linhaGeral.Partidas + linhaGeral.Byes, linhaGeral.Partidas, linhaGeral.Vitorias,
                    linhaGeral.Empates, linhaGeral.Derrotas, linhaGeral.Byes, linhaGeral.PercentualVitoria),
            temporadas.Count(t => t.Campeao), temporadas, etapasVencidas, decks, melhor?.IdDeck,
            ParaResposta(CalculadoraDesempenho.EvolucaoMensal(encerradas, p => p.IdJogador == id)),
            Resposta(CalculadoraDesempenho.Nemesis(confrontos)), Resposta(CalculadoraDesempenho.VitimaFavorita(confrontos)),
            confrontos.Take(8).Select(c => Resposta(c)!).ToList());
    }

    // ------------------------------------------------------------------ Comandantes

    /// <summary>Página do comandante (ou dupla) pela chave "12-34": números, títulos, evolução e todas as listas cadastradas.</summary>
    public async Task<PaginaComandanteResposta> PaginaComandante(string chave)
    {
        var dados = await Carregar();
        var decks = dados.Decks.Values.Where(d => dados.ChaveDoDeck(d.Id) == chave).ToList();
        if (decks.Count == 0) throw new NaoEncontradoException("Comandante não encontrado.");
        var idsDecks = decks.Select(d => d.Id).ToHashSet();
        bool DoComandante(Participacao p) => idsDecks.Contains(p.IdDeck);

        var encerradas = dados.Rodadas.Where(r => r.Situacao == SituacaoRodada.Encerrada).ToList();
        var partidas = CalculadoraDesempenho.Partidas(encerradas).Where(p => DoComandante(p.Participacao)).ToList();
        int Contar(ResultadoParticipacao resultado) => partidas.Count(p => p.Resultado == resultado);
        var vitorias = Contar(ResultadoParticipacao.Vitoria);

        // Título = temporada encerrada em que o campeão jogou mais com esse comandante.
        var titulos = new List<TituloComandanteResposta>();
        foreach (var temporada in dados.Temporadas.Where(t => !t.Ativa).OrderByDescending(t => t.DataFim ?? t.DataInicio))
        {
            var deckDoJogador = RegrasHallDaFama.DeckMaisUsado(dados.Rodadas.Where(r => r.IdTemporada == temporada.Id));
            foreach (var campeao in dados.Rankings[temporada.Id].Where(l => l.Posicao == 1))
                if (deckDoJogador.TryGetValue(campeao.IdJogador, out var idDeck) && idsDecks.Contains(idDeck))
                    titulos.Add(new TituloComandanteResposta(temporada.Id, temporada.Nome, campeao.IdJogador, campeao.NomeJogador));
        }
        var etapasVencidas = dados.EtapasConcluidas.Count(e =>
            e.Lideres.Any(id => e.DeckDoJogador.TryGetValue(id, out var idDeck) && idsDecks.Contains(idDeck)));

        var desempenho = CalculadoraDesempenho.PorDeck(encerradas, DoComandante).ToDictionary(d => d.IdDeck);
        var listas = decks.Select(d =>
            {
                var x = desempenho.GetValueOrDefault(d.Id);
                return new ListaComandanteResposta(d.Id, d.Nome, d.UrlLista, d.IdJogador, dados.Nome(d.IdJogador),
                    x?.Partidas ?? 0, x?.Vitorias ?? 0, x?.Empates ?? 0, x?.PercentualVitoria);
            })
            .OrderByDescending(l => l.Vitorias).ThenByDescending(l => l.Partidas).ThenBy(l => l.NomeJogador).ToList();

        var temporadas = partidas.GroupBy(p => p.Rodada.IdTemporada)
            .Select(g =>
            {
                var t = dados.Temporadas.First(x => x.Id == g.Key);
                var v = g.Count(p => p.Resultado == ResultadoParticipacao.Vitoria);
                return (t.DataInicio, Linha: new TemporadaDoComandanteResposta(t.Id, t.Nome, g.Count(), v, (double)v / g.Count()));
            })
            .OrderByDescending(x => x.DataInicio).Select(x => x.Linha).ToList();

        var comandantes = decks[0].Comandantes.Select(dc => dc.Comandante!).OrderBy(c => c.Nome).ToList();
        var identidade = comandantes.Any(c => c.IdentidadeCor is null) ? null
            : string.Concat("WUBRG".Where(cor => comandantes.Any(c => c.IdentidadeCor!.Contains(cor))));

        return new PaginaComandanteResposta(
            chave, string.Join(" + ", comandantes.Select(c => c.Nome)), comandantes.Select(c => c.ParaResposta()).ToList(), identidade,
            partidas.Count, vitorias, Contar(ResultadoParticipacao.Empate), Contar(ResultadoParticipacao.Derrota),
            partidas.Count == 0 ? null : (double)vitorias / partidas.Count,
            partidas.Select(p => p.Participacao.IdJogador).Distinct().Count(),
            partidas.Where(p => p.Resultado == ResultadoParticipacao.Vitoria).Select(p => p.Participacao.IdJogador).Distinct().Count(),
            titulos, etapasVencidas,
            ParaResposta(CalculadoraDesempenho.EvolucaoMensal(encerradas, DoComandante)), listas, temporadas);
    }
}
