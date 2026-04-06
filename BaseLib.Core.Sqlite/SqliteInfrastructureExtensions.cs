using BaseLib.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace BaseLib.Core.Sqlite
{
    /// <summary>
    /// Configuration options for SQLite infrastructure implementations.
    /// Pass to <see cref="SqliteInfrastructureExtensions.AddSqliteInfrastructure"/> during startup.
    /// </summary>
    public class SqliteInfrastructureOptions
    {
        /// <summary>
        /// SQLite connection string (e.g. <c>Data Source=baselib.db</c> or <c>Data Source=:memory:</c>).
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
    }

    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods for registering SQLite-backed
    /// implementations of the BaseLib.Core interfaces.
    /// </summary>
    public static class SqliteInfrastructureExtensions
    {
        /// <summary>
        /// Registers <see cref="SqliteCoreLongRunningServiceManager"/> as the active implementation of
        /// <see cref="ICoreLongRunningServiceManager"/>.  The <c>LONG_RUNNING_BATCH</c> table is
        /// created automatically on first use (idempotent <c>CREATE TABLE IF NOT EXISTS</c>).
        /// </summary>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <param name="configure">Action to populate <see cref="SqliteInfrastructureOptions"/>.</param>
        /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddSqliteInfrastructure(
            this IServiceCollection services,
            Action<SqliteInfrastructureOptions> configure)
        {
            var options = new SqliteInfrastructureOptions();
            configure(options);

            SqliteConnection ConnectionFactory()
            {
                var connection = new SqliteConnection(options.ConnectionString);
                connection.Open();
                return connection;
            }

            services.AddSingleton<ICoreLongRunningServiceManager>(sp =>
                new SqliteCoreLongRunningServiceManager(
                    ConnectionFactory,
                    sp.GetRequiredService<ICoreServiceFireOnly>()));

            return services;
        }
    }
}
