using Microsoft.AspNetCore.Mvc;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransaccionesController : ControllerBase
{
private readonly ITransaccionStore _store;
private readonly QueueManager _qm;

public TransaccionesController(ITransaccionStore store, QueueManager qm)
{
    _store = store;
    _qm = qm;
}

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tx = await _store.GetAsync(id, ct);
        return tx is null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(tx);
    }
   [HttpPost("enqueue")]
public async Task<IActionResult> Enqueue(
    [FromBody] Transaccion tx,
    [FromQuery] string? queue = null)
{
    try
    {
        var chosenQueue = await _qm.EnqueueAsync(tx, queue);
        return Ok(new
        {
            id = tx.Id,
            estado = tx.Estado,
            queue = chosenQueue   // 👈 ahora también retorna la cola real
        });
    }
    catch (QueueFullException ex)
    {
        return StatusCode(429, new
        {
            error = ex.Message,
            queue = ex.QueueName,
            backlog = ex.Backlog,
            maxAllowed = ex.MaxAllowed
        });
    }
}




}
