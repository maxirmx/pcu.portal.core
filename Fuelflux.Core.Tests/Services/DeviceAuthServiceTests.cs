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

    private PumpController _pump = null!;
    private User _user = null!;

    [SetUp]
    public void Setup()
    {
        var opts = Options.Create(new DeviceAuthSettings { SessionMinutes = 1 });
        var appOpts = Options.Create(new AppSettings { Secret = "secret" });
        var logger = new LoggerFactory().CreateLogger<DeviceAuthService>();
        _service = new DeviceAuthService(opts, appOpts, logger);

        _pump = new PumpController
        {
            Id = 1,
            Uid = Guid.NewGuid().ToString(),
            FuelStationId = 1,
            FuelStation = new FuelStation { Id = 1, Name = "fs" }
        };

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
        var pump = new PumpController
        {
            Id = 2,
            Uid = Guid.NewGuid().ToString(),
            FuelStationId = 2,
            FuelStation = new FuelStation { Id = 2, Name = "fs" }
        };
        var role = new Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "op" };
        var user = new User
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
