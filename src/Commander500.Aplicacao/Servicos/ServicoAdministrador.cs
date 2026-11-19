using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Cadastro de administradores pela tela do admin (listar, criar, trocar senha, excluir).</summary>
public class ServicoAdministrador(IContextoBanco banco, IServicoSenha servicoSenha)
{
    /// <param name="idUsuarioLogado">Id de quem está logado, para marcar "você" na lista.</param>
    public async Task<List<AdministradorResposta>> Listar(int? idUsuarioLogado) =>
        await banco.Administradores.AsNoTracking()
            .OrderBy(a => a.Email)
            .Select(a => new AdministradorResposta(a.Id, a.Email, a.Id == idUsuarioLogado))
            .ToListAsync();

    public async Task<AdministradorResposta> Criar(AdministradorEnvio envio, int? idUsuarioLogado)
    {
        var email = RegrasAdministrador.NormalizarEmail(envio.Email);
        RegrasAdministrador.ValidarSenha(envio.Senha);
        if (await banco.Administradores.AnyAsync(a => a.Email == email))
            throw new RegraNegocioException("Já existe um administrador com esse e-mail.");

        var administrador = new Administrador { Email = email, HashSenha = servicoSenha.GerarHash(envio.Senha) };
        banco.Administradores.Add(administrador);
        await banco.SalvarAsync();
        return new AdministradorResposta(administrador.Id, administrador.Email, administrador.Id == idUsuarioLogado);
    }

    /// <summary>Define uma senha nova (serve para a própria senha ou para quem esqueceu a dele).</summary>
    public async Task AlterarSenha(int id, NovaSenhaEnvio envio)
    {
        RegrasAdministrador.ValidarSenha(envio.Senha);
        var administrador = await banco.Administradores.FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NaoEncontradoException("Administrador não encontrado.");
        administrador.HashSenha = servicoSenha.GerarHash(envio.Senha);
        await banco.SalvarAsync();
    }

    public async Task Excluir(int id, int? idUsuarioLogado)
    {
        var administrador = await banco.Administradores.FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NaoEncontradoException("Administrador não encontrado.");
        RegrasAdministrador.ValidarExclusao(id, idUsuarioLogado, await banco.Administradores.CountAsync());
        banco.Administradores.Remove(administrador);
        await banco.SalvarAsync();
    }
}
