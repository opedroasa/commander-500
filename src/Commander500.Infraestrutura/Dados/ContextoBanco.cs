using Commander500.Aplicacao.Interfaces;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Infraestrutura.Dados;

/// <summary>
/// Contexto do EF Core (PostgreSQL). As tabelas têm o nome dos DbSet (Temporadas, Jogadores…) e as colunas o nome
/// das propriedades (Nome, PontosVitoria…). Detalhes de cada tabela em Dados/Configuracoes.
/// </summary>
public class ContextoBanco(DbContextOptions<ContextoBanco> opcoes) : DbContext(opcoes), IContextoBanco
{
    public DbSet<Temporada> Temporadas => Set<Temporada>();
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    public DbSet<Comandante> Comandantes => Set<Comandante>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckComandante> DecksComandantes => Set<DeckComandante>();
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<Rodada> Rodadas => Set<Rodada>();
    public DbSet<Participacao> Participacoes => Set<Participacao>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Cadeira> Cadeiras => Set<Cadeira>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Regulamento> Regulamentos => Set<Regulamento>();
    public DbSet<ConfiguracaoLoja> ConfiguracoesLoja => Set<ConfiguracaoLoja>();

    public Task<int> SalvarAsync(CancellationToken cancelamento = default) => SaveChangesAsync(cancelamento);

    public void LimparRastreamento() => ChangeTracker.Clear();

    public async Task EmTransacaoAsync(Func<Task> acao, CancellationToken cancelamento = default)
    {
        await using var transacao = await Database.BeginTransactionAsync(cancelamento);
        await acao();
        await transacao.CommitAsync(cancelamento);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder convencoes)
    {
        // Enumeradores gravados pelo nome ("EmAndamento"), não pelo número: fica legível no pgAdmin.
        convencoes.Properties<SituacaoDeck>().HaveConversion<string>().HaveMaxLength(20);
        convencoes.Properties<SituacaoRodada>().HaveConversion<string>().HaveMaxLength(20);
        convencoes.Properties<ResultadoMesa>().HaveConversion<string>().HaveMaxLength(20);
        convencoes.Properties<ModoPontuacao>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder modelo) =>
        modelo.ApplyConfigurationsFromAssembly(typeof(ContextoBanco).Assembly);
}
