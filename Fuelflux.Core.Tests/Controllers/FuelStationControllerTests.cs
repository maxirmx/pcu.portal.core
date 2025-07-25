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
        var update = new FuelTankItem { Number = 1 };
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

    [Test]
    public async Task GetStations_ReturnsList_WhenAdmin()
    {
        var fs1 = new FuelStation { Id = 1, Name = "fs1" };
        var fs2 = new FuelStation { Id = 2, Name = "fs2" };
        _db.FuelStations.AddRange(fs1, fs2);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.GetStations();
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetStations_ReturnsForbidden_WhenNotAdmin()
    {
        SetCurrentUser(2);
        _uis.Setup(u => u.CheckAdmin(2)).ReturnsAsync(false);
        var result = await _controller.GetStations();
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task GetStation_ReturnsStation_WhenAdmin()
    {
        var fs = new FuelStation { Id = 3, Name = "fs" };
        _db.FuelStations.Add(fs);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.GetStation(3);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Name, Is.EqualTo("fs"));
    }

    [Test]
    public async Task GetStation_ReturnsNotFound_WhenMissing()
    {
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.GetStation(99);
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task PutStation_UpdatesName_WhenAdmin()
    {
        var fs = new FuelStation { Id = 4, Name = "old" };
        _db.FuelStations.Add(fs);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var update = new FuelStationItem { Name = "new" };
        var result = await _controller.PutStation(4, update);
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_db.FuelStations.Find(4)!.Name, Is.EqualTo("new"));
    }

    [Test]
    public async Task DeleteStation_RemovesStation_WhenAdmin()
    {
        var fs = new FuelStation { Id = 5, Name = "fs" };
        _db.FuelStations.Add(fs);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.DeleteStation(5);
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_db.FuelStations.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetTank_ReturnsTank_WhenAdmin()
    {
        var fs = new FuelStation { Id = 6, Name = "fs" };
        var tank = new FuelTank { Id = 1, Number = 1, Volume = 10m, FuelStationId = 6, FuelStation = fs };
        _db.FuelStations.Add(fs);
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.GetTank(1);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Number, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTank_ReturnsNotFound_WhenMissing()
    {
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.GetTank(42);
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task PostTank_CreatesTank_WhenAdmin()
    {
        var fs = new FuelStation { Id = 7, Name = "fs" };
        _db.FuelStations.Add(fs);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var item = new FuelTankItem { Number = 2, Volume = 5m, FuelStationId = 7 };
        var result = await _controller.PostTank(item);
        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        Assert.That(_db.FuelTanks.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task PutTank_UpdatesTank_WhenAdmin()
    {
        var fs = new FuelStation { Id = 8, Name = "fs" };
        var tank = new FuelTank { Id = 3, Number = 3, Volume = 1m, FuelStationId = 8, FuelStation = fs };
        _db.FuelStations.Add(fs);
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var update = new FuelTankItem { Volume = 2m };
        var result = await _controller.PutTank(3, update);
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_db.FuelTanks.Find(3)!.Volume, Is.EqualTo(2m));
    }

    [Test]
    public async Task DeleteTank_RemovesTank_WhenAdmin()
    {
        var fs = new FuelStation { Id = 9, Name = "fs" };
        var tank = new FuelTank { Id = 4, Number = 4, Volume = 2m, FuelStationId = 9, FuelStation = fs };
        _db.FuelStations.Add(fs);
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.DeleteTank(4);
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_db.FuelTanks.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task PostPump_CreatesPump_WhenAdmin()
    {
        var fs = new FuelStation { Id = 10, Name = "fs" };
        _db.FuelStations.Add(fs);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var item = new PumpControllerItem { Uid = "uid", FuelStationId = 10 };
        var result = await _controller.PostPump(item);
        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        Assert.That(_db.PumpControllers.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task PutPump_UpdatesUid_WhenAdmin()
    {
        var fs = new FuelStation { Id = 11, Name = "fs" };
        var pump = new PumpCntrl { Id = 1, Uid = "old", FuelStationId = 11, FuelStation = fs };
        _db.FuelStations.Add(fs);
        _db.PumpControllers.Add(pump);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var update = new PumpControllerItem { Uid = "new" };
        var result = await _controller.PutPump(1, update);
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_db.PumpControllers.Find(1)!.Uid, Is.EqualTo("new"));
    }

    [Test]
    public async Task DeletePump_RemovesPump_WhenAdmin()
    {
        var fs = new FuelStation { Id = 12, Name = "fs" };
        var pump = new PumpCntrl { Id = 2, Uid = "uid", FuelStationId = 12, FuelStation = fs };
        _db.FuelStations.Add(fs);
        _db.PumpControllers.Add(pump);
        await _db.SaveChangesAsync();
        SetCurrentUser(1);
        _uis.Setup(u => u.CheckAdmin(1)).ReturnsAsync(true);
        var result = await _controller.DeletePump(2);
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_db.PumpControllers.Count(), Is.EqualTo(0));
    }
}
