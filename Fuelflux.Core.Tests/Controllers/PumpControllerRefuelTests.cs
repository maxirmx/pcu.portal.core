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
public class PumpControllerRefuelTests
{
    private PumpController _controller = null!;
    private DeviceAuthService _service = null!;
    private AppDbContext _dbContext = null!;
    private PumpCntrl _pump = null!;
    private User _operator = null!;

    [SetUp]
    public void Setup()
    {
        var dbOpts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pump_controller_refuel_test_db_{Guid.NewGuid()}")
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
        _operator = new User { Id = 1, Email = "a@b.c", Password = "p", Uid = "uid", RoleId = role.Id, Role = role };
        _dbContext.FuelStations.Add(fs);
        _dbContext.PumpControllers.Add(_pump);
        _dbContext.FuelTanks.AddRange(tank1, tank2);
        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(_operator);
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

    private (Role, User) CreateCustomer(int id, decimal allowance)
    {
        var role = new Role { Id = id, RoleId = UserRoleConstants.Customer, Name = "cust" };
        var customer = new User { Id = id, Email = $"c{id}@c.c", Password = "p", Uid = $"cust{id}", RoleId = role.Id, Role = role, Allowance = allowance };
        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(customer);
        _dbContext.SaveChanges();
        return (role, customer);
    }

    [Test]
    public async Task Refuel_SubtractsVolume_WhenCustomer()
    {
        var (_, customer) = CreateCustomer(2, 200m);

        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;

        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = 100m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var tank = _dbContext.FuelTanks.First(t => t.Id == 1);
        var userFromDb = _dbContext.Users.First(u => u.Id == customer.Id);
        Assert.That(tank.Allowance, Is.EqualTo(400m));
        Assert.That(userFromDb.Allowance, Is.EqualTo(100m));
    }

    [Test]
    public async Task Refuel_ReturnsNotFound_WhenTankMissing()
    {
        var (_, customer) = CreateCustomer(3, 50m);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;

        var req = new RefuelRequest { TankNumber = 99, RefuelVolume = 10m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task Refuel_ReturnsForbidden_WhenUserNotCustomer()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = _operator.Uid;

        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = 10m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Refuel_ReturnsBadRequest_WhenRequestIsNull()
    {
        var (_, customer) = CreateCustomer(4, 10m);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;

        var result = await _controller.Refuel(null!);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task Refuel_ReturnsBadRequest_WhenModelStateInvalid()
    {
        var (_, customer) = CreateCustomer(9, 50m);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;
        _controller.ModelState.AddModelError("TankNumber", "required");

        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = 10m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task Refuel_ReturnsForbidden_WhenUserUidMissing()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = 10m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Refuel_ReturnsForbidden_WhenPumpControllerUidMissing()
    {
        var (_, customer) = CreateCustomer(5, 20m);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;
        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = 10m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Refuel_ReturnsForbidden_WhenUserNotFound()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = "missing";
        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = 10m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Refuel_ReturnsForbidden_WhenPumpNotFound()
    {
        var (_, customer) = CreateCustomer(6, 20m);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = "wrong";
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;
        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = 10m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = result as ObjectResult;
        Assert.That(obj!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Refuel_UpdatesCorrectTank_WithValidInput()
    {
        var (_, customer) = CreateCustomer(7, 300m);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;

        var initialVolume = _dbContext.FuelTanks.First(t => t.Number == 2).Allowance;
        var req = new RefuelRequest { TankNumber = 2, RefuelVolume = 50.5m };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var tank = _dbContext.FuelTanks.First(t => t.Number == 2);
        Assert.That(tank.Allowance, Is.EqualTo(initialVolume - 50.5m));
    }

    [Test]
    public async Task Refuel_HandlesLargeVolumeCorrectly()
    {
        var (_, customer) = CreateCustomer(8, 20000m);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.ControllerContext.HttpContext.Items["PumpControllerUid"] = _pump.Uid;
        _controller.ControllerContext.HttpContext.Items["UserUid"] = customer.Uid;

        var initialVolume = _dbContext.FuelTanks.First(t => t.Number == 1).Allowance;
        var largeVolume = 9999.99m;
        var req = new RefuelRequest { TankNumber = 1, RefuelVolume = largeVolume };
        var result = await _controller.Refuel(req);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var tank = _dbContext.FuelTanks.First(t => t.Number == 1);
        Assert.That(tank.Allowance, Is.EqualTo(initialVolume - largeVolume));
    }

    [Test]
    public void RefuelRequest_Validation_RequiresTankNumber()
    {
        var request = new RefuelRequest { RefuelVolume = 10m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(RefuelRequest.TankNumber))), Is.True);
    }

    [Test]
    public void RefuelRequest_Validation_RequiresPositiveTankNumber()
    {
        var request = new RefuelRequest { TankNumber = 0, RefuelVolume = 10m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(RefuelRequest.TankNumber)) &&
                                    r.ErrorMessage!.Contains("номер резервуара должен быть положительным числом")), Is.True);
    }

    [Test]
    public void RefuelRequest_Validation_RequiresRefuelVolume()
    {
        var request = new RefuelRequest { TankNumber = 1 };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(RefuelRequest.RefuelVolume))), Is.True);
    }

    [Test]
    public void RefuelRequest_Validation_RequiresPositiveRefuelVolume()
    {
        var request = new RefuelRequest { TankNumber = 1, RefuelVolume = 0m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(RefuelRequest.RefuelVolume)) &&
                                    r.ErrorMessage!.Contains("объем заправленного топлива должен быть положительным числом")), Is.True);
    }

    [Test]
    public void RefuelRequest_Validation_AcceptsValidInput()
    {
        var request = new RefuelRequest { TankNumber = 1, RefuelVolume = 15.5m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }
}
