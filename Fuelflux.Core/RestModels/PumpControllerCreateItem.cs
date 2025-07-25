namespace Fuelflux.Core.RestModels;

public class PumpControllerCreateItem
{
    public string Uid { get; set; } = string.Empty;
    public int FuelStationId { get; set; }
}
