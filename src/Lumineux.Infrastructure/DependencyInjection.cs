using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Infrastructure.BackgroundJobs;
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

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TestTokenIssuer>();

        services.AddScoped<IAttendanceSessionRepository, AttendanceSessionRepository>();
        services.AddScoped<IAntennaReadRepository, AntennaReadRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IMemberReadRepository, MemberReadRepository>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default");
            options.UseSqlServer(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        return services;
    }
}
