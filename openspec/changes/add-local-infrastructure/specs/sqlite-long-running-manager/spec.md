## ADDED Requirements

### Requirement: SQLite Long-Running Service Manager
`SqliteCoreLongRunningServiceManager` SHALL implement `ICoreLongRunningServiceManager` by
persisting batch control records in a `LONG_RUNNING_BATCH` SQLite table. Its schema SHALL be
identical to the MySQL implementation in `BaseLib.Core.MySql`. The table SHALL be created
automatically on first use via `CREATE TABLE IF NOT EXISTS`.

#### Scenario: Parent suspended — persists and resumes when children already done
- **WHEN** `HandleParentSuspendedAsync(coreEvent)` is called and all expected child records are
  already present in the table
- **THEN** a parent row is inserted and `ICoreServiceFireOnly.ResumeAsync` is called immediately

#### Scenario: Parent suspended — waits for children
- **WHEN** `HandleParentSuspendedAsync(coreEvent)` is called and no child records exist yet
- **THEN** a parent row is inserted; `ResumeAsync` is NOT called

#### Scenario: Children finished — resumes parent when count satisfied
- **WHEN** `HandleChildrenFinishedAsync(events)` is called and the total completed children
  equals `CHILDREN_COUNT` for the parent row
- **THEN** `ICoreServiceFireOnly.ResumeAsync` is called for the parent service

#### Scenario: Children finished — does not resume when count not yet satisfied
- **WHEN** `HandleChildrenFinishedAsync(events)` is called and the total completed children is
  less than `CHILDREN_COUNT`
- **THEN** child rows are inserted/updated; `ResumeAsync` is NOT called

#### Scenario: Parent finished — updates row
- **WHEN** `HandleParentFinishedAsync(coreEvent)` is called
- **THEN** the parent row's `SERVICE_STATUS`, `FINISHED_ON`, `SUCCEEDED`, and `REASON_CODE`
  columns are updated

### Requirement: SQLite DI Registration
`AddSqliteInfrastructure` SHALL be an `IServiceCollection` extension method that registers
`SqliteCoreLongRunningServiceManager` as `ICoreLongRunningServiceManager`. It SHALL accept an
`Action<SqliteInfrastructureOptions>` to configure the SQLite connection string.

#### Scenario: All implementations registered
- **WHEN** `AddSqliteInfrastructure(services, o => o.ConnectionString = "...")` is called
- **THEN** `ICoreLongRunningServiceManager` resolves to `SqliteCoreLongRunningServiceManager`
