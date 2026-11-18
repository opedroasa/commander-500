using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Regulamento da liga (o "tratado"): página pública e edição pelo lojista.</summary>
public class ServicoRegulamento(IContextoBanco banco, TimeProvider relogio)
{
    /// <summary>Limite do HTML salvo (o tratado inteiro tem ~15 mil caracteres).</summary>
    public const int TamanhoMaximo = 200_000;

    public async Task<RegulamentoResposta> Obter()
    {
        var regulamento = await banco.Regulamentos.AsNoTracking().OrderBy(r => r.Id).FirstOrDefaultAsync();
        return regulamento is null
            ? new RegulamentoResposta("", null, null)
            : new RegulamentoResposta(regulamento.Conteudo, regulamento.AlteradoEm, regulamento.AlteradoPor);
    }

    public async Task<RegulamentoResposta> Salvar(RegulamentoEnvio envio, string? usuario)
    {
        var conteudo = envio.Conteudo ?? "";
        if (conteudo.Length > TamanhoMaximo)
            throw new DadosInvalidosException($"O regulamento passou do limite de {TamanhoMaximo:N0} caracteres.");

        var regulamento = await banco.Regulamentos.OrderBy(r => r.Id).FirstOrDefaultAsync();
        if (regulamento is null)
        {
            regulamento = new Regulamento();
            banco.Regulamentos.Add(regulamento);
        }
        regulamento.Conteudo = conteudo;
        regulamento.AlteradoEm = relogio.GetUtcNow().UtcDateTime;
        regulamento.AlteradoPor = usuario;
        await banco.SalvarAsync();
        return await Obter();
    }
}
