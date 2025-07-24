using Fuelflux.Core.Models;

namespace Fuelflux.Core.Services;

public record DeviceValidationResult(string PumpControllerUid, string UserUid);

public interface IDeviceAuthService
{
    string Authorize(PumpController pump, User user);
    DeviceValidationResult? Validate(string token);
    void Deauthorize(string token);
    void RemoveExpiredTokens();
}
