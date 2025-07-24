using System;
namespace Fuelflux.Core.RestModels;

public class DeviceAuthorizeRequest
{
    public required Guid PumpControllerGuid { get; set; }
    public required string UserUid { get; set; }
}
