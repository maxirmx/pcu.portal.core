using System.Text.Json;
using Fuelflux.Core.Models;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.RestModels;

public class FuelTankViewItem(FuelTank tank)
{
    public int Id { get; set; } = tank.Id;
    public decimal Number { get; set; } = tank.Number;
    public decimal Allowance { get; set; } = tank.Allowance;
    public int FuelStationId { get; set; } = tank.FuelStationId;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
