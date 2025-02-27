using DbUp;
using DbUp.Engine;

namespace DatabaseMigrator;

internal class Program
{
    static void Main(string[] args)
    {
        Run("Host=127.0.0.1;Database=urlshortener;Username=postgres_user;Password=postgres_password");
    }

    private static void Run(string dbConnectionString)
    {
        var sqlScript = new SqlScript("INITIAL_MIGRATION",
            """
            DROP SCHEMA IF EXISTS url CASCADE;
            CREATE SCHEMA url;

            CREATE TABLE url.urls
            (
              id       UUID                 NOT NULL,
              longUrl  TEXT                 NOT NULL,
              CONSTRAINT pk_urls PRIMARY KEY (id)
            );
            """);

        var result = DeployChanges.To
            .PostgresqlDatabase(dbConnectionString)
            .CreateDatabaseIfNotExists(dbConnectionString)
            .WithScript(sqlScript)
            //.WithScriptsEmbeddedInAssemblies(assemblies, s => s.StartsWith("Migrations.") && s.EndsWith(".sql"))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            throw result.Error;
        }
    }
}
