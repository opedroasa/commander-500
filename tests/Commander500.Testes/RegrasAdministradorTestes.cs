using Commander500.Dominio.Comum;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

public class RegrasAdministradorTestes
{
    [Theory]
    [InlineData("  Lojista@FantasyStore.com.br ", "lojista@fantasystore.com.br")]
    [InlineData("pedro@gmail.com", "pedro@gmail.com")]
    public void Email_fica_sem_espacos_e_em_minusculas(string informado, string esperado) =>
        Assert.Equal(esperado, RegrasAdministrador.NormalizarEmail(informado));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba")]
    [InlineData("nome@")]
    [InlineData("@dominio.com")]
    [InlineData("nome@semponto")]
    [InlineData("Nome Sobrenome <nome@dominio.com>")]
    public void Email_invalido_e_recusado(string? email) =>
        Assert.Throws<DadosInvalidosException>(() => RegrasAdministrador.NormalizarEmail(email));

    [Fact]
    public void Email_muito_longo_e_recusado() =>
        Assert.Throws<DadosInvalidosException>(() => RegrasAdministrador.NormalizarEmail(new string('a', 195) + "@x.com"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("        ")]
    public void Senha_curta_ou_vazia_e_recusada(string? senha) =>
        Assert.Throws<DadosInvalidosException>(() => RegrasAdministrador.ValidarSenha(senha));

    [Fact]
    public void Senha_com_8_caracteres_e_aceita() => RegrasAdministrador.ValidarSenha("12345678");

    [Fact]
    public void Senha_muito_longa_e_recusada() =>
        Assert.Throws<DadosInvalidosException>(() => RegrasAdministrador.ValidarSenha(new string('x', 129)));

    [Fact]
    public void Ninguem_exclui_a_si_mesmo() =>
        Assert.Throws<RegraNegocioException>(() => RegrasAdministrador.ValidarExclusao(idExcluido: 2, idUsuarioLogado: 2, totalAdministradores: 3));

    [Fact]
    public void Ultimo_administrador_nao_e_excluido() =>
        Assert.Throws<RegraNegocioException>(() => RegrasAdministrador.ValidarExclusao(idExcluido: 1, idUsuarioLogado: null, totalAdministradores: 1));

    [Fact]
    public void Exclui_outro_administrador_quando_ha_mais_de_um() =>
        RegrasAdministrador.ValidarExclusao(idExcluido: 1, idUsuarioLogado: 2, totalAdministradores: 2);
}
