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

namespace Lumineux.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AutoCloseOptions>(configuration.GetSection(AutoCloseOptions.SectionName));
        services.AddHostedService<SessionAutoCloseService>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IQrTokenService, QrTokenService>();
        services.AddSingleton<IResetTokenService, ResetTokenService>();

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TestTokenIssuer>();

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
        services.AddScoped<IMemberPermissionRepository, MemberPermissionRepository>();
        services.AddHostedService<PermissionBootstrapper>();

        // Feature 006 — mot de passe oublié
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Feature 004 — profils du bureau
        services.AddSingleton<IPermissionCatalog, PermissionCatalog>();
        services.AddScoped<IBureauProfileRepository, BureauProfileRepository>();
        services.AddHostedService<BureauProfilesBootstrapper>();

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
