using System.ComponentModel.DataAnnotations;

namespace Fuelflux.Core.RestModels;

public class PumpUserRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "first must be non-negative.")]
    public int First { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "number must be positive.")]
    public int Number { get; set; }
}
