using System.Net;
using System.Net.Http.Headers;
using Commander500.Aplicacao.Interfaces;
using Commander500.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace Commander500.Infraestrutura.Fotos;

/// <summary>
/// Configuração das fotos (seção "Fotos" / variáveis Fotos__…).
/// Com <see cref="SupabaseUrl"/> e <see cref="SupabaseChave"/> preenchidos, as fotos vão para o Supabase Storage;
/// sem eles, ficam numa pasta local servida pela Api (bom para desenvolvimento; na Vercel a pasta some a cada deploy).
/// </summary>
public class OpcoesFotos
{
    public const string Secao = "Fotos";
    /// <summary>Ex.: https://abcd.supabase.co (Project URL do painel do Supabase).</summary>
    public string? SupabaseUrl { get; set; }
    /// <summary>Chave secreta do projeto (secret key / service_role). Só fica no servidor, nunca no site.</summary>
    public string? SupabaseChave { get; set; }
    /// <summary>Bucket público criado no Supabase Storage.</summary>
    public string Bucket { get; set; } = "fotos";
    /// <summary>Pasta local usada quando o Supabase não está configurado (relativa à pasta de onde a Api roda).</summary>
    public string PastaLocal { get; set; } = "fotos-local";

    public bool UsaSupabase => !string.IsNullOrWhiteSpace(SupabaseUrl) && !string.IsNullOrWhiteSpace(SupabaseChave);
}

/// <summary>Confere o caminho da foto: só letras, números, "-", "_", "." e "/" (sem "..").</summary>
internal static class CaminhoFoto
{
    public static string Validar(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho) || caminho.Contains("..") || caminho.StartsWith('/')
            || caminho.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '/')))
            throw new DadosInvalidosException("Caminho de foto inválido.");
        return caminho;
    }
}

/// <summary>Fotos numa pasta do computador, servidas pela própria Api em /api/fotos/… (desenvolvimento).</summary>
public class ArmazenamentoFotosLocal(OpcoesFotos opcoes) : IArmazenamentoFotos
{
    public const string RotaPublica = "/api/fotos/";

    private string Pasta => Path.GetFullPath(opcoes.PastaLocal);

    /// <summary>Caminho completo no disco (garante que fica dentro da pasta de fotos).</summary>
    public string CaminhoNoDisco(string caminho)
    {
        var completo = Path.GetFullPath(Path.Combine(Pasta, CaminhoFoto.Validar(caminho)));
        if (!completo.StartsWith(Pasta + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new DadosInvalidosException("Caminho de foto inválido.");
        return completo;
    }

    public async Task Salvar(string caminho, Stream conteudo, string tipoConteudo, CancellationToken cancelamento = default)
    {
        var destino = CaminhoNoDisco(caminho);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        await using var arquivo = File.Create(destino);
        await conteudo.CopyToAsync(arquivo, cancelamento);
    }

    public Task Excluir(string caminho, CancellationToken cancelamento = default)
    {
        var destino = CaminhoNoDisco(caminho);
        if (File.Exists(destino)) File.Delete(destino);
        return Task.CompletedTask;
    }

    public string? Url(string? caminho) => caminho is null ? null : RotaPublica + caminho;
}

/// <summary>
/// Fotos no Supabase Storage (produção), num bucket público. Envio e exclusão pela API de Storage com a chave
/// secreta; o site mostra a foto direto do endereço público do Supabase (CDN).
/// </summary>
public class ArmazenamentoFotosSupabase(HttpClient http, OpcoesFotos opcoes, ILogger<ArmazenamentoFotosSupabase> log) : IArmazenamentoFotos
{
    private string Base => opcoes.SupabaseUrl!.TrimEnd('/');

    private HttpRequestMessage Requisicao(HttpMethod metodo, string caminho)
    {
        var requisicao = new HttpRequestMessage(metodo, $"{Base}/storage/v1/object/{opcoes.Bucket}/{CaminhoFoto.Validar(caminho)}");
        // Chaves novas (sb_secret_…) vão só no "apikey"; a service_role antiga (JWT) também vai no Authorization.
        requisicao.Headers.Add("apikey", opcoes.SupabaseChave);
        if (opcoes.SupabaseChave!.StartsWith("eyJ", StringComparison.Ordinal))
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opcoes.SupabaseChave);
        return requisicao;
    }

    public async Task Salvar(string caminho, Stream conteudo, string tipoConteudo, CancellationToken cancelamento = default)
    {
        using var requisicao = Requisicao(HttpMethod.Post, caminho);
        requisicao.Headers.Add("x-upsert", "true");
        requisicao.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromDays(365) };
        requisicao.Content = new StreamContent(conteudo);
        requisicao.Content.Headers.ContentType = new MediaTypeHeaderValue(tipoConteudo);
        using var resposta = await http.SendAsync(requisicao, cancelamento);
        if (!resposta.IsSuccessStatusCode)
        {
            log.LogError("Supabase Storage recusou a foto {Caminho}: {Status} {Corpo}", caminho, (int)resposta.StatusCode,
                await resposta.Content.ReadAsStringAsync(cancelamento));
            throw new ServicoIndisponivelException("Não foi possível guardar a foto agora. Tente de novo em instantes.");
        }
    }

    public async Task Excluir(string caminho, CancellationToken cancelamento = default)
    {
        using var requisicao = Requisicao(HttpMethod.Delete, caminho);
        using var resposta = await http.SendAsync(requisicao, cancelamento);
        if (!resposta.IsSuccessStatusCode && resposta.StatusCode != HttpStatusCode.NotFound)
            log.LogWarning("Não foi possível apagar a foto {Caminho} do Supabase: {Status}", caminho, (int)resposta.StatusCode);
    }

    public string? Url(string? caminho) =>
        caminho is null ? null : $"{Base}/storage/v1/object/public/{opcoes.Bucket}/{caminho}";
}
