using BaseLib.Core.Models;
using MySql.Data.MySqlClient;

namespace BaseLib.Core.Services.MySql
{
    /// <summary>
    /// A basic journal writer implementation for Mysql 
    /// </summary>
    public class JournalEntryWriter : IJournalEntryWriter
    {
        private readonly MySqlConnection connection;
        private const string insertSql = @"
            INSERT INTO JOURNAL(
                OPERATION_ID, 
                CORRELATION_ID, 
                SERVICE_NAME,
                STARTED_ON,
                FINISHED_ON,
                SUCCEEDED,
                REASON_CODE,
                REASON,
                MESSAGES
            )
            VALUES(
                @OPERATION_ID,
                @CORRELATION_ID,
                @SERVICE_NAME,
                @STARTED_ON,
                @FINISHED_ON, 
                @SUCCEEDED,
                @REASON_CODE,
                @REASON,
                @MESSAGES
            );
        ";

        private const string updateSql = @"
            UPDATE 
                JOURNAL
            SET
                FINISHED_ON = @FINISHED_ON,
                SUCCEEDED = @SUCCEEDED,
                REASON_CODE = @REASON_CODE,
                REASON = @REASON,
                MESSAGES = @MESSAGES
            WHERE
                OPERATION_ID = @OPERATION_ID;
        ";
        /// <summary>Initializes the writer with an open MySQL connection.</summary>
        /// <param name="connection">An open <see cref="MySqlConnection"/> used for all write operations.</param>
        public JournalEntryWriter(MySqlConnection connection)
        {
            this.connection = connection;
        }

        /// <inheritdoc/>
        public async Task<int> WriteAsync(JournalEntry entry)
        {
            if (entry.Status == CoreServiceStatus.Started)
            {
                //just insert
                return await InsertAsync(entry);
            }
            else
            {
                //update other wise insert
                var rowsAffected = await UpdateAsync(entry);
                if (rowsAffected == 0)
                {
                    return await InsertAsync(entry);

                }
                return rowsAffected;
            }
        }

        /// <summary>
        /// Insert the entry, if already exists returns 0
        /// </summary>
        private Task<int> InsertAsync(JournalEntry entry)
        {
            try
            {
                using (var command = new MySqlCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("CORRELATION_ID", entry.CorrelationId);
                    command.Parameters.AddWithValue("OPERATION_ID", entry.OperationId);
                    command.Parameters.AddWithValue("SERVICE_NAME", entry.ServiceName);
                    command.Parameters.AddWithValue("STARTED_ON", entry.StartedOn);
                    command.Parameters.AddWithValue("FINISHED_ON", entry.FinishedOn);
                    command.Parameters.AddWithValue("SUCCEEDED", entry.Succeeded);
                    command.Parameters.AddWithValue("REASON_CODE", entry.ReasonCode.Value);
                    command.Parameters.AddWithValue("REASON", entry.ReasonCode.Description);
                    command.Parameters.AddWithValue("MESSAGES", string.Join("\r\n", entry.Messages));

                    return command.ExecuteNonQueryAsync();
                }
            }
            catch (MySqlException ex)
            {
                if (ex.IsDuplicate()) return Task.FromResult(0);
                throw;
            }
        }

        /// <summary>
        /// Updates the entry
        /// </summary>
        private Task<int> UpdateAsync(JournalEntry entry)
        {
            using (var command = new MySqlCommand(updateSql, connection))
            {
                command.Parameters.AddWithValue("OPERATION_ID", entry.OperationId);
                command.Parameters.AddWithValue("FINISHED_ON", entry.FinishedOn.ToString("yyyy-MM-dd hh:mm:ss.fff"));
                command.Parameters.AddWithValue("SUCCEEDED", entry.Succeeded);
                command.Parameters.AddWithValue("REASON_CODE", entry.ReasonCode.Value);
                command.Parameters.AddWithValue("REASON", entry.ReasonCode.Description);
                command.Parameters.AddWithValue("MESSAGES", string.Join("\r\n", entry.Messages));

                return command.ExecuteNonQueryAsync();
            }
        }


    }
}