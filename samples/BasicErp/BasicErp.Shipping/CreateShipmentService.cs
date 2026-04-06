using BaseLib.Core.Services;

namespace BasicErp.Shipping;

public class CreateShipmentService : CoreServiceBase<CreateShipmentRequest, CreateShipmentResponse>
{
    protected override Task<CreateShipmentResponse> RunAsync()
        => Task.FromResult(Succeed());
}
