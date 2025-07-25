using System.ComponentModel.DataAnnotations;

namespace Fuelflux.Core.RestModels;

public class PumpUserRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "начальное значение не может быть отрицательным.")]
    public int First { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "количество должно быть положительным.")]
    public int Number { get; set; }
}
