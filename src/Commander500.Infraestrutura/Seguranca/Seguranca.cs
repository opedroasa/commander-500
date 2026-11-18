using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Commander500.Aplicacao.Interfaces;
using Commander500.Dominio.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Commander500.Infraestrutura.Seguranca;

/// <summary>Configuração do token de login (seção "Jwt" do appsettings / variáveis Jwt__Chave etc.).</summary>
public class OpcoesJwt
{
    public const string Secao = "Jwt";
    /// <summary>Chave secreta com pelo menos 32 caracteres. Em produção vem da variável Jwt__Chave.</summary>
    public string Chave { get; set; } = "";
    public string Emissor { get; set; } = "commander500";
    public string Publico { get; set; } = "commander500";
    public int HorasExpiracao { get; set; } = 12;

    public SymmetricSecurityKey ChaveAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}

/// <summary>Gera o token JWT do administrador (papel "Admin").</summary>
public class GeradorTokenJwt(IOptions<OpcoesJwt> opcoes, TimeProvider relogio) : IGeradorToken
{
    public const string PapelAdmin = "Admin";

    public TokenGerado Gerar(Administrador administrador)
    {
        var configuracao = opcoes.Value;
        var expiraEm = relogio.GetUtcNow().UtcDateTime.AddHours(configuracao.HorasExpiracao);
        var token = new JwtSecurityToken(
            issuer: configuracao.Emissor,
            audience: configuracao.Publico,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, administrador.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, administrador.Email),
                new Claim(ClaimTypes.Role, PapelAdmin),
            ],
            expires: expiraEm,
            signingCredentials: new SigningCredentials(configuracao.ChaveAssinatura(), SecurityAlgorithms.HmacSha256));

        return new TokenGerado(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}

/// <summary>Hash de senha com o PasswordHasher do ASP.NET Core Identity (PBKDF2).</summary>
public class ServicoSenha : IServicoSenha
{
    private readonly PasswordHasher<Administrador> _hasher = new();
    private static readonly Administrador Ninguem = new(); // o PasswordHasher pede um usuário, mas não usa

    public string GerarHash(string senha) => _hasher.HashPassword(Ninguem, senha);

    public VerificacaoSenha Verificar(string hash, string senha) => _hasher.VerifyHashedPassword(Ninguem, hash, senha) switch
    {
        PasswordVerificationResult.Success => VerificacaoSenha.Valida,
        PasswordVerificationResult.SuccessRehashNeeded => VerificacaoSenha.ValidaPrecisaAtualizarHash,
        _ => VerificacaoSenha.Invalida,
    };
}
