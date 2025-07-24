using Fuelflux.Core.Services;
using Fuelflux.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fuelflux.Core.Tests.Services;

[TestFixture]
public class DeviceAuthServiceTests
{
    private DeviceAuthService _service = null!;

    [SetUp]
    public void Setup()
    {
        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 1 });
        var logger = new LoggerFactory().CreateLogger<DeviceAuthService>();
        _service = new DeviceAuthService(opts, logger);
    }

    [Test]
    public void Authorize_And_Validate_ReturnsTrue()
    {
        var token = _service.Authorize();
        Assert.That(_service.Validate(token), Is.True);
    }

    [Test]
    public void Deauthorize_RemovesToken()
    {
        var token = _service.Authorize();
        _service.Deauthorize(token);
        Assert.That(_service.Validate(token), Is.False);
    }

    [Test]
    public void Validate_ReturnsFalse_WhenExpired()
    {
        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = -1 });
        var logger = new LoggerFactory().CreateLogger<DeviceAuthService>();
        var svc = new DeviceAuthService(opts, logger);
        var token = svc.Authorize();
        Assert.That(svc.Validate(token), Is.False);
    }
}
