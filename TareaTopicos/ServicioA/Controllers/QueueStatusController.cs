using Microsoft.AspNetCore.Mvc;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/queue")]
public class QueueStatusController : ControllerBase
{
    private readonly QueueManager _qm;
    private readonly WorkerHost _workerHost;

    public QueueStatusController(QueueManager qm, WorkerHost workerHost)
    {
        _qm = qm;
        _workerHost = workerHost;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        bool running = _workerHost.IsRunning(); // ✅ ahora usa el flag real

        var queues = _workerHost.ListQueues();

        return Ok(new
        {
            running,
            queues = queues.ToDictionary(q => q.Key, q => new
            {
                workers = q.Value
            }),
            timestamp = DateTime.UtcNow,
            mensaje = running
                ? "✅ La cola está activa y procesando transacciones."
                : "⚠️ No hay workers activos o las colas están detenidas."
        });
    }
}
