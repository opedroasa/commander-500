using Commander500.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commander500.Infraestrutura.Dados.Configuracoes;

// Mapeamento das entidades para as tabelas do PostgreSQL: tamanhos, chaves, índices e o que acontece ao excluir.
// Nomes de tabela = nome do DbSet no ContextoBanco; nomes de coluna = nome da propriedade.

public class TemporadaConfiguracao : IEntityTypeConfiguration<Temporada>
{
    public void Configure(EntityTypeBuilder<Temporada> tabela)
    {
        tabela.Property(t => t.Nome).HasMaxLength(120).IsRequired();
        tabela.Property(t => t.CriteriosDesempate).HasMaxLength(200).IsRequired();
        tabela.Property(t => t.PontosPorColocacao).HasMaxLength(100).IsRequired();
        tabela.Property(t => t.CaminhoFoto).HasMaxLength(300);
        tabela.Property(t => t.LegendaFoto).HasMaxLength(200);
    }
}

public class JogadorConfiguracao : IEntityTypeConfiguration<Jogador>
{
    public void Configure(EntityTypeBuilder<Jogador> tabela)
    {
        tabela.Property(j => j.Nome).HasMaxLength(120).IsRequired();
        tabela.Property(j => j.Apelido).HasMaxLength(60);
    }
}

public class ComandanteConfiguracao : IEntityTypeConfiguration<Comandante>
{
    public void Configure(EntityTypeBuilder<Comandante> tabela)
    {
        tabela.Property(c => c.Nome).HasMaxLength(200).IsRequired();
        tabela.Property(c => c.NomeNormalizado).HasMaxLength(200).IsRequired();
        tabela.Property(c => c.UrlArte).HasMaxLength(500);
        tabela.Property(c => c.UrlImagem).HasMaxLength(500);
        tabela.Property(c => c.IdentidadeCor).HasMaxLength(5);
        tabela.HasIndex(c => c.NomeNormalizado).IsUnique();
        tabela.HasIndex(c => c.IdScryfall).IsUnique().HasFilter("\"IdScryfall\" IS NOT NULL");
    }
}

