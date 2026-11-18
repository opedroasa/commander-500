using Commander500.Dominio.Comum;

namespace Commander500.Dominio.Entidades;

/// <summary>
/// Comandante (carta). Não é cadastrado à mão: é criado ao salvar um deck, a partir do Scryfall
/// ou só pelo nome digitado (o serviço de enriquecimento completa a arte depois).
/// </summary>
public class Comandante
{
    public int Id { get; set; }
    /// <summary>Nome oficial (do Scryfall) ou como foi digitado.</summary>
    public string Nome { get; set; } = "";
    /// <summary>Chave única, ver <see cref="NomeComandante.Normalizar"/>.</summary>
    public string NomeNormalizado { get; set; } = "";

    // ----- Dados guardados do Scryfall (nulos quando o nome foi digitado e o Scryfall ainda não foi consultado)
    public Guid? IdScryfall { get; set; }
    /// <summary>Arte recortada (banner, miniaturas).</summary>
    public string? UrlArte { get; set; }
    /// <summary>Imagem da carta inteira.</summary>
    public string? UrlImagem { get; set; }
    /// <summary>Cores em ordem WUBRG, ex.: "WUB". Vazio = incolor.</summary>
    public string? IdentidadeCor { get; set; }
    /// <summary>Última consulta ao Scryfall, para não repetir nomes desconhecidos a cada inicialização.</summary>
    public DateTime? DataConsultaScryfall { get; set; }
}
