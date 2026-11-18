using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;

namespace Commander500.Dominio.Regras;

/// <summary>Etapas são criadas automaticamente: uma por data dentro da temporada, numeradas pela data.</summary>
public static class RegrasEtapa
{
    /// <summary>Devolve a etapa da temporada naquela <paramref name="data"/>, criando uma nova na lista se precisar.</summary>
    public static Etapa Resolver(Temporada temporada, List<Etapa> etapas, DateOnly data)
    {
        if (data < temporada.DataInicio || (temporada.DataFim is { } fim && data > fim))
            throw new DadosInvalidosException($"A data deve estar dentro da temporada ({temporada.DataInicio:dd/MM/yyyy}" +
                                              (temporada.DataFim is { } f ? $" a {f:dd/MM/yyyy})." : " em diante)."));

        var etapa = etapas.FirstOrDefault(e => e.Data == data);
        if (etapa is null)
        {
            etapa = new Etapa { Temporada = temporada, IdTemporada = temporada.Id, Data = data };
            etapas.Add(etapa);
            Renumerar(etapas);
        }
        return etapa;
    }

    /// <summary>Numera as etapas 1..N pela data.</summary>
    public static void Renumerar(IEnumerable<Etapa> etapas)
    {
        var numero = 1;
        foreach (var etapa in etapas.OrderBy(e => e.Data)) etapa.Numero = numero++;
    }
}
