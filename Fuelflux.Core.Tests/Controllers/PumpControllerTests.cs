using Fuelflux.Core.Controllers;
using Microsoft.AspNetCore.Mvc;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;
using Fuelflux.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fuelflux.Core.Tests.Controllers;

[TestFixture]
public class PumpControllerTests
{
    private PumpController _controller = null!;
    private DeviceAuthService _service = null!;

    [SetUp]
    public void Setup()
    {
        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 1 });
        var appOpts = Options.Create(new AppSettings { Secret = "secret" });
        _service = new DeviceAuthService(opts, appOpts, new LoggerFactory().CreateLogger<DeviceAuthService>());
        _controller = new PumpController(_service, new LoggerFactory().CreateLogger<PumpController>());
    }

    [Test]
    public void Authorize_ReturnsValidToken()
    {
        var result = _controller.Authorize();
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var token = ((result.Result as OkObjectResult)!.Value as TokenResponse)!.Token;
        Assert.That(_service.Validate(token), Is.True);
    }

    [Test]
    public void Deauthorize_RemovesToken()
    {
        var token = _service.Authorize();
        var res = _controller.Deauthorize(new TokenRequest { Token = token });
        Assert.That(res, Is.TypeOf<OkResult>());
        Assert.That(_service.Validate(token), Is.False);
    }
}
