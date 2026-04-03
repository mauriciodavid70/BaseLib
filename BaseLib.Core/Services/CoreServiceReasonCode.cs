using System.ComponentModel;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Built-in reason codes set by the framework on <c>CoreResponseBase.ReasonCode</c>
    /// when a service completes. Domain-specific enums can be used alongside these values.
    /// Declared as <see cref="FlagsAttribute"/> so that <c>Suspended</c> can be OR-ed with other codes.
    /// </summary>
    [Flags]
    public enum CoreServiceReasonCode
    {
        /// <summary>Default/unset state. No operation has been performed yet.</summary>
        [Description("Undefined")]
        Undefined = 0,

        /// <summary>The operation completed successfully.</summary>
        [Description("Operación exitosa")]
        Succeeded = 1,

        /// <summary>The operation failed.</summary>
        [Description("Error Operacion")]
        Failed = 2,

        /// <summary>The operation was suspended and is awaiting child completions before resuming.</summary>
        [Description("Suspendida")]
        Suspended = 4,

        /// <summary>The requested operation has not been implemented.</summary>
        [Description("Operación no implementada")]
        NotImplemented = 64,

        /// <summary>The service is temporarily unavailable due to maintenance.</summary>
        [Description("Operación en mantenimiento")]
        Maintenance = 65,

        /// <summary>The request failed FluentValidation checks before the service logic ran.</summary>
        [Description("Resultado de validación de request no es es válido")]
        ValidationResultNotValid = 96,

        /// <summary>An unhandled exception occurred during service execution.</summary>
        [Description("Ocurrio una excepción en el sistema")]
        ExceptionHappened = 127
    }
}