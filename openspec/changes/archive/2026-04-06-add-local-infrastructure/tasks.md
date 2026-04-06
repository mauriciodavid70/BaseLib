## 1. BaseLib.Core.Local — New Project
- [x] 1.1 Create `BaseLib.Core.Local/BaseLib.Core.Local.csproj` — references `BaseLib.Core`; added to `BaseLib.sln`
- [x] 1.2 Move `FileSystemCoreServiceStateStore.cs` from `BaseLib.Core.Containers/` to `BaseLib.Core.Local/`; update namespace to `BaseLib.Core.Local`
- [x] 1.3 Add `LocalServicesOptions` — `StateStoreRootDirectory` defaults to `Path.GetTempPath()`
- [x] 1.4 Add `LocalServicesExtensions.AddLocalServices(IServiceCollection, Action<LocalServicesOptions>?)` — registers `ICoreServiceStateStore` as singleton
- [x] 1.5 Add XML doc comments to all public types and members (CS1591 enforcement)

## 2. BaseLib.Core.Containers — Cleanup (breaking)
- [x] 2.1 Delete `FileSystemCoreServiceStateStore.cs` from `BaseLib.Core.Containers/` (moved to Local)
- [x] 2.2 Remove `StateStoreRootDirectory` from `ContainerServicesOptions`
- [x] 2.3 Remove `ICoreServiceStateStore` singleton registration from `AddContainerServices`
- [x] 2.4 Update `ContainerServicesExtensions` XML doc to remove reference to `FileSystemCoreServiceStateStore`

## 3. BaseLib.Core.Sqlite — New Project
- [x] 3.1 Create `BaseLib.Core.Sqlite/BaseLib.Core.Sqlite.csproj` — references `BaseLib.Core`; NuGet dep `Microsoft.Data.Sqlite 8.x`; added to `BaseLib.sln`
- [x] 3.2 Add `SqliteInfrastructureOptions` — `ConnectionString` property
- [x] 3.3 Implement `SqliteCoreLongRunningServiceManager : ICoreLongRunningServiceManager`
  - `HandleParentSuspendedAsync` — insert parent row; call `ResumeAsync` if all children already done
  - `HandleChildrenFinishedAsync` — `INSERT OR IGNORE` child rows; update counters; call `ResumeAsync` when count satisfied
  - `HandleParentFinishedAsync` — update parent row status/outcome columns
  - Table creation: `CREATE TABLE IF NOT EXISTS LONG_RUNNING_BATCH (...)` called in constructor
- [x] 3.4 Add `SqliteInfrastructureExtensions.AddSqliteInfrastructure(IServiceCollection, Action<SqliteInfrastructureOptions>)` — registers `ICoreLongRunningServiceManager` as singleton
- [x] 3.5 Add XML doc comments to all public types and members (CS1591 enforcement)

## 4. Tests (BaseLib.Core.Tests)
- [x] 4.1 Unit tests for `FileSystemCoreServiceStateStore` — write/read round-trip; missing-state throw; default temp-dir path used when no override
- [x] 4.2 Unit tests for `SqliteCoreLongRunningServiceManager` — parent suspended (children not yet done); parent suspended (children already done triggers resume); children finished (count not met); children finished (count met triggers resume); parent finished updates row

## 5. Version and Validation
- [x] 5.1 Bump version in `Directory.Build.props`: `3.2.1 → 3.2.2`
- [x] 5.2 Run `dotnet build BaseLib.sln` — must be clean (zero errors, zero CS1591 warnings)
- [x] 5.3 Run `dotnet test` — all tests pass
