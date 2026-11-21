using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Temporadas: cadastro, pontuação, encerramento, ranking e histórico de rodadas.</summary>
public class ServicoTemporada(IContextoBanco banco, TimeProvider relogio, IArmazenamentoFotos fotos)
{
    public async Task<IReadOnlyList<TemporadaResposta>> Listar()
    {
        var linhas = await banco.Temporadas.AsNoTracking()
            .OrderByDescending(t => t.Ativa).ThenByDescending(t => t.DataInicio).ThenByDescending(t => t.Id)
            .Select(t => new { Temporada = t, Rodadas = t.Rodadas.Count, Etapas = t.Etapas.Count(e => !e.Final),
                IdFinal = t.Etapas.Where(e => e.Final).Select(e => (int?)e.Id).FirstOrDefault() })
            .ToListAsync();
        return linhas.Select(l => l.Temporada.ParaResposta(l.Rodadas, l.Etapas, l.IdFinal, fotos.Url(l.Temporada.CaminhoFoto))).ToList();
    }

    public async Task<TemporadaResposta> Obter(int id)
    {
        var linha = await banco.Temporadas.AsNoTracking().Where(t => t.Id == id)
            .Select(t => new { Temporada = t, Rodadas = t.Rodadas.Count, Etapas = t.Etapas.Count(e => !e.Final),
                IdFinal = t.Etapas.Where(e => e.Final).Select(e => (int?)e.Id).FirstOrDefault() }).FirstOrDefaultAsync()
            ?? throw new NaoEncontradoException($"Temporada {id} não encontrada.");
        return linha.Temporada.ParaResposta(linha.Rodadas, linha.Etapas, linha.IdFinal, fotos.Url(linha.Temporada.CaminhoFoto));
    }

    public async Task<TemporadaResposta> Criar(TemporadaEnvio envio)
    {
        var temporada = new Temporada();
        PreencherDados(temporada, envio);
        if (envio.Configuracao is not null) RegrasTemporada.AplicarConfiguracao(temporada, envio.Configuracao);
        banco.Temporadas.Add(temporada);
        await banco.SalvarAsync();
        return await Obter(temporada.Id);
    }

    /// <summary>Nome, datas e se está ativa.</summary>
    public async Task<TemporadaResposta> Alterar(int id, TemporadaEnvio envio)
    {
        var temporada = await Buscar(id);
        PreencherDados(temporada, envio);
        if (envio.Configuracao is not null) RegrasTemporada.AplicarConfiguracao(temporada, envio.Configuracao);
        await banco.SalvarAsync();
        return await Obter(id);
    }

    public async Task<TemporadaResposta> AlterarConfiguracao(int id, ConfiguracaoTemporada configuracao)
    {
        var temporada = await Buscar(id);
        RegrasTemporada.AplicarConfiguracao(temporada, configuracao);
        await banco.SalvarAsync();
        return await Obter(id);
    }

    /// <summary>Aviso da próxima etapa (data/hora e prazo das listas) mostrado na página inicial.</summary>
    public async Task<TemporadaResposta> DefinirProximaEtapa(int id, ProximaEtapaEnvio envio)
    {
        var temporada = await Buscar(id);
        RegrasTemporada.DefinirProximaEtapa(temporada, envio.ProximaEtapaEm?.UtcDateTime, envio.PrazoListas);
        await banco.SalvarAsync();
        return await Obter(id);
    }

    /// <summary>Encerra a temporada: fica inativa e a data de fim vira hoje (se vazia). Rodadas abertas precisam ser encerradas ou excluídas antes.</summary>
    public async Task<TemporadaResposta> Encerrar(int id)
    {
        var temporada = await Buscar(id);
        if (await banco.Rodadas.AnyAsync(r => r.IdTemporada == id && r.Situacao != SituacaoRodada.Encerrada))
            throw new RegraNegocioException("Há rodadas em aberto nesta temporada. Encerre ou exclua-as antes.");
        temporada.Ativa = false;
        temporada.DataFim ??= DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        await banco.SalvarAsync();
        return await Obter(id);
    }

    public async Task Excluir(int id)
    {
        var temporada = await Buscar(id);
        if (await banco.Rodadas.AnyAsync(r => r.IdTemporada == id))
            throw new RegraNegocioException("A temporada já tem rodadas e não pode ser excluída. Encerre-a em vez disso.");
        banco.Temporadas.Remove(temporada);
        await banco.SalvarAsync();
    }

    /// <summary>Ranking no modo da temporada (soma das rodadas ou colocação nas etapas + final).</summary>
    public async Task<IReadOnlyList<LinhaRanking>> Ranking(int id)
    {
        var temporada = await BuscarSomenteLeitura(id);
        var etapas = await banco.Etapas.AsNoTracking().Where(e => e.IdTemporada == id).ToListAsync();
        // Todas as rodadas: as abertas dizem se a etapa já terminou (só etapa concluída dá pontos por colocação).
        var rodadas = await banco.Rodadas.AsNoTracking()
            .Where(r => r.IdTemporada == id)
            .Include(r => r.Participacoes).Include(r => r.Mesas).ThenInclude(m => m.Cadeiras)
            .AsSplitQuery().ToListAsync();
        var idsJogadores = rodadas.SelectMany(r => r.Participacoes).Select(p => p.IdJogador).Distinct().ToList();
        var nomes = await banco.Jogadores.Where(j => idsJogadores.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.Apelido == null ? j.Nome : $"{j.Nome} ({j.Apelido})");
        return CalculadoraRankingTemporada.Calcular(temporada, etapas, rodadas, nomes);
    }

    public async Task<IReadOnlyList<RodadaResposta>> Rodadas(int id)
    {
        await BuscarSomenteLeitura(id);
        var agora = relogio.GetUtcNow().UtcDateTime;
        var rodadas = await banco.Rodadas.AsNoTrackingWithIdentityResolution().ComTudo()
            .Where(r => r.IdTemporada == id).OrderByDescending(r => r.Numero).ToListAsync();
        return rodadas.Select(r => r.ParaResposta(agora)).ToList();
    }

    private static void PreencherDados(Temporada temporada, TemporadaEnvio envio)
    {
        if (string.IsNullOrWhiteSpace(envio.Nome)) throw new DadosInvalidosException("Nome da temporada é obrigatório.");
        if (envio.DataFim is { } fim && fim < envio.DataInicio) throw new DadosInvalidosException("Data de fim anterior ao início.");
        temporada.Nome = envio.Nome.Trim();
        temporada.DataInicio = envio.DataInicio;
        temporada.DataFim = envio.DataFim;
        temporada.Ativa = envio.Ativa;
    }

    private async Task<Temporada> Buscar(int id) =>
        await banco.Temporadas.FindAsync(id) ?? throw new NaoEncontradoException($"Temporada {id} não encontrada.");

    private async Task<Temporada> BuscarSomenteLeitura(int id) =>
        await banco.Temporadas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id)
        ?? throw new NaoEncontradoException($"Temporada {id} não encontrada.");
}
