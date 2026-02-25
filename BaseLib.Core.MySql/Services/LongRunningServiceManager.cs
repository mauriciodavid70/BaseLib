using System.Data;
using System.Data.Common;
using System.Text;
using BaseLib.Core.Models;
using MySql.Data.MySqlClient;

namespace BaseLib.Core.Services.MySql
{

    public class LongRunningServiceManager : ICoreLongRunningServiceManager
    {
        private readonly Func<MySqlConnection> connectionFactory;
        private readonly ICoreServiceFireOnly invoker;
        private const string insertSql = @"
            INSERT INTO LONG_RUNNING_BATCH (
                OPERATION_ID,
                CORRELATION_ID,
                SERVICE_NAME,
                SERVICE_STATUS,
                STARTED_ON,
                FINISHED_ON,
                SUCCEEDED,
                REASON_CODE,
                REASON,
                CHILDREN_COUNT,
                COMPLETED_OK,
                COMPLETED_ERR,
                LAST_UPDATED
            ) VALUES (
                @OPERATION_ID,
                @CORRELATION_ID,
                @SERVICE_NAME,
                @SERVICE_STATUS,
                @STARTED_ON,
                @FINISHED_ON,
                @SUCCEEDED,
                @REASON_CODE,
                @REASON,
                @CHILDREN_COUNT,
                @COMPLETED_OK,
                @COMPLETED_ERR,
                @LAST_UPDATED
            );
        ";

        private const string readBatchSummarySql = @"
            SELECT
                LRB.SERVICE_NAME,
                LRB.OPERATION_ID,
                LRB.CORRELATION_ID,
                LRB.CHILDREN_COUNT,
                T.*
            FROM
                (
                    SELECT
                        COUNT(SUCCEEDED) as COMPLETED,
                        SUM(CASE WHEN SUCCEEDED = 1 THEN 1 ELSE 0 END) AS COMPLETED_OK,
                        SUM(CASE WHEN SUCCEEDED = 0 THEN 1 ELSE 0 END) AS COMPLETED_ERR,
                        MAX(FINISHED_ON) AS LAST_FINISHED_ON
                    FROM
                        LONG_RUNNING_BATCH
                    WHERE
                        CORRELATION_ID = @CORRELATION_ID
                ) T
                LEFT JOIN LONG_RUNNING_BATCH LRB ON (LRB.OPERATION_ID=@CORRELATION_ID)";

        private const string beginOfInsertManySql = @"
            INSERT IGNORE INTO LONG_RUNNING_BATCH (
                OPERATION_ID,
                CORRELATION_ID,
                SERVICE_NAME,
                SERVICE_STATUS,
                STARTED_ON,
                FINISHED_ON,
                SUCCEEDED,
                REASON_CODE,
                REASON,
                CHILDREN_COUNT,
                COMPLETED_OK,
                COMPLETED_ERR,
                LAST_UPDATED
            ) VALUES";

        private const string updateCountersSql = @"
            UPDATE
                LONG_RUNNING_BATCH LRB
                JOIN (
                    SELECT
                        SUM(CASE WHEN SUCCEEDED = 1 THEN 1 ELSE 0 END) AS COMPLETED_OK,
                        SUM(CASE WHEN SUCCEEDED = 0 THEN 1 ELSE 0 END) AS COMPLETED_ERR
                    FROM
                        LONG_RUNNING_BATCH
                    WHERE
                        CORRELATION_ID = @CORRELATION_ID
                ) T
            SET
                LRB.COMPLETED_OK=T.COMPLETED_OK,
                LRB.COMPLETED_ERR=T.COMPLETED_ERR
            WHERE LRB.OPERATION_ID=@CORRELATION_ID;


        ";

        private const string updateFinishedBatchSql = @"
            UPDATE
                LONG_RUNNING_BATCH
            SET
                SERVICE_STATUS = @SERVICE_STATUS,
                FINISHED_ON = @FINISHED_ON,
                SUCCEEDED = @SUCCEEDED,
                REASON_CODE = @REASON_CODE,
                REASON = @REASON,
                LAST_UPDATED = @LAST_UPDATED
            WHERE
                OPERATION_ID = @OPERATION_ID
        ";

        public LongRunningServiceManager(Func<MySqlConnection> connectionFactory, ICoreServiceFireOnly invoker)
        {
            this.connectionFactory = connectionFactory;
            this.invoker = invoker;
        }

