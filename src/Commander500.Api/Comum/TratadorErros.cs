using Commander500.Dominio.Comum;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Commander500.Api.Comum;

/// <summary>
/// Converte as exceções em resposta HTTP padronizada (ProblemDetails com a mensagem em "detail"):
/// DadosInvalidos → 400, NaoEncontrado → 404, RegraNegocio / registro duplicado → 409, ServicoIndisponivel → 503.
/// </summary>
public class TratadorErros(IProblemDetailsService detalhesProblema, ILogger<TratadorErros> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception erro, CancellationToken cancelamento)
    {
        (int status, string titulo, string? detalhe) = erro switch
        {
            DadosInvalidosException e => (400, "Dados inválidos", e.Message),
            NaoEncontradoException e => (404, "Não encontrado", e.Message),
            ServicoIndisponivelException e => (503, "Serviço indisponível", e.Message),
            RegraNegocioException e => (409, "Operação não permitida", e.Message),
            BadHttpRequestException e => (400, "Requisição inválida", e.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (409, "Conflito", "Registro duplicado."),
            _ => (500, "Erro interno", null),
        };
        if (status == 500) log.LogError(erro, "Erro não tratado");

        contexto.Response.StatusCode = status;
        return await detalhesProblema.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = erro,
            ProblemDetails = new ProblemDetails { Status = status, Title = titulo, Detail = detalhe },
        });
    }
}
