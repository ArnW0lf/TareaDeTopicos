using Microsoft.AspNetCore.Mvc;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/healthz")]
    public IActionResult Healthz()
        => Ok(new { status = "ok", at = DateTimeOffset.UtcNow });
}
