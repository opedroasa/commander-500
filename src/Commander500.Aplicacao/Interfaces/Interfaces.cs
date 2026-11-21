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
    DbSet<ConfiguracaoLoja> ConfiguracoesLoja { get; }

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

/// <summary>
/// Onde ficam as fotos (top da etapa e da temporada). Em produção é o Supabase Storage; em desenvolvimento, uma pasta
/// local servida pela própria Api. Implementado na Infraestrutura.
/// </summary>
public interface IArmazenamentoFotos
{
    /// <summary>Grava a foto no <paramref name="caminho"/> (ex.: "etapas/12-20261121143005.jpg").</summary>
    Task Salvar(string caminho, Stream conteudo, string tipoConteudo, CancellationToken cancelamento = default);
    /// <summary>Apaga a foto. Não falha se ela já não existir.</summary>
    Task Excluir(string caminho, CancellationToken cancelamento = default);
    /// <summary>Endereço público para o site mostrar a foto (nulo quando não há foto).</summary>
    string? Url(string? caminho);
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
