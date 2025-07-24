using System.ComponentModel.DataAnnotations.Schema;

namespace Fuelflux.Core.Models;

[Table("fuel_stations")]
public class FuelStation
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    public ICollection<FuelTank> FuelTanks { get; set; } = [];
    public ICollection<PumpController> PumpControllers { get; set; } = [];
}