        /// <summary>
        /// Handles the suspension of a long-running service,
        /// triggers <see cref="ICoreServiceFireOnly.ResumeAsync"/> for the parent service when all child services are completed.
        /// </summary>
        public async Task HandleParentSuspendedAsync(CoreStatusEvent coreEvent)
        {
            if (coreEvent.Status != CoreServiceStatus.Suspended)
                throw new ArgumentException("The event status must be Suspended.", nameof(coreEvent));

            //maps the event to BatchRecord
            var batchRecord = new BatchRecord
            {
                OperationId = coreEvent.OperationId!,
                CorrelationId = coreEvent.CorrelationId!,
                ServiceName = coreEvent.TypeName!,
                Status = coreEvent.Status,
                StartedOn = coreEvent.StartedOn,
                ChildrenCount = coreEvent.ChildrenCount
            };

            //revisamos si hay registros detalle completados debe retornar una estructura con SucceededCount, FailedCount, Count
            var batchSummary = await ReadBatchSummaryAsync(coreEvent.OperationId);

            if (batchSummary.Completed > 0)
            {
                batchRecord.CompletedOk = batchSummary.SucceededCount;
                batchRecord.CompletedErr = batchSummary.FailedCount;
            }

            await InsertAsync(batchRecord);

            // when all children are finished invoke resume on parent service
            if (batchSummary.Completed >= batchRecord.ChildrenCount)
            {
                var typeName = coreEvent.TypeName!;
                await invoker.ResumeAsync(typeName, coreEvent.OperationId!, coreEvent.CorrelationId);
            }

        }

        public async Task HandleParentFinishedAsync(CoreStatusEvent coreEvent)
        {
            await this.UpdateFinishedBatchAsync(coreEvent);
        }

        public async Task HandleChildrenFinishedAsync(CoreStatusEvent[] coreEvent)
        {
            if (coreEvent == null || coreEvent.Length == 0)
                return;

            // Maps the events to BatchRecords
            var batchRecords = coreEvent.Select(e => new BatchRecord
            {
                OperationId = e.OperationId!,
                CorrelationId = e.CorrelationId!,
                ServiceName = e.TypeName ?? string.Empty,
                Status = e.Status,
                StartedOn = e.StartedOn,
                FinishedOn = e.FinishedOn,
                Succeeded = e.Response?.Succeeded,
                ReasonCode = e.Response?.ReasonCode,
                ChildrenCount = 0, // This is a child record, so it has no children count
                CompletedOk = 0, // Not applicable for child records
                CompletedErr = 0 // Not applicable for child records
            }).ToArray();

            // insert child records into the database
            await InsertManyAsync(batchRecords);

            //revisamos si hay registros detalle completados debe retornar una estructura con SucceededCount, FailedCount, Count
            var batchSummary = await ReadBatchSummaryAsync(coreEvent[0].CorrelationId);

            // when all children are finished invoke resume on parent service
            if (batchSummary.ChildrenCount > 0 && batchSummary.Completed >= batchSummary.ChildrenCount)
            {
                var typeName = batchSummary.ServiceName!;
                await invoker.ResumeAsync(typeName, batchSummary.OperationId!, batchSummary.CorrelationId);
            }

        }

        private async Task<BatchSummary> ReadBatchSummaryAsync(string? correlationId)
        {
            using (var connection = connectionFactory.Invoke())
            using (var command = new MySqlCommand(readBatchSummarySql, connection))
            {
                command.Parameters.AddWithValue("@CORRELATION_ID", correlationId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new BatchSummary(
                            reader.GetStringEx("SERVICE_NAME"),
                            reader.GetStringEx("OPERATION_ID"),
                            reader.GetStringEx("CORRELATION_ID"),
                            reader.GetInt32Ex("CHILDREN_COUNT"),
                            reader.GetInt32Ex("COMPLETED"),
                            reader.GetInt32Ex("COMPLETED_OK"),
                            reader.GetInt32Ex("COMPLETED_ERR"),
                            reader.GetDateTimeEx("LAST_FINISHED_ON")
                        );
                    }
                    return new BatchSummary(null, null, null, 0, 0, 0, 0, DateTime.MinValue);
                }
            }
        }

