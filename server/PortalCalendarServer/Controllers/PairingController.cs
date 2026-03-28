using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

[ApiController]
[Route("api/pairing")]
[Authorize]
[Tags("Pairing")]
public class PairingController(PairingModeService pairingMode, ILogger<PairingController> logger) : ControllerBase
{
    /// <summary>Activate pairing mode for 5 minutes (or until one device pairs).</summary>
    [HttpPost("activate")]
    public IActionResult Activate()
    {
        pairingMode.Activate();
        logger.LogInformation("Pairing mode activated, expires in {Seconds}s", pairingMode.SecondsRemaining);
        return Ok(new { active = true, secondsRemaining = pairingMode.SecondsRemaining });
    }

    /// <summary>Deactivate pairing mode immediately.</summary>
    [HttpPost("deactivate")]
    public IActionResult Deactivate()
    {
        pairingMode.Deactivate();
        logger.LogInformation("Pairing mode deactivated");
        return Ok(new { active = false, secondsRemaining = 0 });
    }

    /// <summary>Returns current pairing mode status.</summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { active = pairingMode.IsActive, secondsRemaining = pairingMode.SecondsRemaining });
    }
}
