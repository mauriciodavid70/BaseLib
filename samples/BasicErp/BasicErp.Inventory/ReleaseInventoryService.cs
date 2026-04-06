using BaseLib.Core.Services;

namespace BasicErp.Inventory;

public class ReleaseInventoryService : CoreServiceBase<ReleaseInventoryRequest, ReleaseInventoryResponse>
{
    protected override Task<ReleaseInventoryResponse> RunAsync()
        => Task.FromResult(Succeed());
}
