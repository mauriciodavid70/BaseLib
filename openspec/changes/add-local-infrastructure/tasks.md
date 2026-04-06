## 1. BaseLib.Core.Local — New Project
- [ ] 1.1 Create `BaseLib.Core.Local/BaseLib.Core.Local.csproj` — references `BaseLib.Core`; added to `BaseLib.sln`
- [ ] 1.2 Move `FileSystemCoreServiceStateStore.cs` from `BaseLib.Core.Containers/` to `BaseLib.Core.Local/`; update namespace to `BaseLib.Core.Local`
- [ ] 1.3 Add `LocalServicesOptions` — `StateStoreRootDirectory` defaults to `Path.GetTempPath()`
- [ ] 1.4 Add `LocalServicesExtensions.AddLocalServices(IServiceCollection, Action<LocalServicesOptions>?)` — registers `ICoreServiceStateStore` as singleton
- [ ] 1.5 Add XML doc comments to all public types and members (CS1591 enforcement)

## 2. BaseLib.Core.Containers — Cleanup (breaking)
- [ ] 2.1 Delete `FileSystemCoreServiceStateStore.cs` from `BaseLib.Core.Containers/` (moved to Local)
- [ ] 2.2 Remove `StateStoreRootDirectory` from `ContainerServicesOptions`
- [ ] 2.3 Remove `ICoreServiceStateStore` singleton registration from `AddContainerServices`
- [ ] 2.4 Update `ContainerServicesExtensions` XML doc to remove reference to `FileSystemCoreServiceStateStore`

## 3. BaseLib.Core.Sqlite — New Project
- [ ] 3.1 Create `BaseLib.Core.Sqlite/BaseLib.Core.Sqlite.csproj` — references `BaseLib.Core`; NuGet dep `Microsoft.Data.Sqlite 8.x`; added to `BaseLib.sln`
- [ ] 3.2 Add `SqliteInfrastructureOptions` — `ConnectionString` property
- [ ] 3.3 Implement `SqliteCoreLongRunningServiceManager : ICoreLongRunningServiceManager`
  - `HandleParentSuspendedAsync` — insert parent row; call `ResumeAsync` if all children already done
  - `HandleChildrenFinishedAsync` — `INSERT OR IGNORE` child rows; update counters; call `ResumeAsync` when count satisfied
  - `HandleParentFinishedAsync` — update parent row status/outcome columns
  - Table creation: `CREATE TABLE IF NOT EXISTS LONG_RUNNING_BATCH (...)` called in constructor
- [ ] 3.4 Add `SqliteInfrastructureExtensions.AddSqliteInfrastructure(IServiceCollection, Action<SqliteInfrastructureOptions>)` — registers `ICoreLongRunningServiceManager` as singleton
- [ ] 3.5 Add XML doc comments to all public types and members (CS1591 enforcement)

## 4. Tests (BaseLib.Core.Tests)
- [ ] 4.1 Unit tests for `FileSystemCoreServiceStateStore` — write/read round-trip; missing-state throw; default temp-dir path used when no override
- [ ] 4.2 Unit tests for `SqliteCoreLongRunningServiceManager` — parent suspended (children not yet done); parent suspended (children already done triggers resume); children finished (count not met); children finished (count met triggers resume); parent finished updates row

## 5. Version and Validation
- [ ] 5.1 Bump version in `Directory.Build.props`: `3.2.1 → 3.2.2`
- [ ] 5.2 Run `dotnet build BaseLib.sln` — must be clean (zero errors, zero CS1591 warnings)
- [ ] 5.3 Run `dotnet test` — all tests pass
