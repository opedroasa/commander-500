namespace Commander500.Dominio.Comum;

/// <summary>
/// Fuso horário da loja (Uberlândia = horário de Brasília). O banco guarda tudo em UTC; o fuso só serve para saber
/// qual é a data de "hoje" na loja (ex.: uma rodada de ontem é retroativa).
/// </summary>
public static class FusoLoja
{
    private static readonly TimeZoneInfo Fuso = Carregar();

    private static TimeZoneInfo Carregar()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília"); }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília"); }
    }

    /// <summary>Data de hoje na loja a partir de um instante UTC.</summary>
    public static DateOnly Hoje(DateTime agoraUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc), Fuso));
}
