# Change: Local and SQLite Infrastructure Packages

## Why
`FileSystemCoreServiceStateStore` currently lives in `BaseLib.Core.Containers` — the production
container-runtime package. Dev/fallback implementations do not belong there. A new
`BaseLib.Core.Local` package gives all local-only implementations a clearly non-production home.

Additionally, `ICoreLongRunningServiceManager` has only a MySQL implementation. For local
development and the upcoming ERP reference sample, a SQLite-backed manager is needed that runs
without a database server. A new `BaseLib.Core.Sqlite` package provides this.

## What Changes

### BaseLib.Core.Local (new package)
- `FileSystemCoreServiceStateStore : ICoreServiceStateStore` — moved from `BaseLib.Core.Containers`;
  default root directory is `Path.GetTempPath()`; caller may override via `LocalServicesOptions`
- `LocalServicesExtensions.AddLocalServices(IServiceCollection, Action<LocalServicesOptions>?)` DI
  extension registering `ICoreServiceStateStore`

### BaseLib.Core.Sqlite (new package)
- `SqliteCoreLongRunningServiceManager : ICoreLongRunningServiceManager` — same `LONG_RUNNING_BATCH`
  table schema as the MySQL implementation; uses `Microsoft.Data.Sqlite`
- `SqliteInfrastructureExtensions.AddSqliteInfrastructure(IServiceCollection, Action<SqliteInfrastructureOptions>)`
  DI extension registering `ICoreLongRunningServiceManager`

### BaseLib.Core.Containers (modified — **breaking**)
- `FileSystemCoreServiceStateStore.cs` removed (class moved to `BaseLib.Core.Local`)
- `StateStoreRootDirectory` removed from `ContainerServicesOptions`
- `ICoreServiceStateStore` registration removed from `AddContainerServices`

## Impact
- Affected specs: `local-state-store` (new), `sqlite-long-running-manager` (new),
  `containers-runtime` (MODIFIED — removes state-store requirement and DI registration for it)
- New projects: `BaseLib.Core.Local/`, `BaseLib.Core.Sqlite/`
- **Breaking change** in `BaseLib.Core.Containers` public API — callers that used
  `StateStoreRootDirectory` must switch to `AddLocalServices`
- Version bump: **patch** `3.2.1 → 3.2.2` (breaking within a pre-1.0 library line; additive net effect)
- Depends on: no new cross-package dependencies (both new packages reference `BaseLib.Core` only)
