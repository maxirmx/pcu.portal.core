using System;
using Fuelflux.Core.Services;
using Fuelflux.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fuelflux.Core.Models;
using NUnit.Framework;

namespace Fuelflux.Core.Tests.Services;

[TestFixture]
public class DeviceAuthServiceTests
{
    private DeviceAuthService _service = null!;

    private Fuelflux.Core.Models.PumpController _pump = null!;
    private Fuelflux.Core.Models.User _user = null!;

    [SetUp]
    public void Setup()
    {
        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 1 });
        var appOpts = Options.Create(new AppSettings { Secret = "secret" });
        var logger = new LoggerFactory().CreateLogger<DeviceAuthService>();
        _service = new DeviceAuthService(opts, appOpts, logger);

        _pump = new Fuelflux.Core.Models.PumpController
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            FuelStationId = 1,
            FuelStation = new Fuelflux.Core.Models.FuelStation { Id = 1, Name = "fs" }
        };

        var role = new Fuelflux.Core.Models.Role { Id = 1, RoleId = UserRoleConstants.Operator, Name = "op" };
        _user = new Fuelflux.Core.Models.User
        {
            Id = 1,
            Email = "a@b.c",
            Password = "p",
            FirstName = "",
            LastName = "",
            Patronymic = "",
            Uid = "uid",
            RoleId = role.Id,
            Role = role
        };
    }

    [Test]
    public void Authorize_And_Validate_ReturnsTrue()
    {
        var token = _service.Authorize(_pump, _user);
        Assert.That(_service.Validate(token), Is.True);
    }

    [Test]
    public void Deauthorize_RemovesToken()
    {
        var token = _service.Authorize(_pump, _user);
        _service.Deauthorize(token);
        Assert.That(_service.Validate(token), Is.False);
    }

    [Test]
    public void Validate_ReturnsFalse_WhenExpired()
    {
        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 0 });
        var appOpts = Options.Create(new AppSettings { Secret = "secret" });
        var logger = new LoggerFactory().CreateLogger<DeviceAuthService>();
        var svc = new DeviceAuthService(opts, appOpts, logger);
        var pump = new Fuelflux.Core.Models.PumpController
        {
            Id = 2,
            Guid = Guid.NewGuid(),
            FuelStationId = 2,
            FuelStation = new Fuelflux.Core.Models.FuelStation { Id = 2, Name = "fs" }
        };
        var role = new Fuelflux.Core.Models.Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "op" };
        var user = new Fuelflux.Core.Models.User
        {
            Id = 2,
            Email = "x@y.z",
            Password = "p",
            FirstName = "",
            LastName = "",
            Patronymic = "",
            Uid = "uid2",
            RoleId = role.Id,
            Role = role
        };
        var token = svc.Authorize(pump, user);
        Assert.That(svc.Validate(token), Is.False);
    }
}
