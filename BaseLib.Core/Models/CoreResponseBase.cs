namespace BaseLib.Core.Models
{
    /// <summary>
    /// Base class for all service response objects. Derive from this class to define the
    /// output contract for a <c>CoreServiceBase&lt;TRequest, TResponse&gt;</c> implementation.
    /// </summary>
    public abstract class CoreResponseBase
    {
        /// <summary>Unique identifier assigned to this specific service execution.</summary>
        public string? OperationId { get; set; }
        /// <summary><see langword="true"/> if the service completed without errors; otherwise <see langword="false"/>.</summary>
        public bool Succeeded { get; set; }
        /// <summary>Structured reason code describing the outcome. Defaults to <see cref="CoreReasonCode.Null"/> (Undefined).</summary>
        public CoreReasonCode ReasonCode { get; set; } = CoreReasonCode.Null;
        /// <summary>Optional informational or error messages produced during execution.</summary>
        public string[] Messages { get; set; } = [];
    }
}
