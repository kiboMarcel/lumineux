using FluentValidation;
using Lumineux.Application.AttendanceSessions;
using Lumineux.Application.Attendances;
using Lumineux.Application.Members;
using Microsoft.Extensions.DependencyInjection;

namespace Lumineux.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<StartSessionValidator>();

        services.AddScoped<StartSessionHandler>();
        services.AddScoped<GetSessionHandler>();
        services.AddScoped<GetCurrentQrTokenHandler>();
        services.AddScoped<CloseSessionHandler>();

        services.AddScoped<ScanAttendanceHandler>();
        services.AddScoped<SyncOfflineScansHandler>();

        services.AddScoped<AddManualAttendanceHandler>();
        services.AddScoped<CancelAttendanceHandler>();
        services.AddScoped<ListAttendancesHandler>();

        services.AddScoped<CreateMemberHandler>();
        services.AddScoped<SearchMembersHandler>();
        services.AddScoped<GetMemberHandler>();
        services.AddScoped<UpdateMemberHandler>();

        return services;
    }
}
