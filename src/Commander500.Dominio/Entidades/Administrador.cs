namespace Commander500.Dominio.Entidades;

/// <summary>Usuário do lojista (área admin). Criado na inicialização a partir das variáveis Admin__Email / Admin__Senha.</summary>
public class Administrador
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string HashSenha { get; set; } = "";
}
