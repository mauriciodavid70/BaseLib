namespace BaseLib.Core.Services.RabbitMQ
{
    /// <summary>
    /// Configuration options for the RabbitMQ transport integration.
    /// Controls the exchange names used for service dispatch and domain event publishing.
    /// </summary>
    public class RabbitMqOptions
    {
        /// <summary>
        /// Name of the durable topic exchange used for service dispatch messages.
        /// Defaults to <c>"baselib.services"</c>.
        /// </summary>
        public string ExchangeName { get; set; } = "baselib.services";

        /// <summary>
        /// Name of the durable topic exchange used for publishing domain status events.
        /// Defaults to <c>"baselib.events"</c>.
        /// </summary>
        public string EventExchangeName { get; set; } = "baselib.events";
    }
}
