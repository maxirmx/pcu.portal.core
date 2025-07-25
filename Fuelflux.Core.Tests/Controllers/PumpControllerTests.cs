// Copyright (C) 2025 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of Fuelflux Core application
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.


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
public class PumpControllerTests
{
    private PumpController _controller = null!;
    private DeviceAuthService _service = null!;
    private AppDbContext _dbContext = null!;
    private PumpCntrl _pump = null!;
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
        _pump = new PumpCntrl { Id = 1, Uid = Guid.NewGuid().ToString(), FuelStationId = fs.Id, FuelStation = fs };
        var tank1 = new FuelTank { Id = 1, Number = 1, Allowance = 500m, FuelStationId = fs.Id, FuelStation = fs };
        var tank2 = new FuelTank { Id = 2, Number = 2, Allowance = 1000m, FuelStationId = fs.Id, FuelStation = fs };
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
        _dbContext.FuelTanks.AddRange(tank1, tank2);
        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(_user);
        _dbContext.SaveChanges();

        _controller = new PumpController(
            _service,
            _dbContext,
            new LoggerFactory().CreateLogger<PumpController>());
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
        var req = new DeviceAuthorizeRequest { PumpControllerUid = _pump.Uid.ToString(), UserUid = _user.Uid! };
        var result = await _controller.Authorize(req);
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var response = (result.Result as OkObjectResult)!.Value as DeviceAuthorizeResponse;
        Assert.That(response, Is.Not.Null);
        var token = response!.Token;
        var validationResult = _service.Validate(token);
        Assert.That(validationResult, Is.Not.Null);
        Assert.That(validationResult!.PumpControllerUid, Is.EqualTo(_pump.Uid));
        Assert.That(validationResult.UserUid, Is.EqualTo(_user.Uid));
        Assert.That(response.RoleId, Is.EqualTo((int)_user.Role!.RoleId));
        Assert.That(response.FuelTanks.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task Deauthorize_RemovesToken()
    {
        // Arrange - Create a real token
        var token = _service.Authorize(_pump, _user);
        
        // Set up HTTP context with the Authorization header
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";

        // Simulate what the JWT middleware would do - populate the context
        // In real app, this happens automatically in the middleware pipeline
        _controller.ControllerContext.HttpContext.Items["Token"] = token;
        _controller.ControllerContext.HttpContext.Items["TokenType"] = "Device";
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;

        // Act - Call the controller action
        var res = _controller.Deauthorize();
        await Task.Delay(1);

        // Assert
        Assert.That(res, Is.TypeOf<NoContentResult>());
        Assert.That(_service.Validate(token), Is.Null);
    }

    [Test]
    public async Task Authorize_ReturnsUnauthorized_WhenPumpMissing()
    {
        var req = new DeviceAuthorizeRequest { PumpControllerUid = Guid.NewGuid().ToString(), UserUid = _user.Uid! };
        var result = await _controller.Authorize(req);
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Authorize_ReturnsUnauthorized_WhenUserMissing()
    {
        var req = new DeviceAuthorizeRequest { PumpControllerUid = _pump.Uid.ToString(), UserUid = "wrong" };
        var result = await _controller.Authorize(req);
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Authorize_ReturnsAllowance_ForCustomer()
    {
        var role = new Role { Id = 2, RoleId = UserRoleConstants.Customer, Name = "cust" };
        var customer = new User
        {
            Id = 2,
            Email = "c@c.c",
            Password = "p",
            FirstName = "",
            LastName = "",
            Patronymic = "",
            Uid = "custuid",
            RoleId = role.Id,
            Role = role,
            Allowance = 42m
        };
        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(customer);
        _dbContext.SaveChanges();

        var req = new DeviceAuthorizeRequest { PumpControllerUid = _pump.Uid.ToString(), UserUid = customer.Uid! };
        var result = await _controller.Authorize(req);
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var response = (result.Result as OkObjectResult)!.Value as DeviceAuthorizeResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Allowance, Is.EqualTo(customer.Allowance));
        Assert.That(response!.Price, Is.Not.Null);
    }

    [Test]
    public async Task Authorize_ReturnsForbidden_WhenRoleNotAllowed()
    {
        var role = new Role { Id = 3, RoleId = UserRoleConstants.Admin, Name = "admin" };
        var admin = new User { Id = 3, Email = "adm@c.c", Password = "p", Uid = "admin", RoleId = role.Id, Role = role };
        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(admin);
        _dbContext.SaveChanges();

        var req = new DeviceAuthorizeRequest { PumpControllerUid = _pump.Uid.ToString(), UserUid = admin.Uid! };
        var result = await _controller.Authorize(req);

        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        var obj = result.Result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task FuelIntake_AddsVolume_WhenOperator()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;

        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 100m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var tank = _dbContext.FuelTanks.First(t => t.Id == 1);
        Assert.That(tank.Allowance, Is.EqualTo(600m));
    }

    [Test]
    public async Task FuelIntake_ReturnsNotFound_WhenTankMissing()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;

        var req = new FuelIntakeRequest { TankNumber = 99, IntakeVolume = 50m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task FuelIntake_ReturnsForbidden_WhenUserNotOperator()
    {
        var role = new Role { Id = 2, RoleId = UserRoleConstants.Customer, Name = "cust" };
        var customer = new User { Id = 2, Email = "c@c.c", Password = "p", Uid = "cust", RoleId = role.Id, Role = role };
        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(customer);
        _dbContext.SaveChanges();

        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;

        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 10m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task FuelIntake_ReturnsBadRequest_WhenRequestIsNull()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;

        var result = await _controller.FuelIntake(null!);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task FuelIntake_ReturnsBadRequest_WhenModelStateInvalid()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;
        _controller.ModelState.AddModelError("TankNumber", "required");

        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 10m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }


    [Test]
    public async Task FuelIntake_ReturnsForbidden_WhenUserUidMissing()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        // UserUid is intentionally not set

        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 100m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task FuelIntake_ReturnsForbidden_WhenPumpControllerUidMissing()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;
        // PumpControllerUid is intentionally not set

        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 100m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task FuelIntake_ReturnsForbidden_WhenUserNotFound()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = "nonexistent-user-uid";

        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 100m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task FuelIntake_ReturnsForbidden_WhenPumpNotFound()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = "nonexistent-pump-uid";
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;

        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 100m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task FuelIntake_UpdatesCorrectTank_WithValidInput()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;

        var initialVolume = _dbContext.FuelTanks.First(t => t.Number == 2).Allowance;
        var req = new FuelIntakeRequest { TankNumber = 2, IntakeVolume = 250.5m };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var tank = _dbContext.FuelTanks.First(t => t.Number == 2);
        Assert.That(tank.Allowance, Is.EqualTo(initialVolume + 250.5m));
    }

    [Test]
    public async Task FuelIntake_HandlesLargeVolumeCorrectly()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _user.Uid;

        var initialVolume = _dbContext.FuelTanks.First(t => t.Number == 1).Allowance;
        var largeVolume = 9999.99m;
        var req = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = largeVolume };
        var result = await _controller.FuelIntake(req);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var tank = _dbContext.FuelTanks.First(t => t.Number == 1);
        Assert.That(tank.Allowance, Is.EqualTo(initialVolume + largeVolume));
    }

