using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Stores event blob and write a record entry in the journal
    /// </summary>
    public class JournalEventHandler : IJournalEventHandler
    {
        private readonly IJournalEntryWriter journalWriter;
        private readonly ICoreStatusEventStore eventStore;

        /// <param name="journalWriter">Writer that persists journal entries to the relational store.</param>
        /// <param name="eventStore">Store that persists the full event payload for auditing.</param>
        public JournalEventHandler(IJournalEntryWriter journalWriter, ICoreStatusEventStore eventStore)
        {
            this.journalWriter = journalWriter;
            this.eventStore = eventStore;
        }

        public async Task<int> HandleAsync(CoreStatusEvent statusEvent)
        {
            if (statusEvent.Status == CoreServiceStatus.Finished)
            {
                var journalEntry = MapJournalEntry(statusEvent);

                await journalWriter.WriteAsync(journalEntry);

                await eventStore.WriteAsync(statusEvent);

                return 1;
            }
            return 0;
        }

        private JournalEntry MapJournalEntry(CoreStatusEvent statusEvent)
        {
            return new JournalEntry
            {
                ServiceName = statusEvent.ServiceName,
                Status = statusEvent.Status,
                StartedOn = statusEvent.StartedOn,
                FinishedOn = statusEvent.FinishedOn,
                OperationId = statusEvent.OperationId,
                CorrelationId = statusEvent.CorrelationId,
                Succeeded = statusEvent.Response?.Succeeded ?? false,
                ReasonCode = statusEvent.Response?.ReasonCode ?? CoreReasonCode.Null,
                Messages = statusEvent.Response?.Messages ?? Array.Empty<string>(),
                IsLongRunning = statusEvent.IsLongRunningService,
                IsLongRunningChild = statusEvent.IsLongRunningChild               
            };

        }

        
    }
}