using BaseLib.Core.Services;
using BasicErp.Inventory;
using BasicErp.Invoicing;
using BasicErp.Shipping;

namespace BasicErp.Orders;

public class PlaceOrderService : CoreLongRunningServiceBase<PlaceOrderRequest, PlaceOrderResponse>
{
    public PlaceOrderService(
        ICoreServiceFireOnly fireOnly,
        ICoreServiceStateStore stateStore,
        ICoreStatusEventSink? eventSink = null)
        : base(fireOnly, stateStore, validator: null, eventSink: eventSink)
    {
    }

    protected override async Task<PlaceOrderResponse> RunAsync()
    {
        // Fire inventory reservation and invoice creation in parallel
        await FireAsync<ReserveInventoryService>(
            new ReserveInventoryRequest { OrderId = Request.OrderId });

        await FireAsync<CreateInvoiceService>(
            new CreateInvoiceRequest { OrderId = Request.OrderId, Amount = Request.Amount });

        // Suspend — ResumeAsync is called when both children finish
        return Succeed(messages: $"Order {Request.OrderId} processing started.");
    }

    protected override async Task<PlaceOrderResponse> ResumeAsync()
    {
        // Both children (Inventory + Invoice) are done — now ship
        await FireAsync<CreateShipmentService>(
            new CreateShipmentRequest { OrderId = Request.OrderId });

        return Succeed(messages: $"Order {Request.OrderId} fulfilled.");
    }
}
