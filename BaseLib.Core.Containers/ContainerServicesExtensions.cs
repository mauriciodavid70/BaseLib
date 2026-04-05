using BaseLib.Core.Mail;
using BaseLib.Core.Security;
using BaseLib.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BaseLib.Core.Containers
{
    /// <summary>
    /// Configuration options for container-runtime service implementations.
    /// Pass to <see cref="ContainerServicesExtensions.AddContainerServices"/> during startup.
    /// </summary>
    public class ContainerServicesOptions
    {
        /// <summary>
        /// Root directory under which <see cref="FileSystemCoreServiceStateStore"/> writes state files.
        /// </summary>
        public string StateStoreRootDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Hostname or IP address of the SMTP server used by <see cref="SmtpEmailSender"/>.
        /// </summary>
        public string SmtpHost { get; set; } = string.Empty;

        /// <summary>
        /// TCP port of the SMTP server used by <see cref="SmtpEmailSender"/>.
        /// </summary>
        public int SmtpPort { get; set; }

        /// <summary>
        /// Optional SMTP authentication username. Leave <see langword="null"/> for unauthenticated relay.
        /// </summary>
        public string? SmtpUsername { get; set; }

        /// <summary>
        /// Optional SMTP authentication password. Leave <see langword="null"/> for unauthenticated relay.
        /// </summary>
        public string? SmtpPassword { get; set; }
    }

    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods for registering container-runtime
    /// implementations of the BaseLib.Core interfaces.
    /// </summary>
    public static class ContainerServicesExtensions
    {
        /// <summary>
        /// Registers <see cref="FileSystemCoreServiceStateStore"/>, <see cref="EnvironmentSecretsVault"/>,
        /// and <see cref="SmtpEmailSender"/> as the active implementations of
        /// <see cref="ICoreServiceStateStore"/>, <see cref="ICoreSecretsVault"/>, and
        /// <see cref="IEmailSender"/> respectively.
        /// </summary>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <param name="configure">Action to populate <see cref="ContainerServicesOptions"/>.</param>
        /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddContainerServices(
            this IServiceCollection services,
            Action<ContainerServicesOptions> configure)
        {
            var options = new ContainerServicesOptions();
            configure(options);

            services.AddSingleton<ICoreServiceStateStore>(
                _ => new FileSystemCoreServiceStateStore(options.StateStoreRootDirectory));

            services.AddSingleton<ICoreSecretsVault, EnvironmentSecretsVault>();

            services.AddSingleton<IEmailSender>(
                _ => new SmtpEmailSender(options.SmtpHost, options.SmtpPort, options.SmtpUsername, options.SmtpPassword));

            return services;
        }
    }
}
