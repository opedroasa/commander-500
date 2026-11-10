using System.Security.Claims;
using System.Text.Json.Serialization;
using Commander500.Api.Application;
using Commander500.Api.Auth;
using Commander500.Api.Data;
using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;
using Commander500.Api.Endpoints;
using Commander500.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Render (and similar hosts) provide the port in $PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ---- Database
var connectionString = new[] { config.GetConnectionString("Default"), Environment.GetEnvironmentVariable("DATABASE_URL") }
    .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
    ?? throw new InvalidOperationException("Connection string ausente: defina ConnectionStrings__Default (ou DATABASE_URL).");
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ConnectionStrings.Normalize(connectionString)));

// ---- Services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRandomSource, CryptoRandomSource>();
builder.Services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddScoped<SeasonService>();
builder.Services.AddScoped<RoundService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<AuthService>();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrorHandler>();

// ---- Auth (JWT for the store admin)
var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key ausente ou curta (mínimo 32 caracteres). Defina a variável Jwt__Key.");
builder.Services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = jwt.SigningKey(),
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
});
builder.Services.AddAuthorizationBuilder().AddPolicy(AdminEndpoints.AdminPolicy, p => p.RequireRole("Admin"));

// ---- CORS: Cors__AllowedOrigins = "https://a.com,https://b.com"
var origins = (config["Cors:AllowedOrigins"] ?? "")
    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origins.Contains("*")) p.AllowAnyOrigin(); else p.WithOrigins(origins);
    p.AllowAnyHeader().AllowAnyMethod();
}));

// ---- Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Commander 500 – Fantasy Store API", Version = "v1" });
    o.CustomSchemaIds(t => t.FullName?.Replace("+", "."));
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    o.AddSecurityDefinition("Bearer", scheme);
    o.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (config.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "Commander 500 API");
}

app.MapPublicEndpoints();
app.MapAdminEndpoints();

// ---- Migrations + seed on startup
if (config.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider, logger);
}

app.Run();

public partial class Program;
