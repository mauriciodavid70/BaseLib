# Change: BasicErp Reference Sample

## Why
BaseLib provides all the primitives for cloud-agnostic async service orchestration but has no
runnable reference implementation. New adopters must piece together the patterns from unit tests
and documentation. The ERP sample demonstrates the full stack end-to-end: multi-module service
dispatch over NATS, long-running orchestration with resume via SQLite, domain event
choreography, and a docker-compose environment — runnable on any developer machine without AWS.

## What Changes
New `samples/BasicErp/` solution — five projects, runnable with `docker compose up`:

### Projects (project references to BaseLib — no NuGet dependency)
| Project | Contents |
|---|---|
| `BasicErp.Orders/` | `PlaceOrderService` (long-running orchestrator), request/response types |
| `BasicErp.Inventory/` | `ReserveInventoryService`, `ReleaseInventoryService`, types |
| `BasicErp.Invoicing/` | `CreateInvoiceService`, types |
| `BasicErp.Shipping/` | `CreateShipmentService`, types |
| `BasicErp.Host/` | ASP.NET minimal API host, DI wiring, NATS consumers, docker-compose.yml |

### Orchestration flow
```
HTTP POST /orders
  → NatsCoreServiceFireOnly.FireAsync<PlaceOrderService>
      PlaceOrderService.RunAsync()
        → FireManyAsync: [ReserveInventoryService, CreateInvoiceService]
        → (suspends — state persisted via FileSystemCoreServiceStateStore)
      SqliteCoreLongRunningServiceManager.HandleChildrenFinishedAsync()
        → ResumeAsync<PlaceOrderService>
      PlaceOrderService.ResumeAsync()
        → FireAsync<CreateShipmentService>
        → Succeed()
```

### Infrastructure (BasicErp.Host)
- `AddNatsTransport` — `ICoreServiceFireOnly` + `ICoreStatusEventSink` (NATS JetStream)
- `AddLocalServices` — `ICoreServiceStateStore` (file system, temp dir)
- `AddSqliteInfrastructure` — `ICoreLongRunningServiceManager` (SQLite)
- NATS consumers: one `NatsFireAsyncBackgroundServiceBase` subclass per service type
- `ICoreLongRunningServiceManager` consumed by a `NatsCoreStatusEventSink`-driven event handler
  hosted service that routes `CoreStatusEvent` to `HandleParentSuspendedAsync`,
  `HandleParentFinishedAsync`, or `HandleChildrenFinishedAsync`

### docker-compose.yml (BasicErp.Host/)
- `nats` service — `nats:latest`, JetStream enabled (`-js` flag)
- `basicerp` service — built from `BasicErp.Host/`, `depends_on: nats`

## Impact
- No changes to any `BaseLib.Core*` packages
- New `samples/BasicErp/BasicErp.sln` solution
- Depends on: `add-local-infrastructure` (must be merged first)
- NOT breaking — `samples/` is not a packaged library
- No version bump to `Directory.Build.props`
