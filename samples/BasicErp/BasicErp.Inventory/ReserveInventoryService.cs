using BaseLib.Core.Services;

namespace BasicErp.Inventory;

public class ReserveInventoryService : CoreServiceBase<ReserveInventoryRequest, ReserveInventoryResponse>
{
    protected override Task<ReserveInventoryResponse> RunAsync()
        => Task.FromResult(Succeed());
}
