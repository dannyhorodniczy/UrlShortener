using Dapper;
using DbUp.Builder;
using Npgsql;
using System;
using System.Linq;

namespace DatabaseMigrator;

internal static class Extensions
{
    internal static UpgradeEngineBuilder CreateDatabaseIfNotExists(
        this UpgradeEngineBuilder builder,
        string connectionString,
        bool dropDatabase = true)
    {
        string pgConnectionString = GetPostgresConnectionString(connectionString, out string originalDbName);
        using var postgresDb = new NpgsqlConnection(pgConnectionString);
        postgresDb.Open();

        if (dropDatabase)
        {
            postgresDb.Execute($"DROP DATABASE IF EXISTS {originalDbName} WITH (FORCE);");
        }

        var exists = postgresDb.Query($"SELECT 1 FROM pg_database WHERE datname = '{originalDbName}'");

        if (!exists.Any())
        {
            postgresDb.Execute(
                $"""
             CREATE DATABASE {originalDbName} WITH
                 OWNER DEFAULT
                 ENCODING 'UTF8'
                 LC_COLLATE 'en_US.UTF-8'
                 LC_CTYPE 'en_US.UTF-8'
                 CONNECTION LIMIT -1
                 TEMPLATE template0
                 IS_TEMPLATE False;
             """);
        }

        return builder;
    }

    private static string GetPostgresConnectionString(string connectionString, out string originalDbName)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        originalDbName = builder.Database ?? throw new InvalidOperationException("Missing database name");
        builder.Database = "postgres";

        return builder.ConnectionString;
    }
}