        private async Task<int> InsertAsync(BatchRecord batchRecord)
        {
            using (var connection = connectionFactory.Invoke())
            using (var command = new MySqlCommand(insertSql, connection))
            {
                command.Parameters.AddWithValue("@OPERATION_ID", batchRecord.OperationId);
                command.Parameters.AddWithValue("@CORRELATION_ID", batchRecord.CorrelationId);
                command.Parameters.AddWithValue("@SERVICE_NAME", batchRecord.ServiceName);
                command.Parameters.AddWithValue("@SERVICE_STATUS", (int)batchRecord.Status);
                command.Parameters.AddWithValue("@STARTED_ON", batchRecord.StartedOn.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@FINISHED_ON", batchRecord.FinishedOn?.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@SUCCEEDED", batchRecord.Succeeded);
                command.Parameters.AddWithValue("@REASON_CODE", batchRecord.ReasonCode?.Value);
                command.Parameters.AddWithValue("@REASON", batchRecord.ReasonCode?.Description);
                command.Parameters.AddWithValue("@CHILDREN_COUNT", batchRecord.ChildrenCount);
                command.Parameters.AddWithValue("@COMPLETED_OK", batchRecord.CompletedOk);
                command.Parameters.AddWithValue("@COMPLETED_ERR", batchRecord.CompletedErr);
                command.Parameters.AddWithValue("@LAST_UPDATED", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                return await command.ExecuteNonQueryAsync();
            }
        }

        private async Task<int> InsertManyAsync(BatchRecord[] childRecords)
        {
            if (childRecords.Length == 0)
                return 0;

            using (var connection = connectionFactory.Invoke())
            {
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sql = new StringBuilder();
                        const int batchSize = 128;
                        int totalRowsAffected = 0;

                        // Process in batches to avoid exceeding MySQL's max_allowed_packet
                        for (int batchStart = 0; batchStart < childRecords.Length; batchStart += batchSize)
                        {
                            sql.Clear();
                            sql.Append(beginOfInsertManySql);

                            int currentBatchSize = Math.Min(batchSize, childRecords.Length - batchStart);

                            for (int i = 0; i < currentBatchSize; i++)
                            {
                                var record = childRecords[batchStart + i];

                                if (i > 0)
                                    sql.Append(",\n");

                                sql.Append('(');
                                sql.Append($"'{record.OperationId}', ");
                                sql.Append($"'{record.CorrelationId}', ");
                                sql.Append($"'{record.ServiceName}', ");
                                sql.Append($"{(int)record.Status}, ");
                                sql.Append($"'{record.StartedOn:yyyy-MM-dd HH:mm:ss.fff}', ");
                                sql.Append(record.FinishedOn == null
                                    ? "NULL, "
                                    : $"'{record.FinishedOn:yyyy-MM-dd HH:mm:ss.fff}', ");
                                sql.Append($"{((record.Succeeded ?? false) ? 1 : 0)}, ");
                                sql.Append($"{(record.ReasonCode is not null ? record.ReasonCode.Value : 0)}, ");
                                sql.Append($"'{(record.ReasonCode is not null ? record.ReasonCode.Description : string.Empty)}', ");
                                sql.Append($"NULL, ");
                                sql.Append($"NULL, ");
                                sql.Append($"NULL, ");
                                sql.Append($"'{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}'");
                                sql.Append(')');
                            }

                            using (var command = new MySqlCommand(sql.ToString(), connection, transaction))
                            {
                                int rowsAffected = await command.ExecuteNonQueryAsync();
                                totalRowsAffected += rowsAffected;
                            }
                        }

                        //update batch counters
                        using (var command = new MySqlCommand(updateCountersSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@CORRELATION_ID", childRecords[0].CorrelationId);
                            await command.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();

                        return totalRowsAffected;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        private async Task<int> UpdateFinishedBatchAsync(CoreStatusEvent coreEvent)
        {
            using (var connection = connectionFactory.Invoke())
            using (var command = new MySqlCommand(updateFinishedBatchSql, connection))
            {
                command.Parameters.AddWithValue("@OPERATION_ID", coreEvent.OperationId!);
                command.Parameters.AddWithValue("@SERVICE_STATUS", (int)coreEvent.Status);
                command.Parameters.AddWithValue("@FINISHED_ON", coreEvent.FinishedOn.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@SUCCEEDED", coreEvent.Response!.Succeeded);
                command.Parameters.AddWithValue("@REASON_CODE", coreEvent.Response!.ReasonCode?.Value);
                command.Parameters.AddWithValue("@REASON", coreEvent.Response?.ReasonCode!.Description);
                command.Parameters.AddWithValue("@LAST_UPDATED", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                return await command.ExecuteNonQueryAsync();
            }
        }



        private record BatchSummary(string? ServiceName, string? OperationId, string? CorrelationId, int? ChildrenCount, int Completed, int SucceededCount, int FailedCount, DateTime LastFinishedOn);

        /// <summary>
        /// Represents the batch control record.
        /// </summary>
        private struct BatchRecord
        {
            public string OperationId { get; set; }
            public string CorrelationId { get; set; }
            public string ServiceName { get; set; }
            public CoreServiceStatus Status { get; set; }
            public DateTimeOffset StartedOn { get; set; }
            public DateTimeOffset? FinishedOn { get; set; }
            public bool? Succeeded { get; set; }
            public CoreReasonCode? ReasonCode { get; set; }

            // Additional properties for master records:
            public int ChildrenCount { get; set; }
            public int CompletedOk { get; set; }
            public int CompletedErr { get; set; }
        }
    }



}