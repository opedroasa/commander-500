using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Commander500.Infraestrutura.Scryfall;

/// <summary>
/// Rotina em segundo plano: completa os dados do Scryfall (arte, imagem, cores) dos comandantes salvos só pelo nome
/// (dados de demonstração, ou digitados com o Scryfall fora do ar). Roda logo depois de iniciar e depois a cada 6 horas.
/// Nomes que o Scryfall não conhece são tentados de novo no máximo uma vez por dia. Desligar com Scryfall__Habilitado=false.
/// </summary>
public class ServicoEnriquecimentoScryfall(IServiceScopeFactory fabricaEscopo, TimeProvider relogio, ILogger<ServicoEnriquecimentoScryfall> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), parada);
            while (!parada.IsCancellationRequested)
            {
                await ExecutarUmaVez(parada);
                await Task.Delay(TimeSpan.FromHours(6), parada);
            }
        }
        catch (OperationCanceledException) { /* a Api está desligando */ }
    }

    public async Task ExecutarUmaVez(CancellationToken cancelamento)
    {
        using var escopo = fabricaEscopo.CreateScope();
        var banco = escopo.ServiceProvider.GetRequiredService<ContextoBanco>();
        var scryfall = escopo.ServiceProvider.GetRequiredService<IClienteScryfall>();
        var agora = relogio.GetUtcNow().UtcDateTime;
        var tentarDeNovoAntesDe = agora.AddDays(-1);

        List<Comandante> pendentes;
        try
        {
            pendentes = await banco.Comandantes
                .Where(c => c.IdScryfall == null && (c.DataConsultaScryfall == null || c.DataConsultaScryfall < tentarDeNovoAntesDe))
                .OrderBy(c => c.Id).Take(100).ToListAsync(cancelamento);
        }
        catch (Exception erro)
        {
            log.LogWarning(erro, "Enriquecimento Scryfall: banco indisponível.");
            return;
        }

        var completados = 0;
        foreach (var comandante in pendentes)
        {
            try
            {
                var carta = await scryfall.ObterPorNomeExato(comandante.Nome, cancelamento);
                comandante.DataConsultaScryfall = agora;
                var nomeNormalizado = carta is null ? "" : NomeComandante.Normalizar(carta.Nome);
                // Não aplica se outro comandante já tem essa carta ou esse nome (evita duplicar no índice único).
                if (carta is not null && !await banco.Comandantes.AnyAsync(c => c.Id != comandante.Id
                        && (c.IdScryfall == carta.Id || c.NomeNormalizado == nomeNormalizado), cancelamento))
                {
                    DadosScryfall.Aplicar(comandante, carta, agora);
                    completados++;
                }
                await banco.SaveChangesAsync(cancelamento);
            }
            catch (HttpRequestException erro)
            {
                log.LogInformation("Scryfall indisponível ({Mensagem}); tento de novo mais tarde.", erro.Message);
                return;
            }
        }
        if (pendentes.Count > 0) log.LogInformation("Scryfall: {Completados}/{Total} comandantes atualizados.", completados, pendentes.Count);
    }
}
