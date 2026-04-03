namespace BaseLib.Core.Models
{
    /// <summary>
    /// Snapshot of a service execution published to <see cref="Services.ICoreStatusEventSink"/> at
    /// the start and end (and on suspension) of each service run. Consumed by event processors to
    /// drive journaling, long-running service coordination, and downstream choreography.
    /// </summary>
    public class CoreStatusEvent
    {
        /// <summary>Assembly-qualified type name of the service (e.g. <c>MyApp.CheckoutService, MyApp</c>).</summary>
        public string? TypeName { get; set; }
        /// <summary>Assembly name of the module that owns the service.</summary>
        public string? ModuleName { get; set; }
        /// <summary>Short class name of the service.</summary>
        public string? ServiceName { get; set; }
        /// <summary>Current lifecycle status of the service execution.</summary>
        public CoreServiceStatus Status { get; set; }
        /// <summary>UTC timestamp when the service started.</summary>
        public DateTimeOffset StartedOn { get; set; }
        /// <summary>UTC timestamp when the service finished or was suspended.</summary>
        public DateTimeOffset FinishedOn { get; set; }
        /// <summary>Unique identifier for this execution, assigned by the framework.</summary>
        public string? OperationId { get; set; }
        /// <summary>Correlation identifier linking related operations (e.g. parent/child in long-running flows).</summary>
        public string? CorrelationId { get; set; }
        /// <summary>The request payload that triggered this execution.</summary>
        public CoreRequestBase? Request { get; set; }
        /// <summary>The response produced by the service, or <see langword="null"/> if not yet finished.</summary>
        public CoreResponseBase? Response { get; set; }
        /// <summary><see langword="true"/> when this event is from a long-running parent service.</summary>
        public bool IsLongRunningService { get; set; }
        /// <summary>Number of child services spawned by a long-running parent.</summary>
        public int ChildrenCount { get; set; }
        /// <summary><see langword="true"/> when this event is from a child spawned by a long-running parent.</summary>
        public bool IsLongRunningChild { get; set; }
    }
}

