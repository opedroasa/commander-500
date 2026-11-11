using Commander500.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Commander500.Api.Infrastructure;

/// <summary>Maps domain exceptions to ProblemDetails: Validation→400, NotFound→404, Domain/unique-violation→409.</summary>
public class ErrorHandler(IProblemDetailsService problems, ILogger<ErrorHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        (int status, string title, string? detail) = ex switch
        {
            ValidationException v => (400, "Dados inválidos", v.Message),
            NotFoundException n => (404, "Não encontrado", n.Message),
            ServiceUnavailableException s => (503, "Serviço indisponível", s.Message),
            DomainException d => (409, "Operação não permitida", d.Message),
            BadHttpRequestException b => (400, "Requisição inválida", b.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (409, "Conflito", "Registro duplicado."),
            _ => (500, "Erro interno", null),
        };
        if (status == 500) logger.LogError(ex, "Unhandled exception");

        ctx.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = ex,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }
}

public static class ConnectionStrings
{
    /// <summary>Accepts both Npgsql key/value strings and postgres:// URIs (Neon, Render).</summary>
    public static string Normalize(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        };
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        b.SslMode = query["sslmode"]?.ToLowerInvariant() switch
        {
            "disable" => SslMode.Disable,
            "prefer" => SslMode.Prefer,
            "verify-full" => SslMode.VerifyFull,
            "verify-ca" => SslMode.VerifyCA,
            null => SslMode.Prefer,
            _ => SslMode.Require,
        };
        return b.ConnectionString;
    }
}
