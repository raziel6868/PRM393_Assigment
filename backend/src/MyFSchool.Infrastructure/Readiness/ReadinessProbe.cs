using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFSchool.Application.Readiness;
using MyFSchool.Infrastructure.Configuration;
using System.Security;

namespace MyFSchool.Infrastructure.Readiness;

internal sealed class ReadinessProbe(
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<StorageOptions> storageOptions,
    ILogger<ReadinessProbe> logger) : IReadinessProbe
{
    public async Task<ReadinessReport> CheckAsync(CancellationToken cancellationToken)
    {
        var components = new[]
        {
            await CheckDatabaseAsync(databaseOptions.Value.ConnectionString, cancellationToken),
            await CheckStorageAsync(storageOptions.Value.LocalRoot, cancellationToken),
        };

        return new ReadinessReport(components);
    }

    private async Task<ReadinessComponent> CheckDatabaseAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            await command.ExecuteScalarAsync(cancellationToken);

            return new ReadinessComponent("database", true);
        }
        catch (SqlException exception)
        {
            return Unavailable("database", exception);
        }
    }

    private async Task<ReadinessComponent> CheckStorageAsync(
        string storageRoot,
        CancellationToken cancellationToken)
    {
        try
        {
            var directory = new DirectoryInfo(storageRoot);
            if (!directory.Exists || directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return new ReadinessComponent("storage", false);
            }

            var probePath = Path.Combine(storageRoot, $".readiness-{Guid.NewGuid():N}.tmp");
            await using var stream = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            await stream.WriteAsync(new byte[] { 1 }, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            return new ReadinessComponent("storage", true);
        }
        catch (IOException exception)
        {
            return Unavailable("storage", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unavailable("storage", exception);
        }
        catch (SecurityException exception)
        {
            return Unavailable("storage", exception);
        }
    }

    private ReadinessComponent Unavailable(string component, Exception exception)
    {
        logger.LogWarning(
            "Readiness component {Component} is unavailable ({ExceptionType}).",
            component,
            exception.GetType().Name);

        return new ReadinessComponent(component, false);
    }
}
