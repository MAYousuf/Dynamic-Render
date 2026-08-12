using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechnicalInspection.PoC.Data;
using Volo.Abp.DependencyInjection;

namespace TechnicalInspection.PoC.EntityFrameworkCore;

public class EntityFrameworkCorePoCDbSchemaMigrator
    : IPoCDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCorePoCDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the PoCDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        var dbContext = _serviceProvider.GetRequiredService<PoCDbContext>();

        await EnsureDatabaseExistsAsync(dbContext.Database.GetConnectionString());

        await dbContext
            .Database
            .MigrateAsync();
    }

    /* EF Core only creates the database when it can tell that it is missing.
     * A login that has no access to a not-yet-existing database gets error 4060
     * ("Cannot open database ... The login failed."), so we create it up front
     * through the master database instead.
     */
    private static async Task EnsureDatabaseExistsAsync(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

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

        await command.ExecuteNonQueryAsync();
    }
}
