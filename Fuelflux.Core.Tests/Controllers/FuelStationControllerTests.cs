using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Fuelflux.Core.Controllers;
using Fuelflux.Core.Data;
using Fuelflux.Core.Models;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fuelflux.Core.Tests.Controllers;

[TestFixture]
public class FuelStationControllerTests
{
#pragma warning disable CS8618
    private AppDbContext _db;
    private Mock<IHttpContextAccessor> _http;
    private Mock<IUserInformationService> _uis;
    private Mock<ILogger<FuelStationController>> _logger;
    private FuelStationController _controller;
#pragma warning restore CS8618

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"fuelstation_controller_test_db_{System.Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _http = new Mock<IHttpContextAccessor>();
        _uis = new Mock<IUserInformationService>();
        _logger = new Mock<ILogger<FuelStationController>>();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SetCurrentUser(int id)
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["UserId"] = id;
        _http.Setup(x => x.HttpContext).Returns(ctx);
        _controller = new FuelStationController(_http.Object, _db, _uis.Object, _logger.Object);
    }

    [Test]
    public async Task PostStation_CreatesStation_WhenAdmin()
    {
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var item = new FuelStationItem { Name = "FS" };
        var result = await _controller.PostStation(item);
        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        Assert.That(_db.FuelStations.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task PostStation_ReturnsForbidden_WhenNotAdmin()
    {
        SetCurrentUser(2);
        _uis.Setup(u => u.CheckAdmin(2)).ReturnsAsync(false);
        var item = new FuelStationItem { Name = "FS" };
        var result = await _controller.PostStation(item);
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task PutTank_ReturnsNotFound_WhenTankMissing()
    {
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var update = new FuelTankViewItem { Number = 1 };
        var result = await _controller.PutTank(1, update);
        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task GetPumps_ReturnsList_WhenAdmin()
    {
        var fs = new FuelStation { Id = 1, Name = "fs" };
        var pump = new PumpCntrl { Id = 1, Uid = "u", FuelStationId = 1, FuelStation = fs };
        _db.FuelStations.Add(fs);
        _db.PumpControllers.Add(pump);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.GetPumps();
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetPumps_ReturnsForbidden_WhenNotAdmin()
    {
        SetCurrentUser(2);
        _uis.Setup(u => u.CheckAdmin(2)).ReturnsAsync(false);
        var result = await _controller.GetPumps();
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }
}
