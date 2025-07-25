namespace Fuelflux.Core.RestModels;

public class FuelTankCreateItem
{
    public decimal Number { get; set; }
    public decimal? Allowance { get; set; }
    public int FuelStationId { get; set; }
}
