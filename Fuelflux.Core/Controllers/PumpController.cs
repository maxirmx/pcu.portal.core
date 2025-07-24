using Microsoft.AspNetCore.Mvc;
using Fuelflux.Core.Authorization;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;
using Fuelflux.Core.Data;
using Microsoft.EntityFrameworkCore;
using Fuelflux.Core.Models;

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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    public async Task<ActionResult<TokenResponse>> Authorize(DeviceAuthorizeRequest request)
    {
        var pump = await _db.PumpControllers.AsNoTracking().FirstOrDefaultAsync(p => p.Uid == request.PumpControllerUid);
        var user = await _db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Uid == request.UserUid);

        if (pump == null || user == null || !(user.IsOperator() || user.IsCustomer()))
        {
            const string errorMessage = "Необходимо войти в систему.";
            _logger.LogWarning(errorMessage);
            return StatusCode(StatusCodes.Status401Unauthorized, new ErrMessage { Msg = errorMessage });
        }

        var token = _authService.Authorize(pump, user);
        return Ok(new TokenResponse { Token = token });
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
