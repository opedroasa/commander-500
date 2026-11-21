namespace Commander500.Dominio.Entidades;

/// <summary>
/// Configurações gerais da loja, editadas pelo lojista na tela de admin. Há um só registro.
/// </summary>
public class ConfiguracaoLoja
{
    public int Id { get; set; }
    /// <summary>Convite do grupo de WhatsApp do Commander 500 (ex.: https://chat.whatsapp.com/…). Vazio = botão some.</summary>
    public string? LinkGrupoWhatsapp { get; set; }
    public DateTime AlteradaEm { get; set; }
    public string? AlteradaPor { get; set; }
}
