
// Controllers/DetallesInscripcionController.cs
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Models; // para Transaccion

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/detalles")]
public class DetallesInscripcionController : ControllerBase
{
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;
    private readonly ServicioAContext _db;  // si luego haces lecturas directas
    private readonly ILogger<DetallesInscripcionController> _log;

    public DetallesInscripcionController(
        QueueManager qm,
        ITransaccionStore store,
        IConfiguration cfg,
        ServicioAContext db,
        ILogger<DetallesInscripcionController> log)
    {
        _qm = qm; _store = store; _cfg = cfg; _db = db; _log = log;
    }

    // ====== DTOs ======
    public record DetalleCreateHumanoDto(string Registro, int PeriodoId, string MateriaCodigo, string Grupo);
    public record DetalleUpdateHumanoDto(string? NuevoGrupo, string? NuevoEstado);
    static string N(string s) => s.Trim().ToUpperInvariant();

    // ====== CREATE (ASYNC) ======
    // POST /api/detalles/async/humano
    [HttpPost("async/humano")]
    public async Task<IActionResult> CrearAsync(
        [FromBody] DetalleCreateHumanoDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _log.LogInformation("POST Detalle (async) recibido. queue={queue} priority={priority} notBeforeUtc={notBefore}",
            queue, priority, notBeforeUtc);

        if (dto is null ||
            string.IsNullOrWhiteSpace(dto.Registro) ||
            dto.PeriodoId <= 0 ||
            string.IsNullOrWhiteSpace(dto.MateriaCodigo) ||
            string.IsNullOrWhiteSpace(dto.Grupo))
        {
            _log.LogWarning("Validación fallida en POST Detalle. {@dto}", dto);
            return BadRequest(new { mensaje = "Registro, PeriodoId (>0), MateriaCodigo y Grupo son requeridos." });
        }

        var payloadObj = new
        {
            Registro = N(dto.Registro),
            PeriodoId = dto.PeriodoId,
            MateriaCodigo = N(dto.MateriaCodigo),
            Grupo = N(dto.Grupo),
            AutoCrearInscripcion = true
        };

        try
        {
            var tx = BuildTx("POST", "DetalleInscripcion", payloadObj, priority, notBeforeUtc);
            await _qm.EnqueueAsync(tx, queue, ct);

            _log.LogInformation("Tx encolada (POST Detalle). TxId={TxId} Estado={Estado} queue={queue}", tx.Id, tx.Estado, queue);
            return Accepted(new { id = tx.Id, estado = tx.Estado });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error encolando POST Detalle. {@payload}", payloadObj);
            return Ok(new { mensaje = "Error encolando operación", detalle = ex.Message });
        }
    }

