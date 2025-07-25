using System.ComponentModel.DataAnnotations;

namespace Fuelflux.Core.RestModels;

public class FuelIntakeRequest
{
    [Required(ErrorMessage = "����� ���������� ����������.")]
    [Range(1, int.MaxValue, ErrorMessage = "����� ���������� ������ ���� ������������� ������.")]
    public int TankNumber { get; set; }

    [Required(ErrorMessage = "o���� ��������� ������� ����������.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "����� ��������� ������� ������ ���� ������������� ������.")]
    public decimal IntakeVolume { get; set; }
}
