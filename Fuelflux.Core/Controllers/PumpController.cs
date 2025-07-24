using Microsoft.AspNetCore.Mvc;
using Fuelflux.Core.Authorization;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;
using Fuelflux.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Fuelflux.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PumpController(
    IDeviceAuthService authService,
    AppDbContext db,
    ILogger<PumpController> logger) : ControllerBase
{
    private readonly IDeviceAuthService _authService = authService;
    private readonly AppDbContext _db = db;
    private readonly ILogger<PumpController> _logger = logger;

    [HttpPost("authorize")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PumpAuthorizeResponse))]
    public async Task<ActionResult<PumpAuthorizeResponse>> Authorize(PumpAuthorizeRequest request)
    {
        var token = _authService.Authorize();
        _logger.LogDebug("Authorize pump user with uid {uid}", request.Uid);

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Uid == request.Uid);

        if (user == null)
        {
            return Ok(new PumpAuthorizeResponse
            {
                Role = "Не авторизован",
                Token = token
            });
        }

        if (user.IsOperator())
        {
            var tanks = await _db.FuelTanks
                .AsNoTracking()
                .Select(t => new VirtualTankInfo
                {
                    Number = (int)t.Number,
                    NominalVolume = 0m
                })
                .ToListAsync();

            return Ok(new PumpAuthorizeResponse
            {
                Role = "Оператор",
                VirtualTanks = tanks,
                Token = token
            });
        }

        if (user.IsCustomer())
        {
            return Ok(new PumpAuthorizeResponse
            {
                Role = "Пользователь",
                MaxVolume = user.Allowance ?? 0m,
                FuelPrice = 0m,
                Token = token
            });
        }

        return Ok(new PumpAuthorizeResponse { Role = "Не авторизован", Token = token });
    }

    [HttpPost("deauthorize")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Deauthorize(TokenRequest request)
    {
        _authService.Deauthorize(request.Token);
        return Ok();
    }
}
