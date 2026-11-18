using Npgsql;

namespace Commander500.Infraestrutura.Dados;

public static class StringConexao
{
    /// <summary>
    /// Aceita tanto o formato do Npgsql ("Host=...;Database=...") quanto o endereço postgres:// que o Neon e o Render
    /// fornecem, e devolve sempre o formato do Npgsql.
    /// </summary>
    public static string Normalizar(string valor)
    {
        if (!valor.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !valor.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return valor;

        var endereco = new Uri(valor);
        var usuarioSenha = endereco.UserInfo.Split(':', 2);
        var montador = new NpgsqlConnectionStringBuilder
        {
            Host = endereco.Host,
            Port = endereco.IsDefaultPort || endereco.Port <= 0 ? 5432 : endereco.Port,
            Database = endereco.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(usuarioSenha[0]),
            Password = usuarioSenha.Length > 1 ? Uri.UnescapeDataString(usuarioSenha[1]) : null,
        };
        var parametros = System.Web.HttpUtility.ParseQueryString(endereco.Query);
        montador.SslMode = parametros["sslmode"]?.ToLowerInvariant() switch
        {
            "disable" => SslMode.Disable,
            "prefer" => SslMode.Prefer,
            "verify-full" => SslMode.VerifyFull,
            "verify-ca" => SslMode.VerifyCA,
            null => SslMode.Prefer,
            _ => SslMode.Require,
        };
        return montador.ConnectionString;
    }
}
