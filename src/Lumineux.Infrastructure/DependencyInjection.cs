using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Infrastructure.BackgroundJobs;
using Lumineux.Infrastructure.Email;
using Lumineux.Infrastructure.Observability;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Persistence.Interceptors;
using Lumineux.Infrastructure.Repositories;
using Lumineux.Infrastructure.Security;
using Lumineux.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lumineux.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AutoCloseOptions>(configuration.GetSection(AutoCloseOptions.SectionName));
        services.AddHostedService<SessionAutoCloseService>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IQrTokenService, QrTokenService>();
        services.AddSingleton<IResetTokenService, ResetTokenService>();

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<AuditInterceptor>();

        // Émetteur de jetons dev/tests : **jamais** en production (dette m1). Les tests d'intégration
        // tournent en environnement « Testing » (non-production) et le résolvent depuis la DI.
        if (!environment.IsProduction())
        {
            services.AddScoped<TestTokenIssuer>();
        }

        services.AddScoped<IAttendanceSessionRepository, AttendanceSessionRepository>();
        services.AddScoped<IAntennaReadRepository, AntennaReadRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IMemberReadRepository, MemberReadRepository>();

        // Feature 002 — gestion des membres
        services.Configure<MemberReferenceOptions>(configuration.GetSection(MemberReferenceOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMemberAccountRepository, MemberAccountRepository>();
        services.AddScoped<IReferenceLookupRepository, ReferenceLookupRepository>();
        services.AddScoped<IMemberReferenceGenerator, MemberReferenceGenerator>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        // Feature 003 — authentification
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        // Source unique des droits : les profils du bureau (feature 029, mécanisme hérité retiré).
        services.AddScoped<IEffectivePermissionsReader, EffectivePermissionsReader>();

        // Feature 006 — mot de passe oublié
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Feature 010 — données de référence (nomenclatures, lecture seule)
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();

        // Feature 016 — gestion des antennes (référentiels en écriture)
        services.AddScoped<IAntennaRepository, AntennaRepository>();

        // Feature 018 — rapports & statistiques de présence (agrégations, lecture seule)
        services.AddScoped<IAttendanceReportRepository, AttendanceReportRepository>();

        // Feature 004 — profils du bureau
        services.AddSingleton<IPermissionCatalog, PermissionCatalog>();
        services.AddScoped<IBureauProfileRepository, BureauProfileRepository>();

        var emailProvider = configuration.GetSection($"{EmailOptions.SectionName}:Provider").Value ?? "Logging";
        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default");
            options.UseSqlServer(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        return services;
    }
}
