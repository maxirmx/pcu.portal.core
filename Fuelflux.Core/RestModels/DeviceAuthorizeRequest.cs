using System;
namespace Fuelflux.Core.RestModels;

public class DeviceAuthorizeRequest
{
    public required string PumpControllerUid { get; set; }
    public required string UserUid { get; set; }
}