public class DeckConfiguracao : IEntityTypeConfiguration<Deck>
{
    public void Configure(EntityTypeBuilder<Deck> tabela)
    {
        tabela.Property(d => d.Nome).HasMaxLength(120).IsRequired();
        tabela.Property(d => d.UrlLista).HasMaxLength(500);
        tabela.Property(d => d.ObservacaoRevisao).HasMaxLength(1000);
        // Excluir o jogador exclui os decks dele (só é permitido se ele nunca jogou).
        tabela.HasOne(d => d.Jogador).WithMany(j => j.Decks).HasForeignKey(d => d.IdJogador).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeckComandanteConfiguracao : IEntityTypeConfiguration<DeckComandante>
{
    public void Configure(EntityTypeBuilder<DeckComandante> tabela)
    {
        tabela.HasKey(dc => new { dc.IdDeck, dc.IdComandante });
        tabela.HasOne(dc => dc.Deck).WithMany(d => d.Comandantes).HasForeignKey(dc => dc.IdDeck).OnDelete(DeleteBehavior.Cascade);
        tabela.HasOne(dc => dc.Comandante).WithMany().HasForeignKey(dc => dc.IdComandante).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EtapaConfiguracao : IEntityTypeConfiguration<Etapa>
{
    public void Configure(EntityTypeBuilder<Etapa> tabela)
    {
        tabela.HasOne(e => e.Temporada).WithMany(t => t.Etapas).HasForeignKey(e => e.IdTemporada).OnDelete(DeleteBehavior.Cascade);
        tabela.HasIndex(e => new { e.IdTemporada, e.Data, e.Final }).IsUnique();
        tabela.Property(e => e.CaminhoFoto).HasMaxLength(300);
        tabela.Property(e => e.LegendaFoto).HasMaxLength(200); // a final pode cair no mesmo dia da última etapa
        tabela.HasIndex(e => new { e.IdTemporada, e.Numero }); // não é único: ao renumerar pela data as linhas mudam uma a uma
    }
}

public class RodadaConfiguracao : IEntityTypeConfiguration<Rodada>
{
    public void Configure(EntityTypeBuilder<Rodada> tabela)
    {
        tabela.HasOne(r => r.Temporada).WithMany(t => t.Rodadas).HasForeignKey(r => r.IdTemporada).OnDelete(DeleteBehavior.Cascade);
        tabela.HasOne(r => r.Etapa).WithMany(e => e.Rodadas).HasForeignKey(r => r.IdEtapa).OnDelete(DeleteBehavior.Cascade);
        tabela.HasIndex(r => new { r.IdTemporada, r.Numero }).IsUnique();
    }
}

public class ParticipacaoConfiguracao : IEntityTypeConfiguration<Participacao>
{
    public void Configure(EntityTypeBuilder<Participacao> tabela)
    {
        tabela.HasOne(p => p.Rodada).WithMany(r => r.Participacoes).HasForeignKey(p => p.IdRodada).OnDelete(DeleteBehavior.Cascade);
        tabela.HasOne(p => p.Jogador).WithMany().HasForeignKey(p => p.IdJogador).OnDelete(DeleteBehavior.Restrict);
        tabela.HasOne(p => p.Deck).WithMany().HasForeignKey(p => p.IdDeck).OnDelete(DeleteBehavior.Restrict);
        tabela.HasIndex(p => new { p.IdRodada, p.IdJogador }).IsUnique(); // um jogador por rodada
    }
}

public class MesaConfiguracao : IEntityTypeConfiguration<Mesa>
{
    public void Configure(EntityTypeBuilder<Mesa> tabela)
    {
        tabela.HasOne(m => m.Rodada).WithMany(r => r.Mesas).HasForeignKey(m => m.IdRodada).OnDelete(DeleteBehavior.Cascade);
        tabela.HasOne(m => m.ParticipacaoVencedora).WithMany().HasForeignKey(m => m.IdParticipacaoVencedora).OnDelete(DeleteBehavior.SetNull);
        tabela.Property(m => m.AlteradaPor).HasMaxLength(200);
        tabela.HasIndex(m => new { m.IdRodada, m.Numero }).IsUnique();
    }
}

public class CadeiraConfiguracao : IEntityTypeConfiguration<Cadeira>
{
    public void Configure(EntityTypeBuilder<Cadeira> tabela)
    {
        tabela.HasKey(c => new { c.IdMesa, c.IdParticipacao });
        tabela.HasOne(c => c.Mesa).WithMany(m => m.Cadeiras).HasForeignKey(c => c.IdMesa).OnDelete(DeleteBehavior.Cascade);
        tabela.HasOne(c => c.Participacao).WithMany().HasForeignKey(c => c.IdParticipacao).OnDelete(DeleteBehavior.Cascade);
        tabela.HasIndex(c => new { c.IdMesa, c.Numero }).IsUnique(); // uma pessoa por cadeira
        tabela.ToTable(t => t.HasCheckConstraint("CK_Cadeiras_Numero", "\"Numero\" BETWEEN 1 AND 6"));
    }
}

public class AdministradorConfiguracao : IEntityTypeConfiguration<Administrador>
{
    public void Configure(EntityTypeBuilder<Administrador> tabela)
    {
        tabela.Property(a => a.Email).HasMaxLength(200).IsRequired();
        tabela.HasIndex(a => a.Email).IsUnique();
    }
}

public class RegulamentoConfiguracao : IEntityTypeConfiguration<Regulamento>
{
    public void Configure(EntityTypeBuilder<Regulamento> tabela)
    {
        tabela.Property(r => r.Conteudo).IsRequired(); // HTML do editor (text no PostgreSQL)
        tabela.Property(r => r.AlteradoPor).HasMaxLength(200);
    }
}

public class ConfiguracaoLojaConfiguracao : IEntityTypeConfiguration<ConfiguracaoLoja>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoLoja> tabela)
    {
        tabela.Property(c => c.LinkGrupoWhatsapp).HasMaxLength(300);
        tabela.Property(c => c.AlteradaPor).HasMaxLength(200);
    }
}
