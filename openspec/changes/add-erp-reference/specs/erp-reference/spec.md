## ADDED Requirements

### Requirement: BasicErp Solution Structure
The `samples/BasicErp/` directory SHALL contain a `BasicErp.sln` solution with five projects:
`BasicErp.Orders`, `BasicErp.Inventory`, `BasicErp.Invoicing`, `BasicErp.Shipping`, and
`BasicErp.Host`. Module projects (`Orders`, `Inventory`, `Invoicing`, `Shipping`) SHALL
reference only `BaseLib.Core`. `BasicErp.Host` SHALL reference all four module projects and
`BaseLib.Core.Nats`, `BaseLib.Core.Local`, and `BaseLib.Core.Sqlite`.

#### Scenario: Solution builds from repo root
- **WHEN** `dotnet build samples/BasicErp/BasicErp.sln` is run from the repository root
- **THEN** all five projects build with zero errors and zero CS1591 warnings

### Requirement: Order Placement Orchestration
`PlaceOrderService` SHALL extend `CoreLongRunningServiceBase<PlaceOrderRequest, PlaceOrderResponse>`
and orchestrate inventory reservation and invoice creation in parallel before dispatching
shipment creation.

#### Scenario: RunAsync fires inventory and invoicing in parallel
- **WHEN** `PlaceOrderService.RunAsync()` is called via NATS dispatch
- **THEN** `FireManyAsync` is called with `[ReserveInventoryRequest, CreateInvoiceRequest]`
- **AND** the service suspends, persisting state via `ICoreServiceStateStore`

#### Scenario: ResumeAsync fires shipment and succeeds
- **WHEN** `PlaceOrderService.ResumeAsync()` is called after both children complete
- **THEN** `FireAsync<CreateShipmentService>` is called
- **AND** the service calls `Succeed()`

### Requirement: Module Services
The four leaf services SHALL each extend `CoreServiceBase<TRequest, TResponse>` and implement `RunAsync()` returning `Succeed()`: `ReserveInventoryService`, `ReleaseInventoryService`, `CreateInvoiceService`, and `CreateShipmentService`.

#### Scenario: Each leaf service succeeds
- **WHEN** any leaf service `RunAsync()` is invoked via NATS dispatch
- **THEN** the service returns a response with `Succeeded = true`

### Requirement: NATS Consumer Wiring
`BasicErp.Host` SHALL register one `NatsFireAsyncBackgroundServiceBase` subclass per service
type as a hosted service. Each consumer SHALL subscribe to the service-specific subject on the
`BASICERP` JetStream stream and dispatch received messages via `FireAsyncMessageDispatcher`.

#### Scenario: Message dispatched to correct service
- **WHEN** a `FireAsyncMessage` for `PlaceOrderService` arrives on the orders subject
- **THEN** the orders consumer deserializes and dispatches it to `PlaceOrderService.RunAsync()`

### Requirement: Long-Running Manager Event Handler
`BasicErp.Host` SHALL include a hosted service that subscribes to the `BASICERP_EVENTS` NATS
stream and routes `CoreStatusEvent` payloads to `ICoreLongRunningServiceManager`.

#### Scenario: Suspended event routes to HandleParentSuspendedAsync
- **WHEN** a `CoreStatusEvent` with `Status = Suspended` arrives on the events stream
- **THEN** `ICoreLongRunningServiceManager.HandleParentSuspendedAsync` is called

#### Scenario: Child finished event routes to HandleChildrenFinishedAsync
- **WHEN** a `CoreStatusEvent` with `IsLongRunningChild = true` arrives on the events stream
- **THEN** `ICoreLongRunningServiceManager.HandleChildrenFinishedAsync` is called

### Requirement: HTTP Entry Point
`BasicErp.Host` SHALL expose a `POST /orders` minimal API endpoint that accepts a
`PlaceOrderRequest` JSON body, calls `ICoreServiceFireOnly.FireAsync<PlaceOrderService>`,
and returns `202 Accepted`.

#### Scenario: POST /orders enqueues order
- **WHEN** `POST /orders` is called with a valid `PlaceOrderRequest` body
- **THEN** a NATS JetStream message is published for `PlaceOrderService`
- **AND** the HTTP response is `202 Accepted`

### Requirement: Docker Compose Environment
`BasicErp.Host/docker-compose.yml` SHALL define a `nats` service with JetStream enabled and a
`basicerp` service built from the repository root, with `basicerp` depending on `nats`.

#### Scenario: Compose up starts both services
- **WHEN** `docker compose up` is run from `BasicErp.Host/`
- **THEN** the NATS server starts with JetStream enabled
- **AND** the BasicErp host starts and connects to NATS successfully
