using System.Text.Json;
using Fuelflux.Core.Models;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.RestModels;

public class PumpControllerItem(PumpCntrl pump)
{
    public int Id { get; set; } = pump.Id;
    public string Uid { get; set; } = pump.Uid;
    public int FuelStationId { get; set; } = pump.FuelStationId;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
