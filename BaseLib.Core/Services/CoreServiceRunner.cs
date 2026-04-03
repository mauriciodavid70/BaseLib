using BaseLib.Core.Models;
using System.Collections.Concurrent;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Resolves and executes services by assembly-qualified type name using the DI container.
    /// Resolved types are cached for performance on subsequent calls.
    /// Register as a singleton in your DI container.
    /// </summary>
    public class CoreServiceRunner : ICoreServiceRunner
    {
        private readonly ConcurrentDictionary<string, Type> _typeCache = new();
        private readonly IServiceProvider serviceProvider;

        /// <summary>Initializes the runner with the application service provider.</summary>
        /// <param name="serviceProvider">The DI container used to resolve service instances.</param>
        public CoreServiceRunner(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public Task<CoreResponseBase> RunAsync(string typeName, CoreRequestBase request, string? correlationId = null, bool IsLongRunningChild = false)
        {
            var service = this.ResolveServiceByTypeName(typeName) as ICoreServiceBase
                ?? throw new NullReferenceException($"Service '{typeName}' does not implement ICoreServiceBase");
            return service.RunAsync(request, correlationId, IsLongRunningChild);
        }

        /// <inheritdoc/>
        public Task<CoreResponseBase> ResumeAsync(string typeName, string operationId)
        {
            var service = this.ResolveServiceByTypeName(typeName) as ICoreLongRunningService
                ?? throw new NullReferenceException($"Service '{typeName}' does not implement ICoreLongRunningService");
            return service.ResumeAsync(operationId);
        }

        /// <summary>
        /// Creates an instance of ICoreServiceBase based on the fully qualified type name
        /// </summary>
        private object ResolveServiceByTypeName(string typeName)
        {
            // Check cache first for high performance
            var type = _typeCache.GetOrAdd(typeName, ResolveType);

            var service = this.serviceProvider.GetService(type)
                ?? throw new InvalidOperationException($"Failed to resolve service of type '{typeName}'.");

            return service;
        }

        /// <summary>
        /// Resolves a type by its fully qualified name using Type.GetType
        /// </summary>
        private Type ResolveType(string typeName)
        {
            // Use Type.GetType which handles assembly loading for fully qualified names
            var type = Type.GetType(typeName);

            if (type != null)
                return type;

            // As a fallback, try to find it in currently loaded assemblies
            type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName))
                .FirstOrDefault(t => t != null);

            if (type != null)
                return type;

            throw new TypeLoadException($"Could not load type '{typeName}'. Ensure the assembly is referenced and the type name is correct.");
        }
    }

   
}
