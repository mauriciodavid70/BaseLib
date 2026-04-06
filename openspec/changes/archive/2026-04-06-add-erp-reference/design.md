# Design: BasicErp Reference Sample

## Purpose
Show the full BaseLib usage pattern in a self-contained, runnable application. Every major
abstraction introduced across the cloud-agnostic migration is exercised:

| Abstraction | Used by |
|---|---|
| `CoreServiceBase<TReq,TRes>` | Inventory, Invoicing, Shipping services |
| `CoreLongRunningServiceBase<TReq,TRes>` | `PlaceOrderService` |
| `ICoreServiceFireOnly` | `NatsCoreServiceFireOnly` (dispatch) |
| `ICoreStatusEventSink` | `NatsCoreStatusEventSink` (event routing) |
| `ICoreServiceStateStore` | `FileSystemCoreServiceStateStore` (temp dir) |
| `ICoreLongRunningServiceManager` | `SqliteCoreLongRunningServiceManager` |
| `NatsFireAsyncBackgroundServiceBase` | Per-service consumers |

## Project Dependency Graph
```
BasicErp.Orders      → BaseLib.Core
BasicErp.Inventory   → BaseLib.Core
BasicErp.Invoicing   → BaseLib.Core
BasicErp.Shipping    → BaseLib.Core
BasicErp.Host        → BasicErp.Orders
                     → BasicErp.Inventory
                     → BasicErp.Invoicing
                     → BasicErp.Shipping
                     → BaseLib.Core.Nats
                     → BaseLib.Core.Local
                     → BaseLib.Core.Sqlite
```

Module projects reference only `BaseLib.Core` — they have no transport or infrastructure
dependency. Only the host wires infrastructure, mirroring real-world microservice deployment.

## NATS Stream and Consumer Design

One JetStream stream (`BASICERP`) with subject `basicerp.dispatch.>` covers all service types:

| Service | Subject | Consumer name |
|---|---|---|
| `PlaceOrderService` | `basicerp.dispatch.orders` | `orders-consumer` |
| `ReserveInventoryService` | `basicerp.dispatch.inventory.reserve` | `inventory-reserve-consumer` |
| `ReleaseInventoryService` | `basicerp.dispatch.inventory.release` | `inventory-release-consumer` |
| `CreateInvoiceService` | `basicerp.dispatch.invoicing` | `invoicing-consumer` |
| `CreateShipmentService` | `basicerp.dispatch.shipping` | `shipping-consumer` |

A second stream (`BASICERP_EVENTS`) with subject `basicerp.events.>` carries `CoreStatusEvent`
payloads. The long-running manager's hosted service subscribes to `basicerp.events.>` and
routes events to `ICoreLongRunningServiceManager`.

## Long-Running Manager Event Routing
`SqliteCoreLongRunningServiceManager` reacts to `CoreStatusEvent` emitted by NATS. A
`LongRunningManagerEventHandler : BackgroundService` in `BasicErp.Host` subscribes to the
events stream and calls:
- `HandleParentSuspendedAsync` when `CoreServiceStatus == Suspended`
- `HandleParentFinishedAsync` when status is `Succeeded` or `Failed` and `IsLongRunningChild == false`
- `HandleChildrenFinishedAsync` when `IsLongRunningChild == true`

This keeps the routing logic in the sample, not in the library.

## HTTP Entry Point
A single minimal API endpoint `POST /orders` accepts a `PlaceOrderRequest` body and calls
`ICoreServiceFireOnly.FireAsync<PlaceOrderService>`. The response is `202 Accepted` — the
caller polls or subscribes separately for completion events.

## SQLite File Location
`SqliteInfrastructureOptions.ConnectionString` is set to `Data Source=basicerp.db` — the file
is created in the working directory of the container. For the docker-compose scenario this is
ephemeral (no volume mount), which is acceptable for a demo.

## Dockerfile
A minimal `Dockerfile` in `BasicErp.Host/` using `mcr.microsoft.com/dotnet/aspnet:8.0` as
runtime and `mcr.microsoft.com/dotnet/sdk:8.0` as build image. The build COPY context is the
repo root so project references resolve correctly.

## What This Sample Does NOT Show
- Authentication / authorisation
- Dead-letter handling (operator concern — configure NATS MaxDeliver)
- Multi-replica deployment (FileSystem state store is single-replica only by design)
- Production secrets management (uses `EnvironmentSecretsVault` stub)