    [Test]
    public void FuelIntakeRequest_Validation_RequiresTankNumber()
    {
        var request = new FuelIntakeRequest { IntakeVolume = 100m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(FuelIntakeRequest.TankNumber))), Is.True);
    }

    [Test]
    public void FuelIntakeRequest_Validation_RequiresPositiveTankNumber()
    {
        var request = new FuelIntakeRequest { TankNumber = 0, IntakeVolume = 100m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(FuelIntakeRequest.TankNumber)) && 
                                    r.ErrorMessage!.Contains("положительным числом")), Is.True);
    }

    [Test]
    public void FuelIntakeRequest_Validation_RequiresIntakeVolume()
    {
        var request = new FuelIntakeRequest { TankNumber = 1 };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(FuelIntakeRequest.IntakeVolume))), Is.True);
    }

    [Test]
    public void FuelIntakeRequest_Validation_RequiresPositiveIntakeVolume()
    {
        var request = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 0m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(FuelIntakeRequest.IntakeVolume)) && 
                                    r.ErrorMessage!.Contains("объем принятого топлива должен быть положительным числом")), Is.True);
    }

    [Test]
    public void FuelIntakeRequest_Validation_AcceptsValidInput()
    {
        var request = new FuelIntakeRequest { TankNumber = 1, IntakeVolume = 100.5m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }
}
