using System.ComponentModel;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Built-in reason codes set by the framework on <see cref="CoreResponseBase.ReasonCode"/>
    /// when a service completes. Domain-specific enums can be used alongside these values.
    /// Declared as <see cref="FlagsAttribute"/> so that <c>Suspended</c> can be OR-ed with other codes.
    /// </summary>
    [Flags]
    public enum CoreServiceReasonCode
    {
        [Description("Undefined")]
        Undefined = 0,

        [Description("Operación exitosa")]
        Succeeded = 1,

        [Description("Error Operacion")]
        Failed = 2,

        [Description("Suspendida")]
        Suspended = 4,

        [Description("Operación no implementada")]
        NotImplemented = 64,

        [Description("Operación en mantenimiento")]
        Maintenance = 65,

        [Description("Resultado de validación de request no es es válido")]
        ValidationResultNotValid = 96,

        [Description("Ocurrio una excepción en el sistema")]
        ExceptionHappened = 127
    }
}