using System.Security.Claims;
using System.Text.Json.Serialization;
using Commander500.Api.Comum;
using Commander500.Api.Endpoints;
using Commander500.Aplicacao;
using Commander500.Infraestrutura;
using Commander500.Infraestrutura.Dados;
using Commander500.Infraestrutura.Seguranca;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuracao = builder.Configuration;

// A Vercel (container), o Docker e o Render usam a porta da variável PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } porta)
    builder.WebHost.UseUrls($"http://0.0.0.0:{porta}");

// ----- Camadas (arquitetura limpa): Aplicação (casos de uso) e Infraestrutura (banco, Scryfall, JWT)
builder.Services.AdicionarAplicacao();
builder.Services.AdicionarInfraestrutura(configuracao);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorErros>();

// ----- Login do lojista (JWT)
var opcoesJwt = configuracao.GetSection(OpcoesJwt.Secao).Get<OpcoesJwt>() ?? new OpcoesJwt();
if (opcoesJwt.Chave.Length < 32)
    throw new InvalidOperationException("Jwt:Chave ausente ou curta (mínimo 32 caracteres). Defina a variável Jwt__Chave.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = opcoesJwt.Emissor,
        ValidAudience = opcoesJwt.Publico,
        IssuerSigningKey = opcoesJwt.ChaveAssinatura(),
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
    // Administrador excluído pela tela perde o acesso na hora, mesmo com um token ainda dentro da validade.
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async contexto =>
        {
            var banco = contexto.HttpContext.RequestServices.GetRequiredService<ContextoBanco>();
            var existe = int.TryParse(contexto.Principal?.FindFirstValue("sub"), out var id)
                && await banco.Administradores.AnyAsync(a => a.Id == id);
            if (!existe) contexto.Fail("Administrador não existe mais.");
        },
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(EndpointsAdmin.PoliticaAdmin, p => p.RequireRole(GeradorTokenJwt.PapelAdmin));

// ----- CORS: Cors__OrigensPermitidas = "https://a.com,https://b.com" ("*" libera tudo)
var origens = (configuracao["Cors:OrigensPermitidas"] ?? "")
    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origens.Contains("*")) p.AllowAnyOrigin(); else p.WithOrigins(origens);
    p.AllowAnyHeader().AllowAnyMethod();
}));

// ----- Swagger (documentação para testar a Api)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Commander 500 – Fantasy Store API", Version = "v1" });
    o.CustomSchemaIds(tipo => tipo.FullName?.Replace("+", "."));
    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    o.AddSecurityDefinition("Bearer", esquema);
    o.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = [] });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (configuracao.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "Commander 500 API");
    // Abrir a raiz da Api (http://localhost:5080) leva direto para a documentação.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapearEndpointsPublicos();
app.MapearEndpointsAdmin();

// ----- Migrations + dados iniciais ao subir
if (configuracao.GetValue("Banco:MigrarAoIniciar", true))
{
    using var escopo = app.Services.CreateScope();
    var log = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacao");
    await escopo.ServiceProvider.GetRequiredService<ContextoBanco>().Database.MigrateAsync();
    await SemeadorBanco.SemearAsync(escopo.ServiceProvider, log);
}

app.Run();

public partial class Program;
