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

/// <param name="Data">Data da etapa: rodadas na mesma data ficam na mesma etapa. Pode ser uma data passada (rodada retroativa).</param>
/// <param name="IdEtapa">Opcional: cria a rodada nesta etapa (usado pela mesa final e pelo botão "nova rodada"). Ignora a data.</param>
public record NovaRodadaEnvio(DateOnly? Data, int? DuracaoMinutos, int? IdEtapa = null);

/// <param name="Mesas">Cada mesa é a lista de ids de participação na ordem das cadeiras (a primeira começa). Quem ficar de fora é bye.</param>
public record MontagemMesasEnvio(IReadOnlyList<IReadOnlyList<int>> Mesas);

/// <summary>Jogador que passa a começar a mesa (vai para a cadeira 1; os outros giram junto).</summary>
public record QuemComecaEnvio(int IdParticipacao);

/// <param name="ProximaEtapaEm">Data e hora da próxima etapa (com fuso). Nula = some da página inicial.</param>
/// <param name="PrazoListas">Último dia para enviar as listas.</param>
public record ProximaEtapaEnvio(DateTimeOffset? ProximaEtapaEm, DateOnly? PrazoListas);

/// <param name="Finalistas">Jogadores (normalmente o top 4) e o deck de cada um.</param>
/// <param name="QuantidadeRodadas">Quantas rodadas os finalistas vão jogar.</param>
public record MesaFinalEnvio(DateOnly Data, int QuantidadeRodadas, IReadOnlyList<ParticipanteInformado> Finalistas, int? DuracaoMinutos = null);

/// <param name="Conteudo">HTML gerado pelo editor visual.</param>
public record RegulamentoEnvio(string Conteudo);

public record IniciarRodadaEnvio(int? DuracaoMinutos);

public record ResultadoMesaEnvio(ResultadoMesa Resultado, int? IdParticipacaoVencedora);
