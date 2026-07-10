using System.Text;
using Lumineux.Api.Middleware;
using Lumineux.Api.Security;
using Lumineux.Application;
using Lumineux.Application.Abstractions;
using Lumineux.Infrastructure;
using Lumineux.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// --- Services applicatifs (Onion) ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- Authentification JWT ---
// Les options sont configurées de façon différée via IOptions<JwtOptions> afin que la clé de
// signature soit lue après finalisation de la configuration (indispensable pour les tests).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Permissions.ManageAttendance, policy =>
        policy.RequireClaim("permission", Permissions.ManageAttendance));
    options.AddPolicy(Permissions.ManageMembers, policy =>
        policy.RequireClaim("permission", Permissions.ManageMembers));
    options.AddPolicy(Permissions.ManageBureauProfiles, policy =>
        policy.RequireClaim("permission", Permissions.ManageBureauProfiles));
    options.AddPolicy(Permissions.ManageReferentials, policy =>
        policy.RequireClaim("permission", Permissions.ManageReferentials));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Lumineux — API Présence", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

// --- CORS (consommation par la SPA Angular, feature 008) ---
// Origines autorisées lues depuis la configuration (Cors:AllowedOrigins). L'authentification se fait
// par jeton Bearer (en-tête Authorization) : aucune gestion de cookies/credentials n'est requise, on
// n'active donc jamais AllowCredentials. Sans origine configurée, aucune requête cross-origin n'est
// autorisée (comportement sûr par défaut).
const string spaCorsPolicy = "SpaCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(spaCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// --- Garde-fou sécurité : clé de signature JWT obligatoire et suffisante (dette C2). ---
// Lu APRÈS Build pour prendre en compte toutes les sources (user-secrets en dev, variables
// d'environnement/secrets en prod, config injectée par les tests d'intégration). Aucune clé n'est
// committée : démarrage refusé si absente/trop courte, pour éviter des jetons signés avec une clé vide.
var signingKey = app.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey manquante ou trop courte (min. 32 octets). Fournissez-la hors du code : "
        + "`dotnet user-secrets set \"Jwt:SigningKey\" \"<clé>\"` en dev, ou variable d'environnement "
        + "`Jwt__SigningKey` / magasin de secrets en production.");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

// Journalisation structurée de chaque requête HTTP (méthode, chemin, code de statut, durée).
// Garantit la traçabilité des refus d'authentification (401) produits par le middleware
// d'autorisation, y compris pour les endpoints protégés (feature 007, FR-009 / Constitution VI).
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(spaCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Rendu accessible aux tests d'intégration (WebApplicationFactory<Program>).
public partial class Program;
