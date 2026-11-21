using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Configurações gerais da loja (por enquanto, o link do grupo de WhatsApp).</summary>
public class ServicoConfiguracaoLoja(IContextoBanco banco, TimeProvider relogio)
{
    public async Task<ConfiguracaoLojaResposta> Obter()
    {
        var configuracao = await banco.ConfiguracoesLoja.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();
        return new ConfiguracaoLojaResposta(configuracao?.LinkGrupoWhatsapp);
    }

    public async Task<ConfiguracaoLojaResposta> Salvar(ConfiguracaoLojaEnvio envio, string? usuario)
    {
        var link = RegrasLoja.NormalizarLinkWhatsapp(envio.LinkGrupoWhatsapp);
        var configuracao = await banco.ConfiguracoesLoja.OrderBy(c => c.Id).FirstOrDefaultAsync();
        if (configuracao is null)
        {
            configuracao = new ConfiguracaoLoja();
            banco.ConfiguracoesLoja.Add(configuracao);
        }
        configuracao.LinkGrupoWhatsapp = link;
        configuracao.AlteradaEm = relogio.GetUtcNow().UtcDateTime;
        configuracao.AlteradaPor = usuario;
        await banco.SalvarAsync();
        return new ConfiguracaoLojaResposta(link);
    }
}
