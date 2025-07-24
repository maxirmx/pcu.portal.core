using System.Collections.Generic;

namespace Fuelflux.Core.RestModels;

public class PumpAuthorizeResponse
{
    public required string Role { get; set; }
    public decimal? MaxVolume { get; set; }
    public decimal? FuelPrice { get; set; }
    public List<VirtualTankInfo>? VirtualTanks { get; set; }
    public string? Token { get; set; }
}
