using BaseLib.Core.Models;

namespace BasicErp.Inventory;

public class ReserveInventoryRequest : CoreRequestBase
{
    public string OrderId { get; set; } = string.Empty;
}
