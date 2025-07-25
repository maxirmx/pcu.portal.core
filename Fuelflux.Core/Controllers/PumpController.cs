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

public class PumpController(IDeviceAuthService authService, AppDbContext db, ILogger<PumpController> logger) : ControllerBase
{
    private readonly IDeviceAuthService _authService = authService;
    private readonly AppDbContext _db = db;
    private readonly ILogger<PumpController> _logger = logger;

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

        if (pump == null || user == null || !(user.IsOperator() || user.IsCustomer()))
        {
            const string errorMessage = "Необходимо войти в систему.";
            _logger.LogWarning(errorMessage);
            return StatusCode(StatusCodes.Status401Unauthorized, new ErrMessage { Msg = errorMessage });
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
            Allowance = user.IsCustomer() ? user.Allowance : null
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
}
