using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Condução da rodada: criar, presentes, sortear, iniciar, resultados, encerrar e excluir.</summary>
public class ServicoRodada(IContextoBanco banco, TimeProvider relogio, IGeradorAleatorio gerador)
{
    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    private async Task<Rodada> Carregar(int id) =>
        await banco.Rodadas.ComTudo().FirstOrDefaultAsync(r => r.Id == id)
        ?? throw new NaoEncontradoException($"Rodada {id} não encontrada.");

    public async Task<RodadaResposta> Obter(int id) => (await Carregar(id)).ParaResposta(Agora);

    /// <summary>Rodadas sorteadas / em andamento de todas as temporadas ativas (pode haver mais de uma).</summary>
    public async Task<IReadOnlyList<RodadaResposta>> ListarAtuais()
    {
        var rodadas = await banco.Rodadas.AsNoTrackingWithIdentityResolution().ComTudo()
            .Where(r => r.Temporada!.Ativa && (r.Situacao == SituacaoRodada.Sorteada || r.Situacao == SituacaoRodada.EmAndamento))
            .OrderBy(r => r.Temporada!.Nome).ThenBy(r => r.Numero)
            .ToListAsync();
        return rodadas.Select(r => r.ParaResposta(Agora)).ToList();
    }

    public async Task<RodadaResposta> Criar(int idTemporada, NovaRodadaEnvio envio)
    {
        var temporada = await banco.Temporadas.FindAsync(idTemporada) ?? throw new NaoEncontradoException($"Temporada {idTemporada} não encontrada.");
        if (!temporada.Ativa) throw new RegraNegocioException("A temporada não está ativa.");
        var duracao = envio.DuracaoMinutos ?? temporada.MinutosPadraoRodada;
        if (duracao is < 1 or > FluxoRodada.DuracaoMaximaMinutos)
            throw new DadosInvalidosException($"Duração deve estar entre 1 e {FluxoRodada.DuracaoMaximaMinutos} minutos.");

        var data = envio.Data ?? DateOnly.FromDateTime(Agora);
        var etapas = await banco.Etapas.Where(e => e.IdTemporada == idTemporada).ToListAsync();
        var etapa = RegrasEtapa.Resolver(temporada, etapas, data);
        if (etapa.Id == 0) banco.Etapas.Add(etapa);

        var ultimoNumero = await banco.Rodadas.Where(r => r.IdTemporada == idTemporada).MaxAsync(r => (int?)r.Numero) ?? 0;
        var rodada = new Rodada
        {
            IdTemporada = idTemporada,
            Etapa = etapa,
            Numero = ultimoNumero + 1,
            Data = data,
            DuracaoMinutos = duracao,
            Situacao = SituacaoRodada.Rascunho,
        };

        // Outra rodada da mesma etapa: já começa com os presentes e decks da rodada anterior.
        // O lojista só desmarca quem foi embora; os resultados dele na etapa continuam valendo.
        if (etapa.Id != 0)
        {
            var anterior = await banco.Rodadas.Include(r => r.Participacoes)
                .Where(r => r.IdEtapa == etapa.Id).OrderByDescending(r => r.Numero).FirstOrDefaultAsync();
            if (anterior is not null)
                FluxoRodada.DefinirParticipantes(rodada, anterior.Participacoes.Select(p => new ParticipanteInformado(p.IdJogador, p.IdDeck)).ToList());
        }
        banco.Rodadas.Add(rodada);
        await banco.SalvarAsync();
        return await Recarregar(rodada.Id);
    }

