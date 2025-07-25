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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Fuelflux.Core.Authorization;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;
using Fuelflux.Core.Data;

namespace Fuelflux.Core.Controllers;

[ApiController]
[Authorize(AuthorizationType.Device)]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrMessage))]

public class PumpController(IDeviceAuthService authService, AppDbContext db, ILogger<PumpController> logger) :
    FuelfluxControllerPreBase(db, logger)
{
    private readonly IDeviceAuthService _authService = authService;

    [HttpPost("authorize")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeviceAuthorizeResponse))]
    public async Task<ActionResult<DeviceAuthorizeResponse>> Authorize(DeviceAuthorizeRequest request)
    {
        var pump = await _db.PumpControllers
            .AsNoTracking()
            .Include(p => p.FuelStation)
                .ThenInclude(fs => fs.FuelTanks)
            .FirstOrDefaultAsync(p => p.Uid == request.PumpControllerUid);
        var user = await _db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Uid == request.UserUid);

        if (pump == null || user == null || !(user.IsOperator() || user.IsCustomer() || user.IsController()))
        {
            return _403();
        }

        var token = _authService.Authorize(pump, user);

        var fuelTanks = pump.FuelStation.FuelTanks
            .Select(ft => new FuelTankItem { Number = ft.Number, Volume = ft.Allowance })
            .ToList();

        var response = new DeviceAuthorizeResponse
        {
            Token = token,
            RoleId = (int)user.Role!.RoleId,
            FuelTanks = fuelTanks,
            Allowance = user.IsCustomer() ? user.Allowance : null,
            Price = user.IsCustomer() ? 63.09m : null
        };

        return Ok(response);
    }

    [HttpPost("deauthorize")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Deauthorize()
    {
        var token = HttpContext.Items["Token"] as string;
        if (token is not null) _authService.Deauthorize(token);
        return NoContent();
    }

    [HttpPost("fuelintake")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> FuelIntake(FuelIntakeRequest request)
    {
        // Validate request parameter
        if (request == null)
        {
            return _400();
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .FirstOrDefault()?.ErrorMessage ?? "неизвестная проблема.";
            
            _logger.LogWarning("Model validation failed: {Error}", firstError);
            return _400Intake(firstError);
        }

        var userUid = HttpContext.Items["UserUid"] as string;
        if (userUid == null)
        {
            return _403();
        }

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Uid == userUid);
        if (user == null || !user.IsOperator())
        {
            return _403();
        }

        var pumpUid = HttpContext.Items["PumpControllerUid"] as string;
        if (pumpUid == null)
        {
            return _403();
        }

        var pump = await _db.PumpControllers
            .Include(p => p.FuelStation)
                .ThenInclude(fs => fs.FuelTanks)
            .FirstOrDefaultAsync(p => p.Uid == pumpUid);

        if (pump == null)
        {
            return _403();
        }

        var tank = pump.FuelStation.FuelTanks.FirstOrDefault(t => t.Number == request.TankNumber);
        if (tank == null)
        {
            return _404FuelTank(request.TankNumber);
        }

        tank.Allowance += request.IntakeVolume;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Fuel intake successful: Tank {TankNumber}, Volume {IntakeVolume}, New Total {NewTotal}", 
            request.TankNumber, request.IntakeVolume, tank.Allowance);

        return NoContent();
    }

    [HttpPost("refuel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> Refuel(RefuelRequest request)
    {
        if (request == null)
        {
            return _400();
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .FirstOrDefault()?.ErrorMessage ?? "неизвестная проблема.";

            _logger.LogWarning("Model validation failed: {Error}", firstError);
            return _400Refuel(firstError);
        }

        var userUid = HttpContext.Items["UserUid"] as string;
        if (userUid == null)
        {
            return _403();
        }

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Uid == userUid);
        if (user == null || !user.IsCustomer())
        {
            return _403();
        }

        var pumpUid = HttpContext.Items["PumpControllerUid"] as string;
        if (pumpUid == null)
        {
            return _403();
        }

        var pump = await _db.PumpControllers
            .Include(p => p.FuelStation)
                .ThenInclude(fs => fs.FuelTanks)
            .FirstOrDefaultAsync(p => p.Uid == pumpUid);

        if (pump == null)
        {
            return _403();
        }

        var tank = pump.FuelStation.FuelTanks.FirstOrDefault(t => t.Number == request.TankNumber);
        if (tank == null)
        {
            return _404FuelTank(request.TankNumber);
        }

        tank.Allowance -= request.RefuelVolume;
        if (user.Allowance != null)
        {
            user.Allowance -= request.RefuelVolume;
        }
        await _db.SaveChangesAsync();

        _logger.LogInformation("Refuel successful: Tank {TankNumber}, Volume {RefuelVolume}, Tank Left {TankLeft}",
            request.TankNumber, request.RefuelVolume, tank.Allowance);

        return NoContent();
    }

    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PumpUserItem>))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<IEnumerable<PumpUserItem>>> GetPumpUsers(int first, int number)
    {
        var userUid = HttpContext.Items["UserUid"] as string;
        if (userUid == null)
        {
            return _403();
        }

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Uid == userUid);
        if (user == null || !user.IsController())
        {
            return _403();
        }

        var res = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role != null && (u.IsCustomer() || u.IsOperator()))
            .OrderBy(u => u.Id)
            .Skip(first)
            .Take(number)
            .Select(u => new PumpUserItem
            {
                Uid = u.Uid!,
                Allowance = u.IsCustomer() ? u.Allowance : null
            })
            .ToListAsync();

        if (res.Count == 0)
        {
            return NoContent();
        }

        return res;
    }
}
