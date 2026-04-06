using BaseLib.Core.Models;

namespace BasicErp.Invoicing;

public class CreateInvoiceRequest : CoreRequestBase
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
