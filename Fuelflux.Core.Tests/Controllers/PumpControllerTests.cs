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
using NUnit.Framework;

namespace Fuelflux.Core.Tests.Controllers;

[TestFixture]
public class PumpControllerTests
{
    private PumpController _controller = null!;
    private DeviceAuthService _service = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 1 });
        _service = new DeviceAuthService(opts, new LoggerFactory().CreateLogger<DeviceAuthService>());

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pump_controller_test_{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(dbOptions);

        // Seed roles
        _context.Roles.AddRange(
            new Role { Id = 1, RoleId = UserRoleConstants.Operator, Name = "Operator" },
            new Role { Id = 2, RoleId = UserRoleConstants.Customer, Name = "Customer" }
        );

        // Seed users
        _context.Users.AddRange(
            new User
            {
                Id = 10,
                Email = "op@test.com",
                Password = "pwd",
                Uid = "OPUID",
                UserRoles = [ new() { UserId = 10, RoleId = 1 } ]
            },
            new User
            {
                Id = 11,
                Email = "cust@test.com",
                Password = "pwd",
                Uid = "CUSTUID",
                Allowance = 100m,
                UserRoles = [ new() { UserId = 11, RoleId = 2 } ]
            }
        );

        // Seed tanks
        _context.FuelStations.Add(new FuelStation { Id = 1, Name = "FS" });
        _context.FuelTanks.AddRange(
            new FuelTank { Id = 1, Number = 1, FuelStationId = 1 },
            new FuelTank { Id = 2, Number = 2, FuelStationId = 1 }
        );

        _context.SaveChanges();

        _controller = new PumpController(_service, _context, new LoggerFactory().CreateLogger<PumpController>());
    }

    [Test]
    public async Task Authorize_ReturnsCustomerRole()
    {
        var result = await _controller.Authorize(new PumpAuthorizeRequest { Uid = "CUSTUID" });
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var resp = (PumpAuthorizeResponse)((OkObjectResult)result.Result!).Value!;
        Assert.That(resp.Role, Is.EqualTo("Пользователь"));
        Assert.That(_service.Validate(resp.Token!), Is.True);
    }

    [Test]
    public async Task Authorize_ReturnsOperatorRole()
    {
        var result = await _controller.Authorize(new PumpAuthorizeRequest { Uid = "OPUID" });
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var resp = (PumpAuthorizeResponse)((OkObjectResult)result.Result!).Value!;
        Assert.That(resp.Role, Is.EqualTo("Оператор"));
        Assert.That(resp.VirtualTanks, Is.Not.Null);
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
