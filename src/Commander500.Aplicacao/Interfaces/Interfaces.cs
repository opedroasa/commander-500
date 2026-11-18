using Commander500.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Interfaces;

// Contratos que a Aplicação usa e a Infraestrutura implementa (inversão de dependência).

/// <summary>Acesso ao banco. Implementado pelo ContextoBanco (EF Core + PostgreSQL) na Infraestrutura.</summary>
public interface IContextoBanco
{
    DbSet<Temporada> Temporadas { get; }
    DbSet<Jogador> Jogadores { get; }
    DbSet<Comandante> Comandantes { get; }
    DbSet<Deck> Decks { get; }
    DbSet<DeckComandante> DecksComandantes { get; }
    DbSet<Etapa> Etapas { get; }
    DbSet<Rodada> Rodadas { get; }
    DbSet<Participacao> Participacoes { get; }
    DbSet<Mesa> Mesas { get; }
    DbSet<Cadeira> Cadeiras { get; }
    DbSet<Administrador> Administradores { get; }
    DbSet<Regulamento> Regulamentos { get; }

    /// <summary>Grava no banco tudo o que foi alterado (uma transação).</summary>
    Task<int> SalvarAsync(CancellationToken cancelamento = default);

    /// <summary>Executa várias gravações como uma só: se alguma falhar, nenhuma vale.</summary>
    Task EmTransacaoAsync(Func<Task> acao, CancellationToken cancelamento = default);

    /// <summary>Esquece os objetos carregados, para a próxima consulta vir fresca do banco.</summary>
    void LimparRastreamento();
}

/// <summary>Dados de uma carta guardados do Scryfall.</summary>
/// <param name="IdentidadeCor">Letras em ordem WUBRG; "" = incolor.</param>
public record CartaScryfall(Guid Id, string Nome, string? UrlArte, string? UrlImagem, string IdentidadeCor, string Tipo);

/// <summary>API pública do Scryfall (nomes, artes e cores das cartas). Gratuita e sem chave.</summary>
public interface IClienteScryfall
{
    /// <summary>Comandantes e backgrounds cujo nome contém <paramref name="busca"/>, os mais jogados primeiro.</summary>
    Task<IReadOnlyList<CartaScryfall>> PesquisarComandantes(string busca, CancellationToken cancelamento = default);
    Task<CartaScryfall?> ObterPorId(Guid id, CancellationToken cancelamento = default);
    Task<CartaScryfall?> ObterPorNomeExato(string nome, CancellationToken cancelamento = default);
}

public enum VerificacaoSenha { Invalida, Valida, ValidaPrecisaAtualizarHash }

/// <summary>Gera e confere o hash da senha do administrador.</summary>
public interface IServicoSenha
{
    string GerarHash(string senha);
    VerificacaoSenha Verificar(string hash, string senha);
}

public record TokenGerado(string Token, DateTime ExpiraEm);

/// <summary>Gera o token JWT de login do administrador.</summary>
public interface IGeradorToken
{
    TokenGerado Gerar(Administrador administrador);
}
