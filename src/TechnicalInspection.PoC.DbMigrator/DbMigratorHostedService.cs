using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TechnicalInspection.PoC.Data;
using Serilog;
using Volo.Abp;
using Volo.Abp.Data;

namespace TechnicalInspection.PoC.DbMigrator;

public class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _configuration;

    public DbMigratorHostedService(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        /* The ABP application already connects to the database while initializing,
         * so the database has to exist before the migration service runs. */
        await EnsureDatabaseExistsAsync(
            _configuration.GetConnectionString(ConnectionStrings.DefaultConnectionStringName),
            cancellationToken);

        using (var application = await AbpApplicationFactory.CreateAsync<PoCDbMigratorModule>(options =>
        {
           options.Services.ReplaceConfiguration(_configuration);
           options.UseAutofac();
           options.Services.AddLogging(c => c.AddSerilog());
           options.AddDataMigrationEnvironment();
        }))
        {
            await application.InitializeAsync();

            await application
                .ServiceProvider
                .GetRequiredService<PoCDbMigrationService>()
                .MigrateAsync();

            await application.ShutdownAsync();

            _hostApplicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /* EF Core only creates the database when it can tell that it is missing.
     * A login that has no access to a not-yet-existing database gets error 4060
     * ("Cannot open database ... The login failed."), so we create it up front
     * through the master database instead.
     */
    private static async Task EnsureDatabaseExistsAsync(string? connectionString, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new AbpException(
                $"The '{ConnectionStrings.DefaultConnectionStringName}' connection string is not configured. " +
                "Check that appsettings.json was copied next to the migrator executable.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new AbpException(
                $"The '{ConnectionStrings.DefaultConnectionStringName}' connection string does not name a database.");
        }

        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            IF DB_ID(@databaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@databaseName);
                EXEC sp_executesql @sql;
            END
            """;
        command.Parameters.Add("@databaseName", SqlDbType.NVarChar, 128).Value = databaseName;

        await command.ExecuteNonQueryAsync(cancellationToken);

        Log.Information("Ensured database {DatabaseName} exists on {DataSource}.", databaseName, builder.DataSource);
    }
}
