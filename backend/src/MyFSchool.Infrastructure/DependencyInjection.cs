using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using MyFSchool.Application.Readiness;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Application.Imports;
using MyFSchool.Infrastructure.Configuration;
using MyFSchool.Infrastructure.Readiness;
using MyFSchool.Infrastructure.Identity;
using MyFSchool.Infrastructure.Imports;
using MyFSchool.Infrastructure.Persistence;
using MyFSchool.Infrastructure.School;

namespace MyFSchool.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationContentRoot)
    {
        var repositoryRoot = ResolveRepositoryRoot(applicationContentRoot);
        var connectionString = configuration.GetConnectionString("Default") ?? string.Empty;

        services
            .AddOptions<DatabaseOptions>()
            .Configure(options =>
            {
                options.ConnectionString = connectionString;
            })
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Missing required setting: ConnectionStrings__Default")
            .Validate(
                options => IsValidSqlServerConnectionString(options.ConnectionString),
                "ConnectionStrings__Default must be a valid SQL Server connection string")
            .ValidateOnStart();

        services.AddDbContext<MyFSchoolDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDataProtection();
        services.AddSingleton(TimeProvider.System);
        services
            .AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Missing required setting: Auth__Issuer")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Missing required setting: Auth__Audience")
            .Validate(options => Encoding.UTF8.GetByteCount(options.JwtSigningKey) >= 32, "Auth__JwtSigningKey must contain at least 32 bytes")
            .Validate(options => options.AccessTokenMinutes is >= 1 and <= 60, "Auth__AccessTokenMinutes must be between 1 and 60")
            .Validate(options => options.RestrictedTokenMinutes is >= 1 and <= 15, "Auth__RestrictedTokenMinutes must be between 1 and 15")
            .Validate(options => options.RefreshTokenDays is >= 1 and <= 30, "Auth__RefreshTokenDays must be between 1 and 30")
            .Validate(options => options.TemporaryPasswordHours is >= 1 and <= 72, "Auth__TemporaryPasswordHours must be between 1 and 72")
            .ValidateOnStart();
        services
            .AddOptions<BootstrapOptions>()
            .Bind(configuration.GetSection(BootstrapOptions.SectionName))
            .Validate(
                options => !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.AdministratorUserName) &&
                     !string.IsNullOrWhiteSpace(options.AdministratorEmail) &&
                     !string.IsNullOrWhiteSpace(options.AdministratorDisplayName) &&
                     !string.IsNullOrWhiteSpace(options.AdministratorPassword)),
                "Bootstrap Administrator settings are required when Bootstrap__Enabled is true")
            .ValidateOnStart();
        services
            .AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .Validate(
                options => !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.Host) &&
                     options.Port is > 0 and <= 65535 &&
                     !string.IsNullOrWhiteSpace(options.UserName) &&
                     !string.IsNullOrWhiteSpace(options.Password) &&
                     !string.IsNullOrWhiteSpace(options.FromEmail)),
                "Gmail SMTP settings are required when Smtp__Enabled is true")
            .Validate(
                options => !options.Enabled ||
                    string.Equals(options.Security, "startTls", StringComparison.OrdinalIgnoreCase),
                "Smtp__Security must be startTls")
            .ValidateOnStart();
        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<MyFSchoolDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHelpService, PasswordHelpService>();
        services.AddScoped<IAccountAdministrationService, AccountAdministrationService>();
        services.AddScoped<IIdentityRelationshipAdministrationService, RelationshipAdministrationService>();
        services.AddScoped<IRelationshipAuthorizationService, RelationshipAuthorizationService>();
        services.AddScoped<IIdentityBootstrapper, IdentityBootstrapper>();
        services.AddScoped<ISchoolReferenceAdministrationService, SchoolReferenceAdministrationService>();
        services.AddScoped<ISchoolScopeQueryService, SchoolScopeQueryService>();
        services.AddScoped<IAttendanceAdministrationService, AttendanceAdministrationService>();
        services.AddScoped<ILeaveRequestAdministrationService, LeaveRequestAdministrationService>();
        services.AddScoped<IClubAdministrationService, ClubAdministrationService>();
        services.AddScoped<IGradeAdministrationService, GradeAdministrationService>();
        services.AddScoped<ITimetableQueryService, TimetableQueryService>();
        services.AddScoped<IEventQueryService, EventQueryService>();
        services.AddScoped<IAnnouncementAdministrationService, AnnouncementAdministrationService>();
        services.AddScoped<IAnnouncementQueryService, AnnouncementQueryService>();
        services.AddScoped<IAnnouncementEmailSender, GmailSmtpAnnouncementEmailSender>();
        services.AddScoped<IExcelImportService, ExcelImportService>();

        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .Validate(
                options => string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase),
                "Storage__Provider must be Local")
            .Validate(
                options => Path.IsPathFullyQualified(options.LocalRoot),
                "Storage__LocalRoot must be an absolute path")
            .Validate(
                options => IsOutsideRepository(options.LocalRoot, repositoryRoot),
                "Storage__LocalRoot must be outside the repository")
            .ValidateOnStart();

        services.AddSingleton<IReadinessProbe, ReadinessProbe>();

        return services;
    }

    private static string ResolveRepositoryRoot(string contentRoot)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(contentRoot));
        for (var current = directory; current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
            {
                return current.FullName;
            }
        }

        throw new InvalidOperationException("Unable to resolve repository root from application content root.");
    }

    private static bool IsOutsideRepository(string path, string repositoryRoot)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            return false;
        }

        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var fullRepositoryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot));
        var relativePath = Path.GetRelativePath(fullRepositoryRoot, fullPath);

        return Path.IsPathRooted(relativePath) ||
               relativePath == ".." ||
               relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static bool IsValidSqlServerConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var hasAnySqlCredential = !string.IsNullOrWhiteSpace(builder.UserID) ||
                                      !string.IsNullOrWhiteSpace(builder.Password);
            var hasCompleteSqlCredentials = !string.IsNullOrWhiteSpace(builder.UserID) &&
                                            !string.IsNullOrWhiteSpace(builder.Password);
            var hasIntegratedAuthentication = builder.IntegratedSecurity &&
                                              !hasAnySqlCredential &&
                                              builder.Authentication == SqlAuthenticationMethod.NotSpecified;
            var hasSqlAuthentication = !builder.IntegratedSecurity &&
                                       hasCompleteSqlCredentials &&
                                       builder.Authentication is SqlAuthenticationMethod.NotSpecified or
                                           SqlAuthenticationMethod.SqlPassword;

            return !string.IsNullOrWhiteSpace(builder.DataSource) &&
                   !string.IsNullOrWhiteSpace(builder.InitialCatalog) &&
                   (hasIntegratedAuthentication || hasSqlAuthentication);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
