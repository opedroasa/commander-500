using Commander500.Dominio.Comum;

namespace Commander500.Dominio.Regras;

/// <summary>Regras das configurações gerais da loja.</summary>
public static class RegrasLoja
{
    public const int TamanhoMaximoLink = 300;

    /// <summary>Endereços aceitos para o grupo: convite de grupo (chat.whatsapp.com) ou link direto (wa.me / whatsapp.com).</summary>
    private static readonly string[] DominiosWhatsapp = ["chat.whatsapp.com", "wa.me", "whatsapp.com", "www.whatsapp.com"];

    /// <summary>
    /// Confere o link do grupo de WhatsApp. Vazio vira nulo (o botão some do site).
    /// Só aceita https e endereços do WhatsApp, para ninguém colocar um link estranho no menu do site.
    /// </summary>
    public static string? NormalizarLinkWhatsapp(string? link)
    {
        var texto = (link ?? "").Trim();
        if (texto.Length == 0) return null;
        if (texto.Length > TamanhoMaximoLink)
            throw new DadosInvalidosException($"O link pode ter no máximo {TamanhoMaximoLink} caracteres.");
        if (!Uri.TryCreate(texto, UriKind.Absolute, out var endereco) || endereco.Scheme != Uri.UriSchemeHttps)
            throw new DadosInvalidosException("O link precisa começar com https://");
        if (!DominiosWhatsapp.Contains(endereco.Host.ToLowerInvariant()))
            throw new DadosInvalidosException("Use o link de convite do WhatsApp (https://chat.whatsapp.com/…).");
        return endereco.ToString();
    }
}
