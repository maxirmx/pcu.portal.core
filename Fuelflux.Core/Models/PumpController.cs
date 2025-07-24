using System.ComponentModel.DataAnnotations.Schema;

namespace Fuelflux.Core.Models;

[Table("pump_controllers")]
public class PumpController
{
    [Column("id")]
    public int Id { get; set; }

    [Column("guid")]
    public required string Guid { get; set; }

    [Column("fuel_station_id")]
    public int FuelStationId { get; set; }
    public FuelStation FuelStation { get; set; } = null!;
}
