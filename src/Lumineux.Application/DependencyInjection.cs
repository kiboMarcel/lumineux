using FluentValidation;
using Lumineux.Application.AttendanceSessions;
using Lumineux.Application.Attendances;
using Lumineux.Application.Auth;
using Lumineux.Application.BureauProfiles;
using Lumineux.Application.Members;
using Lumineux.Application.Setup;
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

        services.AddScoped<LoginHandler>();
        services.AddScoped<ActivateAccountHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<GetCurrentUserHandler>();

        services.AddScoped<Reference.GetReferenceDataHandler>();

        services.AddScoped<CreateBureauProfileHandler>();
        services.AddScoped<UpdateBureauProfileHandler>();
        services.AddScoped<DeleteBureauProfileHandler>();
        services.AddScoped<AssignProfileHandler>();
        services.AddScoped<RevokeProfileHandler>();
        services.AddScoped<ListBureauProfilesHandler>();
        services.AddScoped<GetBureauProfileHandler>();
        services.AddScoped<GetMemberProfilesHandler>();
        services.AddScoped<ListPermissionsHandler>();

        services.AddScoped<InstallFirstAdminHandler>();
        services.AddScoped<GetSetupStatusHandler>();

        return services;
    }
}