    public async Task<RodadaResposta> DefinirParticipantes(int id, IReadOnlyList<ParticipanteInformado> participantes)
    {
        var rodada = await Carregar(id);

        var idsDecks = participantes.Select(p => p.IdDeck).Distinct().ToList();
        var decks = await banco.Decks.Where(d => idsDecks.Contains(d.Id)).ToDictionaryAsync(d => d.Id);
        var idsJogadores = participantes.Select(p => p.IdJogador).Distinct().ToList();
        var jogadores = await banco.Jogadores.Where(j => idsJogadores.Contains(j.Id)).ToDictionaryAsync(j => j.Id);
        foreach (var participante in participantes)
        {
            if (!jogadores.ContainsKey(participante.IdJogador)) throw new DadosInvalidosException($"Jogador {participante.IdJogador} não existe.");
            if (!decks.TryGetValue(participante.IdDeck, out var deck)) throw new DadosInvalidosException($"Deck {participante.IdDeck} não existe.");
            if (deck.IdJogador != participante.IdJogador)
                throw new DadosInvalidosException($"O deck '{deck.Nome}' não pertence a {jogadores[participante.IdJogador].Nome}.");
        }

        FluxoRodada.DefinirParticipantes(rodada, participantes);
        await banco.SalvarAsync();
        return await Recarregar(id);
    }

    public async Task<RodadaResposta> Sortear(int id)
    {
        var rodada = await Carregar(id);
        var rodadasDaTemporada = await banco.Rodadas.Where(r => r.IdTemporada == rodada.IdTemporada).Include(r => r.Participacoes).ToListAsync();
        var byesAnteriores = FluxoRodada.ContarByesAnteriores(rodadasDaTemporada, id);
        // Rodadas anteriores da mesma etapa: o sorteio evita repetir adversários e cadeiras.
        var rodadasDaEtapa = await banco.Rodadas.AsNoTracking()
            .Where(r => r.IdEtapa == rodada.IdEtapa && r.Id != id && r.Situacao != SituacaoRodada.Rascunho)
            .Include(r => r.Participacoes).Include(r => r.Mesas).ThenInclude(m => m.Cadeiras)
            .AsSplitQuery().ToListAsync();
        FluxoRodada.Sortear(rodada, byesAnteriores, gerador, rodadasDaEtapa);
        await banco.SalvarAsync();
        return await Recarregar(id);
    }

    public async Task<RodadaResposta> Iniciar(int id, int? duracaoMinutos)
    {
        var rodada = await Carregar(id);
        FluxoRodada.Iniciar(rodada, Agora, duracaoMinutos);
        await banco.SalvarAsync();
        return await Recarregar(id);
    }

    public async Task<RodadaResposta> RegistrarResultadoMesa(int idMesa, ResultadoMesaEnvio envio, string? usuario)
    {
        var idRodada = await banco.Mesas.Where(m => m.Id == idMesa).Select(m => (int?)m.IdRodada).FirstOrDefaultAsync()
            ?? throw new NaoEncontradoException($"Mesa {idMesa} não encontrada.");
        var rodada = await Carregar(idRodada);
        var mesa = rodada.Mesas.Single(m => m.Id == idMesa);
        FluxoRodada.RegistrarResultado(rodada, mesa, envio.Resultado, envio.IdParticipacaoVencedora, Agora, usuario);
        await banco.SalvarAsync();
        return await Recarregar(idRodada);
    }

    public async Task<RodadaResposta> Encerrar(int id)
    {
        var rodada = await Carregar(id);
        FluxoRodada.Encerrar(rodada, Agora);
        await banco.SalvarAsync();
        return await Recarregar(id);
    }

    public async Task Excluir(int id)
    {
        var rodada = await Carregar(id);
        FluxoRodada.GarantirQuePodeExcluir(rodada);
        banco.Rodadas.Remove(rodada);

        // Era a última rodada da etapa → apaga a etapa vazia e renumera as outras.
        if (rodada.Etapa is { } etapa && etapa.Rodadas.All(r => r.Id == rodada.Id))
        {
            banco.Etapas.Remove(etapa);
            var outras = await banco.Etapas.Where(e => e.IdTemporada == rodada.IdTemporada && e.Id != etapa.Id).ToListAsync();
            RegrasEtapa.Renumerar(outras);
        }
        await banco.SalvarAsync();
    }

    /// <summary>Esquece o que estava carregado para que as navegações das linhas novas venham do banco.</summary>
    private async Task<RodadaResposta> Recarregar(int id)
    {
        banco.LimparRastreamento();
        return await Obter(id);
    }
}
