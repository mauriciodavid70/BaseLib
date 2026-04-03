# BaseLib.Core.MySql

## Overview

Contains concrete implementations of the interfaces from **BaseLib.Core** for MySQL.

## Services

### JournalEntryWriter

`JournalEntryWriter` implements `IJournalEntryWriter` to persist service execution records in a relational `JOURNAL` table.

On each service run, two writes occur:
1. **Started** — an `INSERT` when the service begins.
2. **Finished** — an `UPDATE` when the service completes (falls back to `INSERT` if the started record is missing).

> **Best practice**: Store only the `JournalEntry` (metadata) in the relational database. Request and response payloads should be stored in a secure object store (e.g. S3).

#### JOURNAL table schema

Run `dbinstall-v1.0.0.sql` (included in the source) to create the table:

```sql
CREATE TABLE IF NOT EXISTS JOURNAL (
  ID int NOT NULL AUTO_INCREMENT,
  SERVICE_NAME VARCHAR(255) DEFAULT NULL,
  STARTED_ON TIMESTAMP(6) DEFAULT NULL,
  FINISHED_ON TIMESTAMP(6) DEFAULT NULL,
  OPERATION_ID VARCHAR(255) DEFAULT NULL,
  CORRELATION_ID VARCHAR(255) DEFAULT NULL,
  SUCCEEDED BOOLEAN DEFAULT NULL,
  REASON_CODE INT DEFAULT NULL,
  REASON VARCHAR(255),
  MESSAGES TEXT,
  PRIMARY KEY (ID),
  UNIQUE KEY IX_OPERATION (OPERATION_ID),
  KEY IX_CORRELATION (CORRELATION_ID),
  KEY IX_SERVICE_DATES (STARTED_ON,SERVICE_NAME)
)
```

#### Configuration

```csharp
// Register per-request (transient) since MySqlConnection is not thread-safe
services.AddTransient<IJournalEntryWriter>(sp =>
{
    var connection = new MySqlConnection(connectionString);
    connection.Open();
    return new JournalEntryWriter(connection);
});
```

---

### LongRunningServiceManager

`LongRunningServiceManager` implements `ICoreLongRunningServiceManager` using a MySQL `LONG_RUNNING_BATCH` table to track parent/child service relationships.

It reacts to three lifecycle events:

| Event | Action |
|---|---|
| `HandleParentSuspendedAsync` | Inserts a parent batch record; resumes immediately if all children already finished |
| `HandleChildrenFinishedAsync` | Bulk-inserts child records and updates counters; resumes the parent when all children are done |
| `HandleParentFinishedAsync` | Updates the parent batch record with the final status |

#### LONG_RUNNING_BATCH table schema

```sql
CREATE TABLE IF NOT EXISTS LONG_RUNNING_BATCH (
  ID int NOT NULL AUTO_INCREMENT,
  OPERATION_ID VARCHAR(255) NOT NULL,
  CORRELATION_ID VARCHAR(255) DEFAULT NULL,
  SERVICE_NAME VARCHAR(500) DEFAULT NULL,
  SERVICE_STATUS INT DEFAULT NULL,
  STARTED_ON TIMESTAMP(6) DEFAULT NULL,
  FINISHED_ON TIMESTAMP(6) DEFAULT NULL,
  SUCCEEDED BOOLEAN DEFAULT NULL,
  REASON_CODE INT DEFAULT NULL,
  REASON VARCHAR(255) DEFAULT NULL,
  CHILDREN_COUNT INT DEFAULT NULL,
  COMPLETED_OK INT DEFAULT NULL,
  COMPLETED_ERR INT DEFAULT NULL,
  LAST_UPDATED TIMESTAMP(6) DEFAULT NULL,
  PRIMARY KEY (ID),
  UNIQUE KEY IX_OPERATION (OPERATION_ID),
  KEY IX_CORRELATION (CORRELATION_ID)
)
```

#### Configuration

```csharp
services.AddSingleton<ICoreLongRunningServiceManager>(sp =>
    new LongRunningServiceManager(
        connectionFactory: () =>
        {
            var conn = new MySqlConnection(connectionString);
            conn.Open();
            return conn;
        },
        invoker: sp.GetRequiredService<ICoreServiceFireOnly>()
    ));
```

---

## Extensions

### `MySqlConnection` extensions

| Method | Description |
|---|---|
| `SetWaitTimeout(int timeout)` | Sets the MySQL session `wait_timeout` in seconds. Useful in long-lived connection pools to prevent silent disconnects. |
| `GetWaitTimeout()` | Returns the current session `wait_timeout` value. |

```csharp
using (var conn = new MySqlConnection(connectionString))
{
    conn.Open();
    conn.SetWaitTimeout(3600); // keep connection alive for 1 hour
}
```

### `MySqlException` extensions

| Method | Returns | Description |
|---|---|---|
| `IsTransient()` | `bool` | `true` for transient errors (stream read failures, timeouts, deadlocks, connection failures) that are safe to retry. |
| `IsDuplicate()` | `bool` | `true` for duplicate key violations (safe to swallow on upsert patterns). |

```csharp
catch (MySqlException ex) when (ex.IsTransient())
{
    // retry logic
}
```

---

## Troubleshooting

### "Reading from the stream has failed" / connection drops

The MySQL server closed the connection while it was idle in the pool. Use `SetWaitTimeout` to align the session timeout with your application's connection pool idle timeout, or configure keep-alive pings in the connection string:

```
Server=...;Connection Timeout=30;Default Command Timeout=60;Connection Reset=true;
```

### Duplicate key on JOURNAL insert

`JournalEntryWriter` silently ignores duplicate-key exceptions on insert (`IsDuplicate()` returns `true`). This is expected when a `Started` event arrives after the `Finished` event due to out-of-order delivery.

### "max_allowed_packet" errors on bulk child insert

`LongRunningServiceManager` batches child records in groups of 128 rows. If you still hit packet-size limits, reduce the batch by lowering the constant or increase `max_allowed_packet` in your MySQL server config.
