using BaseLib.Core.Models;

namespace BasicErp.Orders;

public class PlaceOrderRequest : CoreRequestBase
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
