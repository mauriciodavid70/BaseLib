using BasicErp.Host;
using BasicErp.Host.Consumers;
using BasicErp.Inventory;
using BasicErp.Invoicing;
using BasicErp.Orders;
using BasicErp.Shipping;
using BaseLib.Core.Services;
using BaseLib.Core.Services.Nats;
using BaseLib.Core.Local;
using BaseLib.Core.Sqlite;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

var builder = WebApplication.CreateBuilder(args);

// --- NATS connection ---
var natsUrl = builder.Configuration["Nats__Url"] ?? builder.Configuration["Nats:Url"] ?? "nats://localhost:4222";
var natsOpts = NatsOpts.Default with { Url = natsUrl };
var natsConnection = new NatsConnection(natsOpts);
builder.Services.AddSingleton<INatsConnection>(natsConnection);
builder.Services.AddSingleton<INatsJSContext>(_ =>
    new NatsJSContext((NatsConnection)natsConnection));

// --- BaseLib transport ---
builder.Services.AddNatsTransport(o =>
{
    o.StreamName = "BASICERP";
    o.DispatchSubject = "basicerp.dispatch";
});

// --- BaseLib infrastructure ---
builder.Services.AddLocalServices();
builder.Services.AddSqliteInfrastructure(o => o.ConnectionString = "Data Source=basicerp.db");

// --- Service runner ---
builder.Services.AddSingleton<ICoreServiceRunner, CoreServiceRunner>();
builder.Services.AddSingleton<FireAsyncMessageDispatcher>();

// --- ERP services (transient — each request gets a fresh instance) ---
builder.Services.AddTransient<PlaceOrderService>();
builder.Services.AddTransient<ReserveInventoryService>();
builder.Services.AddTransient<ReleaseInventoryService>();
builder.Services.AddTransient<CreateInvoiceService>();
builder.Services.AddTransient<CreateShipmentService>();

// --- NATS JetStream consumer (created after app is built so the stream exists) ---
builder.Services.AddSingleton<ErpDispatchConsumer>(sp =>
{
    var js = sp.GetRequiredService<INatsJSContext>();
    // Create stream and consumer synchronously during startup
    var consumer = CreateDispatchConsumerAsync(js).GetAwaiter().GetResult();
    var dispatcher = sp.GetRequiredService<FireAsyncMessageDispatcher>();
    return new ErpDispatchConsumer(dispatcher, consumer);
});
builder.Services.AddHostedService(sp => sp.GetRequiredService<ErpDispatchConsumer>());

// --- Long-running manager event handler ---
builder.Services.AddHostedService<LongRunningManagerEventHandler>();

var app = builder.Build();

// --- Ensure JetStream stream exists ---
using (var scope = app.Services.CreateScope())
{
    var js = scope.ServiceProvider.GetRequiredService<INatsJSContext>();
    await EnsureStreamAsync(js);
}

// --- HTTP endpoints ---
app.MapPost("/orders", async (PlaceOrderRequest request, ICoreServiceFireOnly fireOnly) =>
{
    await fireOnly.FireAsync<PlaceOrderService>(request);
    return Results.Accepted();
});

app.Run();

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static async Task EnsureStreamAsync(INatsJSContext js)
{
    try
    {
        await js.CreateStreamAsync(new StreamConfig
        {
            Name = "BASICERP",
            Subjects = ["basicerp.dispatch"],
        });
    }
    catch (NatsJSApiException ex) when (ex.Error.Code == 400)
    {
        // Stream already exists — ignore
    }
}

static async Task<INatsJSConsumer> CreateDispatchConsumerAsync(INatsJSContext js)
{
    return await js.CreateOrUpdateConsumerAsync("BASICERP", new ConsumerConfig
    {
        Name = "erp-dispatch-consumer",
        DurableName = "erp-dispatch-consumer",
        AckPolicy = ConsumerConfigAckPolicy.Explicit,
    });
}
