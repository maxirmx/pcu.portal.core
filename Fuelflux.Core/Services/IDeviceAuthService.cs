using Fuelflux.Core.RestModels;

namespace Fuelflux.Core.Services;

public interface IDeviceAuthService
{
    string Authorize();
    bool Validate(string token);
    void Deauthorize(string token);
}
