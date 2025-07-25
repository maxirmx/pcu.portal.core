using System.Text.Json;
using Fuelflux.Core.Models;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.RestModels;

public class FuelTankViewItem
{
    public FuelTankViewItem() { }

    public FuelTankViewItem(FuelTank tank)
    {
        Id = tank.Id;
        Number = tank.Number;
        Volume = tank.Volume;
        FuelStationId = tank.FuelStationId;
    }

    public int Id { get; set; }
    public decimal? Number { get; set; }
    public decimal? Volume { get; set; }
    public int? FuelStationId { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
