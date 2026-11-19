using Commander500.Aplicacao.Servicos;
using Commander500.Dominio.Regras;
using Microsoft.Extensions.DependencyInjection;

namespace Commander500.Aplicacao;

public static class InjecaoDependencia
{
    /// <summary>Registra os serviços de aplicação (casos de uso). Chamado no Program.cs da Api.</summary>
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection servicos)
    {
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddSingleton<IGeradorAleatorio, GeradorAleatorioCriptografico>();

        servicos.AddScoped<ServicoTemporada>();
        servicos.AddScoped<ServicoRodada>();
        servicos.AddScoped<ServicoJogador>();
        servicos.AddScoped<ServicoDeck>();
        servicos.AddScoped<ServicoComandante>();
        servicos.AddScoped<ServicoEstatisticas>();
        servicos.AddScoped<ServicoAutenticacao>();
        servicos.AddScoped<ServicoAdministrador>();
        servicos.AddScoped<ServicoRegulamento>();
        return servicos;
    }
}
