using BaseLib.Core.Models;

namespace BasicErp.Shipping;

public class CreateShipmentRequest : CoreRequestBase
{
    public string OrderId { get; set; } = string.Empty;
}
