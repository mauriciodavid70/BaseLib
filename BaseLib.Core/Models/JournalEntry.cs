namespace BaseLib.Core.Models
{
    /// <summary>
    /// Audit record written to the JOURNAL table after each service execution.
    /// Contains metadata only — request/response payloads should be stored separately.
    /// </summary>
    public class JournalEntry
    {
        /// <summary>Short class name of the service that produced this entry.</summary>
        public string? ServiceName { get; set; }
        /// <summary>Lifecycle status at the time of writing.</summary>
        public CoreServiceStatus Status { get; set; }
        /// <summary>UTC timestamp when the service started.</summary>
        public DateTimeOffset StartedOn { get; set; }
        /// <summary>UTC timestamp when the service finished.</summary>
        public DateTimeOffset FinishedOn { get; set; }
        /// <summary>Unique operation identifier assigned by the framework.</summary>
        public string? OperationId { get; set; }
        /// <summary>Correlation identifier linking related operations.</summary>
        public string? CorrelationId { get; set; }
        /// <summary><see langword="true"/> if the service completed successfully.</summary>
        public bool Succeeded { get; set; }
        /// <summary>Structured outcome code. Defaults to <see cref="CoreReasonCode.Null"/>.</summary>
        public CoreReasonCode ReasonCode { get; set; } = CoreReasonCode.Null;
        /// <summary>Informational or error messages produced during execution.</summary>
        public string[] Messages { get; set; } = Array.Empty<string>();
        /// <summary><see langword="true"/> when the service is a long-running parent.</summary>
        public bool IsLongRunning { get; set; }
        /// <summary><see langword="true"/> when the service is a child spawned by a long-running parent.</summary>
        public bool IsLongRunningChild { get; set; }
    }





}