using System.Text.Json;
using Fuelflux.Core.Models;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.RestModels;

public class PumpControllerItem
{
    public PumpControllerItem() { }

    public PumpControllerItem(PumpCntrl pump)
    {
        Id = pump.Id;
        Uid = pump.Uid;
        FuelStationId = pump.FuelStationId;
    }

    public int Id { get; set; }
    public string? Uid { get; set; }
    public int? FuelStationId { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
