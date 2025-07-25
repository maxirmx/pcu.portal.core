using System.Text.Json;
using Fuelflux.Core.Models;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.RestModels;

public class FuelStationItem
{
    public FuelStationItem() { }

    public FuelStationItem(FuelStation fs)
    {
        Id = fs.Id;
        Name = fs.Name;
    }

    public int Id { get; set; }
    public string? Name { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
