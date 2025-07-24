using System;
using Microsoft.AspNetCore.Http;
using Fuelflux.Core.Controllers;
using Microsoft.AspNetCore.Mvc;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;
using Fuelflux.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Fuelflux.Core.Data;
using Fuelflux.Core.Models;
using System.Threading.Tasks;
using PumpModel = Fuelflux.Core.Models.PumpController;
using NUnit.Framework;

namespace Fuelflux.Core.Tests.Controllers;

[TestFixture]
public class PumpControllerTests
{
    private Fuelflux.Core.Controllers.PumpController _controller = null!;
    private DeviceAuthService _service = null!;
    private AppDbContext _dbContext = null!;
    private PumpModel _pump = null!;
    private User _user = null!;

    [SetUp]
    public void Setup()
    {
        var dbOpts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pump_controller_test_db_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(dbOpts);

        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 1 });
        var appOpts = Options.Create(new AppSettings { Secret = "secret" });
        _service = new DeviceAuthService(opts, appOpts, new LoggerFactory().CreateLogger<DeviceAuthService>());

        var fs = new FuelStation { Id = 1, Name = "fs" };
        _pump = new PumpModel { Id = 1, Guid = Guid.NewGuid(), FuelStationId = fs.Id, FuelStation = fs };
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Operator, Name = "op" };
        _user = new User
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
        _dbContext.FuelStations.Add(fs);
        _dbContext.PumpControllers.Add(_pump);
        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(_user);
        _dbContext.SaveChanges();

        _controller = new Fuelflux.Core.Controllers.PumpController(
            _service,
            _dbContext,
            new LoggerFactory().CreateLogger<Fuelflux.Core.Controllers.PumpController>());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task Authorize_ReturnsValidToken()
    {
        var req = new DeviceAuthorizeRequest { PumpControllerGuid = _pump.Guid, UserUid = _user.Uid! };
        var result = await _controller.Authorize(req);
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var token = ((result.Result as OkObjectResult)!.Value as TokenResponse)!.Token;
        Assert.That(_service.Validate(token), Is.True);
    }

    [Test]
    public async Task Deauthorize_RemovesToken()
    {
        var token = _service.Authorize(_pump, _user);
        var res = _controller.Deauthorize(new TokenRequest { Token = token });
        Assert.That(res, Is.TypeOf<OkResult>());
        Assert.That(_service.Validate(token), Is.False);
    }

    [Test]
    public async Task Authorize_ReturnsUnauthorized_WhenPumpMissing()
    {
        var req = new DeviceAuthorizeRequest { PumpControllerGuid = Guid.NewGuid(), UserUid = _user.Uid! };
        var result = await _controller.Authorize(req);
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public async Task Authorize_ReturnsUnauthorized_WhenUserMissing()
    {
        var req = new DeviceAuthorizeRequest { PumpControllerGuid = _pump.Guid, UserUid = "wrong" };
        var result = await _controller.Authorize(req);
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }
}
