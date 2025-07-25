using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;
using Fuelflux.Core.Settings;
using Fuelflux.Core.Controllers;
using Fuelflux.Core.Data;
using Fuelflux.Core.Models;

namespace Fuelflux.Core.Tests.Controllers;

[TestFixture]
public class PumpControllerUserListTests
{
    private PumpController _controller = null!;
    private DeviceAuthService _service = null!;
    private AppDbContext _dbContext = null!;
    private User _controllerUser = null!;

    [SetUp]
    public void Setup()
    {
        var dbOpts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pump_controller_user_list_test_db_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(dbOpts);

        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 1 });
        var appOpts = Options.Create(new AppSettings { Secret = "secret" });
        _service = new DeviceAuthService(opts, appOpts, new LoggerFactory().CreateLogger<DeviceAuthService>());

        var ctrlRole = new Role { Id = 1, RoleId = UserRoleConstants.Controller, Name = "ctrl" };
        _controllerUser = new User
        {
            Id = 1,
            Email = "c@c.c",
            Password = "p",
            Uid = "ctrluid",
            RoleId = ctrlRole.Id,
            Role = ctrlRole
        };
        _dbContext.Roles.Add(ctrlRole);
        _dbContext.Users.Add(_controllerUser);
        _dbContext.SaveChanges();

        _controller = new PumpController(_service, _dbContext, new LoggerFactory().CreateLogger<PumpController>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _controllerUser.Uid;
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetPumpUsers_ReturnsUsers_WhenController()
    {
        var opRole = new Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "op" };
        var custRole = new Role { Id = 3, RoleId = UserRoleConstants.Customer, Name = "cust" };
        var op1 = new User { Id = 2, Email = "o1@a", Password = "p", Uid = "op1", RoleId = opRole.Id, Role = opRole };
        var cust = new User { Id = 3, Email = "c@a", Password = "p", Uid = "cust1", Allowance = 10m, RoleId = custRole.Id, Role = custRole };
        var op2 = new User { Id = 4, Email = "o2@a", Password = "p", Uid = "op2", RoleId = opRole.Id, Role = opRole };
        _dbContext.Roles.AddRange(opRole, custRole);
        _dbContext.Users.AddRange(op1, cust, op2);
        _dbContext.SaveChanges();

        var req = new PumpUserRequest { First = 1, Number = 2 };
        var result = await _controller.GetPumpUsers(req);

        Assert.That(result.Value, Is.Not.Null);
        var items = result.Value!.ToList();
        Assert.That(items.Count, Is.EqualTo(2));
        Assert.That(items[0].Uid, Is.EqualTo(cust.Uid));
        Assert.That(items[0].Allowance, Is.EqualTo(10m));
        Assert.That(items[1].Uid, Is.EqualTo(op2.Uid));
        Assert.That(items[1].Allowance, Is.Null);
    }

    [Test]
    public async Task GetPumpUsers_ReturnsForbidden_WhenNotController()
    {
        var opRole = new Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "op" };
        var opUser = new User { Id = 2, Email = "o1@a", Password = "p", Uid = "op1", RoleId = opRole.Id, Role = opRole };
        _dbContext.Roles.Add(opRole);
        _dbContext.Users.Add(opUser);
        _dbContext.SaveChanges();

        _controller.ControllerContext.HttpContext.Items["UserUid"] = opUser.Uid;
        var req = new PumpUserRequest { First = 0, Number = 1 };
        var result = await _controller.GetPumpUsers(req);

        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task GetPumpUsers_ReturnsNoContent_WhenRangeEmpty()
    {
        var opRole = new Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "op" };
        var custRole = new Role { Id = 3, RoleId = UserRoleConstants.Customer, Name = "cust" };
        _dbContext.Roles.AddRange(opRole, custRole);
        _dbContext.Users.Add(new User { Id = 2, Email = "o1@a", Password = "p", Uid = "op1", RoleId = opRole.Id, Role = opRole });
        _dbContext.SaveChanges();

        var req = new PumpUserRequest { First = 10, Number = 5 };
        var result = await _controller.GetPumpUsers(req);

        Assert.That(result.Result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task GetPumpUsers_ReturnsBadRequest_WhenModelStateInvalid()
    {
        _controller.ModelState.AddModelError("First", "required");
        var req = new PumpUserRequest { First = 0, Number = 1 };
        var result = await _controller.GetPumpUsers(req);

        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task GetPumpUsers_ReturnsForbidden_WhenUserUidMissing()
    {
        _controller.ControllerContext.HttpContext.Items.Remove("UserUid");
        var req = new PumpUserRequest { First = 0, Number = 1 };
        var result = await _controller.GetPumpUsers(req);

        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public void PumpUserRequest_Validation_RequiresNonNegativeFirst()
    {
        var request = new PumpUserRequest { First = -1, Number = 1 };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(PumpUserRequest.First))), Is.True);
    }

    [Test]
    public void PumpUserRequest_Validation_RequiresPositiveNumber()
    {
        var request = new PumpUserRequest { First = 0, Number = 0 };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(PumpUserRequest.Number))), Is.True);
    }

    [Test]
    public void PumpUserRequest_Validation_AcceptsValidInput()
    {
        var request = new PumpUserRequest { First = 0, Number = 5 };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }
}