    // ====== UPDATE (ASYNC) ======
    // PUT /api/detalles/async/humano/by-clave/{registro}/{periodoId:int}/{materiaCodigo}/{grupo}
    [HttpPut("async/humano/by-clave/{registro}/{periodoId:int}/{materiaCodigo}/{grupo}")]
    public async Task<IActionResult> ActualizarAsync(
        string registro, int periodoId, string materiaCodigo, string grupo,
        [FromBody] DetalleUpdateHumanoDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _log.LogInformation("PUT Detalle (async) recibido. clave={@clave} queue={queue} priority={priority}",
            new { registro, periodoId, materiaCodigo, grupo }, queue, priority);

        if (dto is null || (string.IsNullOrWhiteSpace(dto.NuevoGrupo) && string.IsNullOrWhiteSpace(dto.NuevoEstado)))
        {
            _log.LogWarning("Validación fallida en PUT Detalle: body inválido. {@dto}", dto);
            return BadRequest(new { mensaje = "Debes enviar NuevoGrupo o NuevoEstado." });
        }

        if (string.IsNullOrWhiteSpace(registro) || periodoId <= 0 ||
            string.IsNullOrWhiteSpace(materiaCodigo) || string.IsNullOrWhiteSpace(grupo))
        {
            _log.LogWarning("Validación fallida en PUT Detalle: ruta inválida. registro={registro}, periodoId={periodoId}, materiaCodigo={materiaCodigo}, grupo={grupo}",
                registro, periodoId, materiaCodigo, grupo);
            return BadRequest(new { mensaje = "Ruta inválida: registro, periodoId (>0), materiaCodigo y grupo son requeridos." });
        }

        var payloadObj = new
        {
            ClaveActual = new
            {
                Registro = N(registro),
                PeriodoId = periodoId,
                MateriaCodigo = N(materiaCodigo),
                Grupo = N(grupo)
            },
            Update = new
            {
                NuevoGrupo = string.IsNullOrWhiteSpace(dto.NuevoGrupo) ? null : N(dto.NuevoGrupo),
                NuevoEstado = string.IsNullOrWhiteSpace(dto.NuevoEstado) ? null : N(dto.NuevoEstado)
            }
        };

        try
        {
            var tx = BuildTx("PUT", "DetalleInscripcion", payloadObj, priority, notBeforeUtc);
            await _qm.EnqueueAsync(tx, queue, ct);

            _log.LogInformation("Tx encolada (PUT Detalle). TxId={TxId} Estado={Estado} queue={queue}", tx.Id, tx.Estado, queue);
            return Accepted(new { id = tx.Id, estado = tx.Estado });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error encolando PUT Detalle. {@payload}", payloadObj);
            return Ok(new { mensaje = "Error encolando operación", detalle = ex.Message });
        }
    }

    // ====== DELETE (ASYNC) ======
    // DELETE /api/detalles/async/humano/by-clave/{registro}/{periodoId:int}/{materiaCodigo}/{grupo}
    [HttpDelete("async/humano/by-clave/{registro}/{periodoId:int}/{materiaCodigo}/{grupo}")]
    public async Task<IActionResult> EliminarAsync(
        string registro, int periodoId, string materiaCodigo, string grupo,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _log.LogInformation("DELETE Detalle (async) recibido. clave={@clave} queue={queue} priority={priority}",
            new { registro, periodoId, materiaCodigo, grupo }, queue, priority);

        if (string.IsNullOrWhiteSpace(registro) || periodoId <= 0 ||
            string.IsNullOrWhiteSpace(materiaCodigo) || string.IsNullOrWhiteSpace(grupo))
        {
            _log.LogWarning("Validación fallida en DELETE Detalle: ruta inválida. registro={registro}, periodoId={periodoId}, materiaCodigo={materiaCodigo}, grupo={grupo}",
                registro, periodoId, materiaCodigo, grupo);
            return BadRequest(new { mensaje = "Ruta inválida: registro, periodoId (>0), materiaCodigo y grupo son requeridos." });
        }

        var payloadObj = new
        {
            Registro = N(registro),
            PeriodoId = periodoId,
            MateriaCodigo = N(materiaCodigo),
            Grupo = N(grupo)
        };

        try
        {
            var tx = BuildTx("DELETE", "DetalleInscripcion", payloadObj, priority, notBeforeUtc);
            await _qm.EnqueueAsync(tx, queue, ct);

            _log.LogInformation("Tx encolada (DELETE Detalle). TxId={TxId} Estado={Estado} queue={queue}", tx.Id, tx.Estado, queue);
            return Accepted(new { id = tx.Id, estado = tx.Estado });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error encolando DELETE Detalle. {@payload}", payloadObj);
            return Ok(new { mensaje = "Error encolando operación", detalle = ex.Message });
        }
    }

    // ====== Helpers ======
    private Transaccion BuildTx(string tipo, string entidad, object payloadObj, int priority, DateTimeOffset? notBeforeUtc)
    {
        var payload = JsonSerializer.Serialize(payloadObj);
        var tx = new Transaccion
        {
            TipoOperacion = tipo,
            Entidad = entidad,
            Payload = payload,
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        _log.LogDebug("Transacción construida {@tx}", new { tx.Id, tx.TipoOperacion, tx.Entidad, tx.Priority, tx.NotBefore, tx.IdempotencyKey });
        return tx;
    }
}
