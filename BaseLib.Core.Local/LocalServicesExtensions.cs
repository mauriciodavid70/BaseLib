using BaseLib.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BaseLib.Core.Local
{
    /// <summary>
    /// Configuration options for local (single-process) service implementations.
    /// Pass to <see cref="LocalServicesExtensions.AddLocalServices"/> during startup.
    /// </summary>
    public class LocalServicesOptions
    {
        /// <summary>
        /// Root directory under which <see cref="FileSystemCoreServiceStateStore"/> writes state files.
        /// Defaults to <see cref="Path.GetTempPath()"/>.
        /// </summary>
        public string StateStoreRootDirectory { get; set; } = Path.GetTempPath();
    }

    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods for registering local (single-process)
    /// implementations of the BaseLib.Core interfaces.
    /// </summary>
    public static class LocalServicesExtensions
    {
        /// <summary>
        /// Registers <see cref="FileSystemCoreServiceStateStore"/> as the active implementation of
        /// <see cref="ICoreServiceStateStore"/>.
        /// </summary>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <param name="configure">
        /// Optional action to configure <see cref="LocalServicesOptions"/>.
        /// When <see langword="null"/> the default options are used (state files written to the OS temp directory).
        /// </param>
        /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddLocalServices(
            this IServiceCollection services,
            Action<LocalServicesOptions>? configure = null)
        {
            var options = new LocalServicesOptions();
            configure?.Invoke(options);

            services.AddSingleton<ICoreServiceStateStore>(
                _ => new FileSystemCoreServiceStateStore(options.StateStoreRootDirectory));

            return services;
        }
    }
}
