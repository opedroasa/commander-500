using Commander500.Aplicacao.Interfaces;
using Commander500.Infraestrutura.Dados;
using Commander500.Infraestrutura.Scryfall;
using Commander500.Infraestrutura.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commander500.Infraestrutura;

public static class InjecaoDependencia
{
    /// <summary>Registra banco, Scryfall, JWT e senha. Chamado no Program.cs da Api.</summary>
    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        // ----- Banco: ConnectionStrings__Padrao ou DATABASE_URL (padrão do Render/Neon)
        var stringConexao = new[] { configuracao.GetConnectionString("Padrao"), Environment.GetEnvironmentVariable("DATABASE_URL") }
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
            ?? throw new InvalidOperationException("Connection string ausente: defina ConnectionStrings__Padrao (ou DATABASE_URL).");
        servicos.AddDbContext<ContextoBanco>(o => o.UseNpgsql(StringConexao.Normalizar(stringConexao)));
        servicos.AddScoped<IContextoBanco>(provedor => provedor.GetRequiredService<ContextoBanco>());

        // ----- Login
        servicos.Configure<OpcoesJwt>(configuracao.GetSection(OpcoesJwt.Secao));
        servicos.AddSingleton<IGeradorToken, GeradorTokenJwt>();
        servicos.AddSingleton<IServicoSenha, ServicoSenha>();

        // ----- Scryfall (nomes, artes e cores dos comandantes). API pública e gratuita, sem chave.
        servicos.AddMemoryCache();
        servicos.AddHttpClient<IClienteScryfall, ClienteScryfall>(cliente =>
        {
            cliente.BaseAddress = new Uri(ClienteScryfall.EnderecoBase);
            cliente.Timeout = TimeSpan.FromSeconds(10);
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd(ClienteScryfall.IdentificacaoAplicativo);
            cliente.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        if (configuracao.GetValue("Scryfall:Habilitado", true))
            servicos.AddHostedService<ServicoEnriquecimentoScryfall>();

        return servicos;
    }
}
