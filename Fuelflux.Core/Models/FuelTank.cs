using System.ComponentModel.DataAnnotations.Schema;

namespace Fuelflux.Core.Models;

[Table("fuel_tanks")]
public class FuelTank
{
    [Column("id")]
    public int Id { get; set; }

    [Column("number", TypeName = "numeric(3)")]
    public decimal Number { get; set; }

    [Column("fuel_station_id")]
    public int FuelStationId { get; set; }
    public virtual FuelStation FuelStation { get; set; } = null!;
}
