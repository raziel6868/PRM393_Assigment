using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using MyFSchool.Application.Readiness;
using MyFSchool.Infrastructure.Configuration;
using MyFSchool.Infrastructure.Readiness;
using MyFSchool.Infrastructure.Identity;
using MyFSchool.Infrastructure.Persistence;

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
        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<MyFSchoolDbContext>()
            .AddDefaultTokenProviders();

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
