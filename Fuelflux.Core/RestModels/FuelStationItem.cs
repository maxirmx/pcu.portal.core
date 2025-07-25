using System.Text.Json;
using Fuelflux.Core.Models;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.RestModels;

public class FuelStationItem(FuelStation fs)
{
    public int Id { get; set; } = fs.Id;
    public string Name { get; set; } = fs.Name;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
