using BaseLib.Core.Services;

namespace BasicErp.Invoicing;

public class CreateInvoiceService : CoreServiceBase<CreateInvoiceRequest, CreateInvoiceResponse>
{
    protected override Task<CreateInvoiceResponse> RunAsync()
        => Task.FromResult(Succeed());
}
