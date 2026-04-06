using BaseLib.Core.Models;

namespace BasicErp.Orders;

public class PlaceOrderResponse : CoreResponseBase
{
    public string? OrderId { get; set; }
}
