using BaseLib.Core.Models;

namespace BasicErp.Inventory;

public class ReleaseInventoryRequest : CoreRequestBase
{
    public string OrderId { get; set; } = string.Empty;
}
