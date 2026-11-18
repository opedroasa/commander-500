namespace Commander500.Dominio.Comum;

// Exceções de regra de negócio. A Api transforma cada uma em uma resposta HTTP (ver Api/Comum/TratadorErros.cs).

/// <summary>Regra de negócio violada (ex.: sortear uma rodada já iniciada). Vira HTTP 409.</summary>
public class RegraNegocioException(string mensagem) : Exception(mensagem);

/// <summary>Dado enviado inválido (campo vazio, valor fora do limite). Vira HTTP 400.</summary>
public class DadosInvalidosException(string mensagem) : RegraNegocioException(mensagem);

/// <summary>Registro não encontrado. Vira HTTP 404.</summary>
public class NaoEncontradoException(string mensagem) : Exception(mensagem);

/// <summary>Serviço externo fora do ar (Scryfall). Vira HTTP 503.</summary>
public class ServicoIndisponivelException(string mensagem) : Exception(mensagem);
