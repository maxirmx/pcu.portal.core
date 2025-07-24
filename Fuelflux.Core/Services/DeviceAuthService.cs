using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Fuelflux.Core.Settings;

namespace Fuelflux.Core.Services;

public class DeviceAuthService : IDeviceAuthService
{
    private readonly ConcurrentDictionary<string, DateTime> _sessions = new();
    private readonly DeviceAuthSettings _settings;
    private readonly ILogger<DeviceAuthService> _logger;

    public DeviceAuthService(IOptions<DeviceAuthSettings> options, ILogger<DeviceAuthService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public string Authorize()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var expires = DateTime.UtcNow.AddMinutes(_settings.SessionMinutes);
        _sessions[token] = expires;
        _logger.LogDebug("Token {token} authorized until {expires}", token, expires);
        return token;
    }

    public bool Validate(string token)
    {
        if (_sessions.TryGetValue(token, out var expires))
        {
            if (DateTime.UtcNow < expires)
            {
                return true;
            }
            _sessions.TryRemove(token, out _);
            _logger.LogDebug("Token {token} expired", token);
        }
        return false;
    }

    public void Deauthorize(string token)
    {
        _sessions.TryRemove(token, out _);
        _logger.LogDebug("Token {token} deauthorized", token);
    }
}
