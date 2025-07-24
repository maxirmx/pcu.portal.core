using Fuelflux.Core.Models;

namespace Fuelflux.Core.Services;

public interface IDeviceAuthService
{
    string Authorize(PumpController pump, User user);
    bool Validate(string token);
    void Deauthorize(string token);
    void RemoveExpiredTokens();
}
