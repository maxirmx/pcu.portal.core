using Microsoft.AspNetCore.Mvc;
using Fuelflux.Core.Authorization;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Services;

namespace Fuelflux.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PumpController(IDeviceAuthService authService, ILogger<PumpController> logger) : ControllerBase
{
    private readonly IDeviceAuthService _authService = authService;
    private readonly ILogger<PumpController> _logger = logger;

    [HttpPost("authorize")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    public ActionResult<TokenResponse> Authorize()
    {
        var token = _authService.Authorize();
        return Ok(new TokenResponse { Token = token });
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
