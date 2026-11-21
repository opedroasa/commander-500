using Commander500.Dominio.Comum;

namespace Commander500.Dominio.Regras;

/// <summary>
/// Fotos do top da etapa e da temporada. O site reduz a foto no celular antes de enviar (~300 KB);
/// o limite aqui só protege o armazenamento (Supabase Storage, 1 GB no plano grátis).
/// </summary>
public static class RegrasFoto
{
    public const long TamanhoMaximoBytes = 5 * 1024 * 1024;
    public const int TamanhoMaximoLegenda = 200;

    /// <summary>Tipos aceitos e a extensão usada no nome do arquivo.</summary>
    private static readonly Dictionary<string, string> Extensoes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    /// <summary>Confere tipo e tamanho e devolve a extensão do arquivo.</summary>
    public static string Validar(string? tipoConteudo, long tamanhoBytes)
    {
        if (tamanhoBytes <= 0) throw new DadosInvalidosException("A foto está vazia.");
        if (tamanhoBytes > TamanhoMaximoBytes)
            throw new DadosInvalidosException($"A foto passou de {TamanhoMaximoBytes / 1024 / 1024} MB.");
        if (tipoConteudo is null || !Extensoes.TryGetValue(tipoConteudo.Split(';')[0].Trim(), out var extensao))
            throw new DadosInvalidosException("Envie uma foto JPG, PNG ou WEBP.");
        return extensao;
    }

    /// <summary>Legenda opcional: espaços tirados, vazia vira nula.</summary>
    public static string? NormalizarLegenda(string? legenda)
    {
        var texto = (legenda ?? "").Trim();
        if (texto.Length == 0) return null;
        if (texto.Length > TamanhoMaximoLegenda)
            throw new DadosInvalidosException($"A legenda pode ter no máximo {TamanhoMaximoLegenda} caracteres.");
        return texto;
    }

    /// <summary>
    /// Caminho do arquivo no armazenamento, ex.: "etapas/12-20261121143005.jpg". A data no nome faz cada foto nova
    /// ter um endereço novo (o navegador e o CDN não mostram a antiga do cache).
    /// </summary>
    public static string MontarCaminho(string pasta, int id, string extensao, DateTime agoraUtc) =>
        $"{pasta}/{id}-{agoraUtc:yyyyMMddHHmmss}.{extensao}";
}
