namespace Fuelflux.Core.RestModels;

public class FuelTankUpdateItem
{
    public decimal? Number { get; set; }
    public decimal? Volume { get; set; }
    public int? FuelStationId { get; set; }
}
