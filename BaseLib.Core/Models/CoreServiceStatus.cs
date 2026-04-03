namespace BaseLib.Core.Models
{
    /// <summary>Lifecycle states of a service execution.</summary>
    public enum CoreServiceStatus
    {
        /// <summary>The service has begun processing the request.</summary>
        Started,
        /// <summary>The service has completed (successfully or with an error).</summary>
        Finished,
        /// <summary>A long-running service is suspended while awaiting child completions.</summary>
        Suspended,
        /// <summary>A previously suspended long-running service has been resumed.</summary>
        Resumed
    }
}

