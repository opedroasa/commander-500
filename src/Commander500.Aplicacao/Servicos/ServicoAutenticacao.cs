using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Login do lojista (área admin).</summary>
public class ServicoAutenticacao(IContextoBanco banco, IServicoSenha servicoSenha, IGeradorToken geradorToken)
{
    /// <summary>Devolve o token JWT, ou null se e-mail/senha não conferem.</summary>
    public async Task<LoginResposta?> Entrar(LoginEnvio envio)
    {
        var email = (envio.Email ?? "").Trim().ToLowerInvariant();
        var administrador = await banco.Administradores.FirstOrDefaultAsync(a => a.Email == email);
        if (administrador is null) return null;

        var verificacao = servicoSenha.Verificar(administrador.HashSenha, envio.Senha ?? "");
        if (verificacao == VerificacaoSenha.Invalida) return null;
        if (verificacao == VerificacaoSenha.ValidaPrecisaAtualizarHash)
        {
            administrador.HashSenha = servicoSenha.GerarHash(envio.Senha!);
            await banco.SalvarAsync();
        }

        var token = geradorToken.Gerar(administrador);
        return new LoginResposta(token.Token, token.ExpiraEm, administrador.Email);
    }
}
