using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Typed view over the field-level state dictionary persisted by
    /// <see cref="ICoreServiceStateStore"/> for long-running services.
    /// Provides strongly-typed accessors for the common framework fields and
    /// generic <see cref="Get{T}"/> / <see cref="Set{T}"/> helpers for custom fields.
    /// </summary>
    public class CoreServiceState
    {
        private readonly IDictionary<string, object> environment;

        /// <summary>Creates a state view over an optional pre-populated dictionary.</summary>
        /// <param name="environment">Existing state dictionary, or <see langword="null"/> to start empty.</param>
        public CoreServiceState(IDictionary<string, object>? environment = null)
        {
            this.environment = environment ?? new Dictionary<string, object>(StringComparer.Ordinal);
        }

        /// <summary>UTC timestamp when the service started.</summary>
        public DateTimeOffset StartedOn => this.Get<DateTimeOffset>("StartedOn");
        /// <summary>UTC timestamp when the service finished or was suspended.</summary>
        public DateTimeOffset FinishedOn => this.Get<DateTimeOffset>("FinishedOn");
        /// <summary>Unique operation identifier.</summary>
        public string? OperationId => this.Get<string?>("OperationId");
        /// <summary>Correlation identifier linking related operations.</summary>
        public string? CorrelationId => this.Get<string?>("CorrelationId");
        /// <summary>The original request payload.</summary>
        public CoreRequestBase? Request => this.Get<CoreRequestBase?>("Request");
        /// <summary>The response produced so far, or <see langword="null"/> if not yet set.</summary>
        public CoreResponseBase? Response => this.Get<CoreResponseBase?>("Response");

        /// <summary>Retrieves a typed value from the state dictionary by key.</summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="key">The state dictionary key.</param>
        public virtual T? Get<T>(string key)
        {
            return environment.TryGetValue(key, out var value) ? (T)value : default;
        }
        
        /// <summary>Stores a value in the state dictionary under the given key.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The state dictionary key.</param>
        /// <param name="value">The value to store. <see langword="null"/> values are ignored.</param>
        public virtual void Set<T>(string key, T value)
        {
            if (value == null) { return; }
            this.environment[key] = value;
        }
    }

}