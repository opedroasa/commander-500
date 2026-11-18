using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Aplicacao.Modelos;

// Dados que o site (React) envia para a API. No JSON os nomes ficam em camelCase (ex.: "idJogador").

public record LoginEnvio(string Email, string Senha);

/// <param name="Configuracao">Opcional: pontuação, bye, timer e desempate.</param>
public record TemporadaEnvio(string Nome, DateOnly DataInicio, DateOnly? DataFim, bool Ativa = true, ConfiguracaoTemporada? Configuracao = null);

public record JogadorEnvio(string Nome, string? Apelido, bool Ativo = true);

/// <summary>Comandante escolhido no cadastro do deck: vindo da busca do Scryfall (com id) ou digitado (só o nome).</summary>
public record ComandanteEnvio(Guid? IdScryfall, string Nome);

/// <param name="Nome">Opcional: quando vazio vira o nome do(s) comandante(s).</param>
/// <param name="Comandantes">1 ou 2 comandantes (parceiro / background).</param>
public record DeckEnvio(int IdJogador, string? Nome, string? UrlLista, IReadOnlyList<ComandanteEnvio> Comandantes);

/// <param name="Observacao">Obrigatória ao reprovar.</param>
public record RevisaoDeckEnvio(SituacaoDeck Situacao, string? Observacao);

/// <param name="Data">Data da etapa: rodadas na mesma data ficam na mesma etapa.</param>
public record NovaRodadaEnvio(DateOnly? Data, int? DuracaoMinutos);

public record IniciarRodadaEnvio(int? DuracaoMinutos);

public record ResultadoMesaEnvio(ResultadoMesa Resultado, int? IdParticipacaoVencedora);
