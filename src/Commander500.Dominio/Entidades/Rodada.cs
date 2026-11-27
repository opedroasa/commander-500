using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Entidades;

/// <summary>
/// Rodada: presentes são sorteados em mesas de 4, jogam com timer e o lojista registra o resultado de cada mesa.
/// Fluxo e regras em <see cref="Regras.FluxoRodada"/>.
/// </summary>
public class Rodada
{
    public int Id { get; set; }
    public int IdTemporada { get; set; }
    public Temporada? Temporada { get; set; }
    public int IdEtapa { get; set; }
    public Etapa? Etapa { get; set; }
    /// <summary>Sequencial na temporada (1, 2, 3…). A posição dentro da etapa é calculada.</summary>
    public int Numero { get; set; }
    /// <summary>Mesma data da etapa (guardada para consultas e histórico).</summary>
    public DateOnly Data { get; set; }
    public SituacaoRodada Situacao { get; set; } = SituacaoRodada.Rascunho;
    /// <summary>Quando o timer começou (UTC). O tempo restante é calculado a partir daqui (spec 4.4).</summary>
    public DateTime? IniciadaEm { get; set; }
    public int DuracaoMinutos { get; set; }
    public DateTime? EncerradaEm { get; set; }
    public List<Participacao> Participacoes { get; set; } = [];
    public List<Mesa> Mesas { get; set; } = [];
}

/// <summary>Presença de um jogador numa rodada, com o deck que ele joga.</summary>
public class Participacao
{
    public int Id { get; set; }
    public int IdRodada { get; set; }
    public Rodada? Rodada { get; set; }
    public int IdJogador { get; set; }
    public Jogador? Jogador { get; set; }
    public int IdDeck { get; set; }
    public Deck? Deck { get; set; }
    /// <summary>Ficou fora das mesas porque o número de presentes não fechou múltiplo de 4.</summary>
    public bool Bye { get; set; }
}

/// <summary>Mesa (pod) de 4 jogadores. Resultado: um vencedor ou empate.</summary>
public class Mesa
{
    public int Id { get; set; }
    public int IdRodada { get; set; }
    public Rodada? Rodada { get; set; }
    public int Numero { get; set; }
    public ResultadoMesa Resultado { get; set; } = ResultadoMesa.Pendente;
    public int? IdParticipacaoVencedora { get; set; }
    public Participacao? ParticipacaoVencedora { get; set; }
    /// <summary>Auditoria do lançamento/correção do resultado.</summary>
    public DateTime? AlteradaEm { get; set; }
    public string? AlteradaPor { get; set; }
    public List<Cadeira> Cadeiras { get; set; } = [];

    /// <summary>
    /// O jogador desta participação ficou no empate? Empate sem ninguém marcado = todos empataram;
    /// com marcados (empate parcial), só eles empataram e os outros contam como derrota.
    /// </summary>
    public bool Empatou(int idParticipacao)
    {
        if (Resultado != ResultadoMesa.Empate) return false;
        var marcados = Cadeiras.Where(c => c.Empatou).ToList();
        return marcados.Count == 0 || marcados.Any(c => (c.Participacao?.Id ?? c.IdParticipacao) == idParticipacao);
    }
}

/// <summary>Lugar de um jogador na mesa. Número 1..4; a cadeira 1 começa jogando.</summary>
public class Cadeira
{
    public int IdMesa { get; set; }
    public Mesa? Mesa { get; set; }
    public int IdParticipacao { get; set; }
    public Participacao? Participacao { get; set; }
    public int Numero { get; set; }
    /// <summary>
    /// Empate parcial: true para quem ficou no empate (ex.: 2 sobreviventes de 4). Se a mesa deu empate e ninguém está
    /// marcado, o empate foi entre todos (como antes). Ver <see cref="Mesa.Empatou"/>.
    /// </summary>
    public bool Empatou { get; set; }
}
