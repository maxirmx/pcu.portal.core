// Copyright (C) 2025 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of Fuelflux core application
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
public class PumpControllerUserListTests
{
    private PumpController _controller = null!;
    private DeviceAuthService _service = null!;
    private AppDbContext _dbContext = null!;
    private User _controllerUser = null!;
    
    // Common roles for reuse across tests
    private Role _controllerRole = null!;
    private Role _adminRole = null!;
    private Role _operatorRole = null!;
    private Role _customerRole = null!;

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

        // Create all common roles
        _controllerRole = new Role { Id = 1, RoleId = UserRoleConstants.Controller, Name = "ctrl" };
        _adminRole = new Role { Id = 2, RoleId = UserRoleConstants.Admin, Name = "admin" };
        _operatorRole = new Role { Id = 3, RoleId = UserRoleConstants.Operator, Name = "op" };
        _customerRole = new Role { Id = 4, RoleId = UserRoleConstants.Customer, Name = "cust" };
        
        _controllerUser = new User
        {
            Id = 1,
            Email = "c@c.c",
            Password = "p",
            Uid = "ctrluid",
            RoleId = _controllerRole.Id,
            Role = _controllerRole
        };
        
        _dbContext.Roles.AddRange(_controllerRole, _adminRole, _operatorRole, _customerRole);
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
        var op1 = new User { Id = 2, Email = "o1@a", Password = "p", Uid = "op1", RoleId = _operatorRole.Id, Role = _operatorRole };
        var cust = new User { Id = 3, Email = "c@a", Password = "p", Uid = "cust1", Allowance = 10m, RoleId = _customerRole.Id, Role = _customerRole };
        var op2 = new User { Id = 4, Email = "o2@a", Password = "p", Uid = "op2", RoleId = _operatorRole.Id, Role = _operatorRole };
        _dbContext.Users.AddRange(op1, cust, op2);
        _dbContext.SaveChanges();

        var req = new PumpUserRequest { First = 1, Number = 2 };
        var result = await _controller.GetPumpUsers(req);

        Assert.That(result.Value, Is.Not.Null);
        var items = result.Value!.ToList();
        Assert.That(items.Count, Is.EqualTo(2));
        
        // First user should be customer (has allowance)
        Assert.That(items[0].Uid, Is.EqualTo(cust.Uid));
        Assert.That(items[0].Allowance, Is.EqualTo(10m));
        Assert.That(items[0].RoleId, Is.EqualTo((int)UserRoleConstants.Customer));
        
        // Second user should be operator (no allowance)
        Assert.That(items[1].Uid, Is.EqualTo(op2.Uid));
        Assert.That(items[1].Allowance, Is.Null);
        Assert.That(items[1].RoleId, Is.EqualTo((int)UserRoleConstants.Operator));
    }

    [Test]
    public async Task GetPumpUsers_ReturnsForbidden_WhenNotController()
    {
        var opUser = new User { Id = 2, Email = "o1@a", Password = "p", Uid = "op1", RoleId = _operatorRole.Id, Role = _operatorRole };
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
        _dbContext.Users.Add(new User { Id = 2, Email = "o1@a", Password = "p", Uid = "op1", RoleId = _operatorRole.Id, Role = _operatorRole });
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

    [Test]
    public async Task GetPumpUsers_ReturnsCorrectRoleIds_ForDifferentUserTypes()
    {
        // Arrange
        var operator1 = new User { Id = 2, Email = "op1@test.com", Password = "p", Uid = "operator1", RoleId = _operatorRole.Id, Role = _operatorRole };
        var customer1 = new User { Id = 3, Email = "cust1@test.com", Password = "p", Uid = "customer1", Allowance = 50.0m, RoleId = _customerRole.Id, Role = _customerRole };
        
        _dbContext.Users.AddRange(operator1, customer1);
        _dbContext.SaveChanges();

        var req = new PumpUserRequest { First = 0, Number = 10 };
        
        // Act
        var result = await _controller.GetPumpUsers(req);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        var items = result.Value!.ToList();
        Assert.That(items.Count, Is.EqualTo(2));

        // Verify operator user
        var operatorItem = items.First(i => i.Uid == "operator1");
        Assert.That(operatorItem.RoleId, Is.EqualTo((int)UserRoleConstants.Operator));
        Assert.That(operatorItem.Allowance, Is.Null);
        
        // Verify customer user
        var customerItem = items.First(i => i.Uid == "customer1");
        Assert.That(customerItem.RoleId, Is.EqualTo((int)UserRoleConstants.Customer));
        Assert.That(customerItem.Allowance, Is.EqualTo(50.0m));
    }

    [Test]
    public async Task GetPumpUsers_ExcludesAdminUsers_FromResults()
    {
        // Arrange
        var admin = new User { Id = 2, Email = "admin@test.com", Password = "p", Uid = "admin1", RoleId = _adminRole.Id, Role = _adminRole };
        var operator1 = new User { Id = 3, Email = "op1@test.com", Password = "p", Uid = "operator1", RoleId = _operatorRole.Id, Role = _operatorRole };
        
        _dbContext.Users.AddRange(admin, operator1);
        _dbContext.SaveChanges();

        var req = new PumpUserRequest { First = 0, Number = 10 };
        
        // Act
        var result = await _controller.GetPumpUsers(req);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        var items = result.Value!.ToList();
        
        // Only operator should be returned, admin should be excluded
        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].Uid, Is.EqualTo("operator1"));
        Assert.That(items[0].RoleId, Is.EqualTo((int)UserRoleConstants.Operator));
        
        // Verify no admin users in results
        Assert.That(items.Any(i => i.RoleId == (int)UserRoleConstants.Admin), Is.False);
    }

    [Test]
    public void PumpUserItem_CanBeInstantiated_WithAllProperties()
    {
        // Arrange & Act
        var pumpUserItem = new PumpUserItem
        {
            Uid = "test-uid-123",
            Allowance = 100.50m,
            RoleId = (int)UserRoleConstants.Customer
        };

        // Assert
        Assert.That(pumpUserItem.Uid, Is.EqualTo("test-uid-123"));
        Assert.That(pumpUserItem.Allowance, Is.EqualTo(100.50m));
        Assert.That(pumpUserItem.RoleId, Is.EqualTo((int)UserRoleConstants.Customer));
    }

    [Test]
    public void PumpUserItem_AllowsNullAllowance_WithRoleId()
    {
        // Arrange & Act
        var pumpUserItem = new PumpUserItem
        {
            Uid = "operator-uid",
            Allowance = null,
            RoleId = (int)UserRoleConstants.Operator
        };

        // Assert
        Assert.That(pumpUserItem.Uid, Is.EqualTo("operator-uid"));
        Assert.That(pumpUserItem.Allowance, Is.Null);
        Assert.That(pumpUserItem.RoleId, Is.EqualTo((int)UserRoleConstants.Operator));
    }
}
