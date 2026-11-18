using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Comandantes: busca no Scryfall e criação automática ao salvar um deck (não há cadastro manual).</summary>
public class ServicoComandante(IContextoBanco banco, IClienteScryfall scryfall, TimeProvider relogio, ILogger<ServicoComandante> log)
{
    /// <summary>Campo de comandante do cadastro de deck: busca comandantes e backgrounds no Scryfall.</summary>
    public async Task<IReadOnlyList<CartaScryfallResposta>> Pesquisar(string? busca)
    {
        try
        {
            return (await scryfall.PesquisarComandantes(busca ?? "")).Select(c => c.ParaResposta()).ToList();
        }
        catch (HttpRequestException)
        {
            throw new ServicoIndisponivelException("Não foi possível consultar o Scryfall agora. Digite o nome do comandante manualmente.");
        }
    }

    /// <summary>
    /// Encontra ou cria os comandantes do deck (ainda sem salvar). Com o id do Scryfall, os dados da carta são buscados e guardados;
    /// só com o nome, procura pelo nome normalizado ou cria um novo (o serviço de enriquecimento busca a arte depois).
    /// </summary>
    public async Task<List<Comandante>> Resolver(IReadOnlyList<ComandanteEnvio> informados)
    {
        var escolhidos = informados.Where(c => c.IdScryfall is not null || !string.IsNullOrWhiteSpace(c.Nome)).ToList();
        if (escolhidos.Count is < 1 or > 2) throw new DadosInvalidosException("O deck deve ter 1 ou 2 comandantes.");

        var resultado = new List<Comandante>();
        foreach (var informado in escolhidos)
        {
            Comandante? comandante = null;
            CartaScryfall? carta = null;

            if (informado.IdScryfall is { } idScryfall)
            {
                comandante = await banco.Comandantes.FirstOrDefaultAsync(c => c.IdScryfall == idScryfall)
                             ?? banco.Comandantes.Local.FirstOrDefault(c => c.IdScryfall == idScryfall);
                if (comandante is null)
                {
                    try { carta = await scryfall.ObterPorId(idScryfall); }
                    catch (HttpRequestException erro) { log.LogInformation("Scryfall indisponível ao salvar deck: {Mensagem}", erro.Message); }
                }
            }

            if (comandante is null)
            {
                var nome = NomeComandante.Limpar(carta?.Nome ?? informado.Nome);
                if (nome.Length == 0) throw new DadosInvalidosException("Nome do comandante é obrigatório.");
                var nomeNormalizado = NomeComandante.Normalizar(nome);
                comandante = await banco.Comandantes.FirstOrDefaultAsync(c => c.NomeNormalizado == nomeNormalizado)
                             ?? banco.Comandantes.Local.FirstOrDefault(c => c.NomeNormalizado == nomeNormalizado);
                if (comandante is null)
                {
                    comandante = new Comandante { Nome = nome, NomeNormalizado = nomeNormalizado };
                    banco.Comandantes.Add(comandante);
                }
                if (carta is not null && comandante.IdScryfall is null)
                    DadosScryfall.Aplicar(comandante, carta, relogio.GetUtcNow().UtcDateTime);
            }

            if (resultado.Contains(comandante)) throw new DadosInvalidosException("Comandantes repetidos.");
            resultado.Add(comandante);
        }
        return resultado;
    }
}
