using System.ComponentModel.DataAnnotations;

namespace Fuelflux.Core.RestModels;

public class FuelIntakeRequest
{
    [Required(ErrorMessage = "номер резервуара обязателен.")]
    [Range(1, int.MaxValue, ErrorMessage = "номер резервуара должен быть положительным числом.")]
    public int TankNumber { get; set; }

    [Required(ErrorMessage = "oбъем принятого топлива обязателен.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "объем принятого топлива должен быть положительным числом.")]
    public decimal IntakeVolume { get; set; }
}
