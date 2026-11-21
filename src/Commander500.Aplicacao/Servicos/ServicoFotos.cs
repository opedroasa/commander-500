using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Arquivo de foto recebido do site (já reduzido no celular).</summary>
public record ArquivoFoto(Stream Conteudo, string TipoConteudo, long TamanhoBytes);

/// <summary>Foto do top da etapa e da temporada: enviar (com legenda), trocar a legenda e remover.</summary>
public class ServicoFotos(IContextoBanco banco, IArmazenamentoFotos armazenamento, TimeProvider relogio)
{
    private const string PastaEtapas = "etapas";
    private const string PastaTemporadas = "temporadas";

    public async Task<FotoResposta> SalvarFotoEtapa(int id, ArquivoFoto? arquivo, string? legenda)
    {
        var etapa = await banco.Etapas.FindAsync(id) ?? throw new NaoEncontradoException($"Etapa {id} não encontrada.");
        (etapa.CaminhoFoto, etapa.LegendaFoto) = await Gravar(PastaEtapas, id, etapa.CaminhoFoto, arquivo, legenda);
        await banco.SalvarAsync();
        return new FotoResposta(armazenamento.Url(etapa.CaminhoFoto), etapa.LegendaFoto);
    }

    public async Task RemoverFotoEtapa(int id)
    {
        var etapa = await banco.Etapas.FindAsync(id) ?? throw new NaoEncontradoException($"Etapa {id} não encontrada.");
        var caminho = etapa.CaminhoFoto;
        (etapa.CaminhoFoto, etapa.LegendaFoto) = (null, null);
        await banco.SalvarAsync();
        if (caminho is not null) await armazenamento.Excluir(caminho);
    }

    public async Task<FotoResposta> SalvarFotoTemporada(int id, ArquivoFoto? arquivo, string? legenda)
    {
        var temporada = await banco.Temporadas.FindAsync(id) ?? throw new NaoEncontradoException($"Temporada {id} não encontrada.");
        (temporada.CaminhoFoto, temporada.LegendaFoto) = await Gravar(PastaTemporadas, id, temporada.CaminhoFoto, arquivo, legenda);
        await banco.SalvarAsync();
        return new FotoResposta(armazenamento.Url(temporada.CaminhoFoto), temporada.LegendaFoto);
    }

    public async Task RemoverFotoTemporada(int id)
    {
        var temporada = await banco.Temporadas.FindAsync(id) ?? throw new NaoEncontradoException($"Temporada {id} não encontrada.");
        var caminho = temporada.CaminhoFoto;
        (temporada.CaminhoFoto, temporada.LegendaFoto) = (null, null);
        await banco.SalvarAsync();
        if (caminho is not null) await armazenamento.Excluir(caminho);
    }

    /// <summary>
    /// Sem arquivo: só troca a legenda (precisa já ter foto). Com arquivo: grava a nova e apaga a antiga.
    /// Devolve o caminho e a legenda que ficam no registro.
    /// </summary>
    private async Task<(string? Caminho, string? Legenda)> Gravar(string pasta, int id, string? caminhoAtual, ArquivoFoto? arquivo, string? legenda)
    {
        var legendaNormalizada = RegrasFoto.NormalizarLegenda(legenda);
        if (arquivo is null)
        {
            if (caminhoAtual is null) throw new DadosInvalidosException("Envie a foto.");
            return (caminhoAtual, legendaNormalizada);
        }

        var extensao = RegrasFoto.Validar(arquivo.TipoConteudo, arquivo.TamanhoBytes);
        var caminhoNovo = RegrasFoto.MontarCaminho(pasta, id, extensao, relogio.GetUtcNow().UtcDateTime);
        await armazenamento.Salvar(caminhoNovo, arquivo.Conteudo, arquivo.TipoConteudo);
        // A antiga só sai depois que a nova foi gravada; se apagar falhar, sobra um arquivo, mas o site continua certo.
        if (caminhoAtual is not null && caminhoAtual != caminhoNovo)
        {
            try { await armazenamento.Excluir(caminhoAtual); }
            catch (Exception) { /* arquivo antigo órfão não quebra nada */ }
        }
        return (caminhoNovo, legendaNormalizada);
    }
}
