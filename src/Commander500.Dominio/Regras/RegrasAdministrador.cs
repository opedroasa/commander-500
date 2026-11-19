using System.Net.Mail;
using Commander500.Dominio.Comum;

namespace Commander500.Dominio.Regras;

/// <summary>Regras do cadastro de administradores (usuários da área do lojista).</summary>
public static class RegrasAdministrador
{
    public const int TamanhoMaximoEmail = 200;
    public const int TamanhoMinimoSenha = 8;
    public const int TamanhoMaximoSenha = 128;

    /// <summary>E-mail sem espaços e em minúsculas (o login compara assim). Recusa e-mail vazio ou mal formado.</summary>
    public static string NormalizarEmail(string? email)
    {
        var normalizado = (email ?? "").Trim().ToLowerInvariant();
        if (normalizado.Length == 0)
            throw new DadosInvalidosException("Informe o e-mail.");
        if (normalizado.Length > TamanhoMaximoEmail)
            throw new DadosInvalidosException($"O e-mail pode ter no máximo {TamanhoMaximoEmail} caracteres.");
        if (!MailAddress.TryCreate(normalizado, out var endereco) || endereco.Address != normalizado || !endereco.Host.Contains('.'))
            throw new DadosInvalidosException("E-mail inválido.");
        return normalizado;
    }

    /// <summary>Senha com tamanho mínimo e máximo. Não exige símbolos: uma frase longa é mais segura e fácil de lembrar.</summary>
    public static void ValidarSenha(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < TamanhoMinimoSenha)
            throw new DadosInvalidosException($"A senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.");
        if (senha.Length > TamanhoMaximoSenha)
            throw new DadosInvalidosException($"A senha pode ter no máximo {TamanhoMaximoSenha} caracteres.");
    }

    /// <summary>
    /// Ninguém exclui a si mesmo (evita ficar sem acesso por engano) e o último administrador nunca é excluído.
    /// </summary>
    public static void ValidarExclusao(int idExcluido, int? idUsuarioLogado, int totalAdministradores)
    {
        if (idUsuarioLogado == idExcluido)
            throw new RegraNegocioException("Você não pode excluir o seu próprio acesso. Peça para outro administrador.");
        if (totalAdministradores <= 1)
            throw new RegraNegocioException("Não é possível excluir o último administrador.");
    }
}
