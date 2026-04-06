# Design: Local and SQLite Infrastructure Packages

## Context
BaseLib currently maps each Core interface to exactly one infrastructure package:

| Interface | Package | Production? |
|---|---|---|
| `ICoreServiceStateStore` | `BaseLib.Core.Containers` (FileSystem) | No — single-replica only |
| `ICoreServiceStateStore` | `BaseLib.Core.AmazonCloud` (S3) | Yes |
| `ICoreLongRunningServiceManager` | `BaseLib.Core.MySql` | Yes |
| `ICoreSecretsVault` | `BaseLib.Core.Containers` (Env vars) | Dev only |

`FileSystemCoreServiceStateStore` and `EnvironmentSecretsVault` are already non-production by
design. Placing them in `BaseLib.Core.Containers` is a category error — Containers is meant to
be the production runtime for containerised deployments, not a bag of dev stubs.

## Package Boundary Rationale

### BaseLib.Core.Local
Owns all implementations that are correct only in a single-process, single-replica context:
- `FileSystemCoreServiceStateStore` — file-per-operationId; races under multiple writers
- Any future in-process stubs (e.g. `InMemoryCoreStatusEventSink`)

The package name signals "not for distributed/production use" without encoding a specific
technology (unlike `.Sqlite`, `.InMemory`, etc.).

### BaseLib.Core.Sqlite
Owns the SQLite-backed `ICoreLongRunningServiceManager`. SQLite is a real database and can be
used in lightweight single-node production scenarios, so it lives in its own package rather than
in `.Local`. It is structurally parallel to `BaseLib.Core.MySql`.

## FileSystemCoreServiceStateStore — Temp Directory Default
The existing constructor takes an explicit `rootDirectory`. After the move:
- `LocalServicesOptions.StateStoreRootDirectory` defaults to `Path.GetTempPath()`
- The constructor signature is unchanged — it still accepts `string rootDirectory`
- DI callers that did not set `StateStoreRootDirectory` automatically use temp

Files written to temp are cleaned up by the OS; this is acceptable for dev workflows.
Callers that need persistence across reboots can override the path.

## SQLite Schema — LONG_RUNNING_BATCH
`SqliteCoreLongRunningServiceManager` replicates the MySQL table schema exactly so the two
implementations are interchangeable:

```sql
CREATE TABLE IF NOT EXISTS LONG_RUNNING_BATCH (
    OPERATION_ID    TEXT    PRIMARY KEY,
    CORRELATION_ID  TEXT    NOT NULL,
    SERVICE_NAME    TEXT    NOT NULL,
    SERVICE_STATUS  INTEGER NOT NULL,
    STARTED_ON      TEXT    NOT NULL,
    FINISHED_ON     TEXT,
    SUCCEEDED       INTEGER,
    REASON_CODE     INTEGER,
    REASON          TEXT,
    CHILDREN_COUNT  INTEGER NOT NULL DEFAULT 0,
    COMPLETED_OK    INTEGER NOT NULL DEFAULT 0,
    COMPLETED_ERR   INTEGER NOT NULL DEFAULT 0,
    LAST_UPDATED    TEXT    NOT NULL
);
```

SQLite stores datetimes as TEXT in ISO 8601 format — same as the MySQL implementation which
already formats dates as `yyyy-MM-dd HH:mm:ss.fff` strings.

The `INSERT OR IGNORE` idiom replaces MySQL's `INSERT IGNORE` for idempotent child inserts.

## Breaking Change in BaseLib.Core.Containers
Removing `StateStoreRootDirectory` and the `ICoreServiceStateStore` registration from
`AddContainerServices` is a breaking change in the public API surface. The mitigation is:
- Version bump makes the break explicit (`3.2.1 → 3.2.2`)
- The fix for callers is simple: add `AddLocalServices()` alongside `AddContainerServices()`
- No runtime behaviour changes — `FileSystemCoreServiceStateStore` itself is unchanged

`EnvironmentSecretsVault` stays in `BaseLib.Core.Containers` for now; moving it is a separate
concern and would add a second breaking change to this PR.

## DI Extension Design

### AddLocalServices
```csharp
services.AddLocalServices();               // uses Path.GetTempPath()
services.AddLocalServices(o => o.StateStoreRootDirectory = "/custom/path");
```

### AddSqliteInfrastructure
```csharp
services.AddSqliteInfrastructure(o => o.ConnectionString = "Data Source=baselib.db");
```
The extension creates the `LONG_RUNNING_BATCH` table on first use (idempotent `CREATE TABLE IF
NOT EXISTS`), so no separate migration step is required.
