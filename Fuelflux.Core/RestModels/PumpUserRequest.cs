using System.ComponentModel.DataAnnotations;

namespace Fuelflux.Core.RestModels;

public class PumpUserRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "��������� �������� �� ����� ���� �������������.")]
    public int First { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "���������� ������ ���� �������������.")]
    public int Number { get; set; }
}
