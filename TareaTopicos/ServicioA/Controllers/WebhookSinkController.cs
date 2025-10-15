using Microsoft.AspNetCore.Mvc;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("webhook/sink")]           // => POST http://localhost:5001/webhook/sink
public class WebhookSinkController : ControllerBase
{
    private readonly ILogger<WebhookSinkController> _logger;
    public WebhookSinkController(ILogger<WebhookSinkController> logger) => _logger = logger;

    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        _logger.LogInformation("📣 Webhook recibido: {Body}", body);
        return Ok(new { ok = true });
    }
}
