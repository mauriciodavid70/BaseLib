## 1. Solution Scaffold
- [ ] 1.1 Create `samples/BasicErp/` directory and `BasicErp.sln`
- [ ] 1.2 Create `BasicErp.Orders/BasicErp.Orders.csproj` — references `BaseLib.Core`
- [ ] 1.3 Create `BasicErp.Inventory/BasicErp.Inventory.csproj` — references `BaseLib.Core`
- [ ] 1.4 Create `BasicErp.Invoicing/BasicErp.Invoicing.csproj` — references `BaseLib.Core`
- [ ] 1.5 Create `BasicErp.Shipping/BasicErp.Shipping.csproj` — references `BaseLib.Core`
- [ ] 1.6 Create `BasicErp.Host/BasicErp.Host.csproj` — ASP.NET minimal API; references all four module projects + `BaseLib.Core.Nats`, `BaseLib.Core.Local`, `BaseLib.Core.Sqlite`
- [ ] 1.7 Add all six projects to `BasicErp.sln`

## 2. Module Services
- [ ] 2.1 `BasicErp.Orders`: `PlaceOrderRequest`, `PlaceOrderResponse`, `PlaceOrderService : CoreLongRunningServiceBase<PlaceOrderRequest, PlaceOrderResponse>` — `RunAsync` fires ReserveInventory + CreateInvoice via `FireManyAsync`; `ResumeAsync` fires CreateShipment then calls `Succeed()`
- [ ] 2.2 `BasicErp.Inventory`: `ReserveInventoryRequest/Response`, `ReserveInventoryService`; `ReleaseInventoryRequest/Response`, `ReleaseInventoryService` — both call `Succeed()`
- [ ] 2.3 `BasicErp.Invoicing`: `CreateInvoiceRequest/Response`, `CreateInvoiceService` — calls `Succeed()`
- [ ] 2.4 `BasicErp.Shipping`: `CreateShipmentRequest/Response`, `CreateShipmentService` — calls `Succeed()`

## 3. Host — Infrastructure Wiring
- [ ] 3.1 `Program.cs`: register `AddNatsTransport`, `AddLocalServices`, `AddSqliteInfrastructure`, `AddCoreServiceRunner` (or equivalent)
- [ ] 3.2 Configure NATS JetStream streams `BASICERP` and `BASICERP_EVENTS` on startup (create if not exists)
- [ ] 3.3 Implement one `NatsFireAsyncBackgroundServiceBase` subclass per service type (5 consumers): `PlaceOrderConsumer`, `ReserveInventoryConsumer`, `ReleaseInventoryConsumer`, `CreateInvoiceConsumer`, `CreateShipmentConsumer`
- [ ] 3.4 Register all five consumers as `IHostedService` in `Program.cs`
- [ ] 3.5 Implement `LongRunningManagerEventHandler : BackgroundService` — subscribes to `BASICERP_EVENTS` stream; routes `CoreStatusEvent` to `ICoreLongRunningServiceManager` based on `Status` and `IsLongRunningChild`
- [ ] 3.6 Register `LongRunningManagerEventHandler` as `IHostedService`
- [ ] 3.7 Add `POST /orders` minimal API endpoint — calls `ICoreServiceFireOnly.FireAsync<PlaceOrderService>`; returns `202 Accepted`

## 4. Docker Compose and Dockerfile
- [ ] 4.1 Add `BasicErp.Host/Dockerfile` — multi-stage build; COPY context is repo root to resolve project references
- [ ] 4.2 Add `BasicErp.Host/docker-compose.yml` — `nats` service (`nats:latest -js`); `basicerp` service (`build: context: ../..`); `depends_on: nats`

## 5. Validation
- [ ] 5.1 Run `dotnet build samples/BasicErp/BasicErp.sln` — zero errors
- [ ] 5.2 Run `docker compose up --build` from `BasicErp.Host/` — both services start; NATS connects; `POST /orders` returns `202`
