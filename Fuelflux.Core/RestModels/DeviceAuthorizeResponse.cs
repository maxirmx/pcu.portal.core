using System.Text.Json;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.RestModels;

public class DeviceAuthorizeResponse
{
    public required string Token { get; set; }
    public int RoleId { get; set; }
    public IEnumerable<FuelTankItem> FuelTanks { get; set; } = [];
    public decimal? Allowance { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
