using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Commander500.Dominio.Comum;

/// <summary>Padroniza nomes de comandante para que grafias diferentes virem o mesmo comandante.</summary>
public static partial class NomeComandante
{
    /// <summary>
    /// Chave de comparação: tira espaços sobrando, troca apóstrofo curvo por reto, tira acentos e deixa minúsculo.
    /// "  Atraxa,  Praetors’ Voice " e "atraxa, praetors' voice" geram a mesma chave.
    /// </summary>
    public static string Normalizar(string nome)
    {
        var texto = Limpar(nome).Replace('’', '\'').Replace('‘', '\'').Normalize(NormalizationForm.FormD);
        var semAcento = new StringBuilder(texto.Length);
        foreach (var letra in texto)
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(letra);
        return semAcento.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>Forma de exibição: sem espaços nas pontas e com um espaço só entre as palavras.</summary>
    public static string Limpar(string nome) => EspacosRepetidos().Replace(nome.Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRepetidos();
}
