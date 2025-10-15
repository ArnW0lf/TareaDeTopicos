using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NivelesController : ControllerBase
{
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly ServicioAContext _db;
    private readonly IConfiguration _cfg;
    private readonly WorkerHost _workerHost;
    public NivelesController(QueueManager qm, ITransaccionStore store, ServicioAContext db, IConfiguration cfg,
    WorkerHost workerHost)
    {
        _qm = qm;
        _store = store;
        _db = db;
        _cfg = cfg;
        _workerHost = workerHost;
    }
    




// === ENDPOINTS SÍNCRONOS (CRUD directo) ===
// === ENDPOINTS SÍNCRONOS (CRUD directo) ===

// GET /api/niveles
[HttpGet]
public async Task<ActionResult> GetAll(CancellationToken ct = default)
{
    var items = await _db.Niveles
        .AsNoTracking()
        .OrderBy(n => n.Numero)
        .ToListAsync(ct);

    return Ok(items);
}

// GET /api/niveles/numero/9
[HttpGet("numero/{numero:int}")]
public async Task<ActionResult> GetByNumero(int numero, CancellationToken ct = default)
{
    var nivel = await _db.Niveles
        .AsNoTracking()
        .FirstOrDefaultAsync(n => n.Numero == numero, ct);

    if (nivel is null)
        return NotFound(new { mensaje = $"Nivel número {numero} no encontrado" });

    return Ok(nivel);
}

// POST /api/niveles  (idempotente)
[HttpPost]
public async Task<ActionResult> Create([FromBody] Nivel dto, CancellationToken ct = default)
{
    if (dto.Numero <= 0)
        return BadRequest(new { mensaje = "Numero debe ser > 0" });
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return BadRequest(new { mensaje = "Nombre es requerido" });

    // Si ya existe, responde 200 con el recurso existente (sin ❌)
    var existente = await _db.Niveles.AsNoTracking()
        .FirstOrDefaultAsync(n => n.Numero == dto.Numero, ct);
    if (existente is not null)
        return Ok(existente); // <- idempotente: no 409

    var entity = new Nivel
    {
        Numero = dto.Numero,
        Nombre = dto.Nombre.Trim()
    };

    try
    {
        _db.Niveles.Add(entity);
        await _db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
    {
        // Carrera: alguien lo creó entre el check y el insert → devolvemos el existente como 200
        var ya = await _db.Niveles.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Numero == dto.Numero, ct);
        if (ya is not null) return Ok(ya);
        throw;
    }

    return CreatedAtAction(nameof(GetByNumero), new { numero = entity.Numero }, entity);
}

[HttpPut("numero/{numero:int}")]
public async Task<IActionResult> UpdateByNumero(
    int numero, [FromBody] Nivel dto, CancellationToken ct = default)
{
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return BadRequest(new { mensaje = "Nombre es requerido" });

    var nivel = await _db.Niveles.FirstOrDefaultAsync(n => n.Numero == numero, ct);
    if (nivel is null)
        return Ok(new { mensaje = $"El nivel con número {numero} no existe, no se puede editar." });

    var nuevoNombre = dto.Nombre.Trim();

    // Idempotencia: si ya está igual → responde igual
    if (string.Equals(nivel.Nombre, nuevoNombre, StringComparison.Ordinal))
        return Ok(new { mensaje = "Sin cambios", recurso = nivel });

    nivel.Nombre = nuevoNombre;
    await _db.SaveChangesAsync(ct);

    return Ok(new { mensaje = "Nivel actualizado", recurso = nivel });
}


// DELETE /api/niveles/numero/9
[HttpDelete("numero/{numero:int}")]
public async Task<IActionResult> DeleteByNumero(int numero, CancellationToken ct = default)
{
    var nivel = await _db.Niveles.FirstOrDefaultAsync(n => n.Numero == numero, ct);

    if (nivel is null)
    {
        // Idempotencia: borrar varias veces → 204 igual
        return NoContent();
    }

    try
    {
        _db.Niveles.Remove(nivel);
        await _db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
    {
        return Conflict(new { mensaje = $"No se puede eliminar el nivel {numero} porque tiene dependencias (FK)." });
    }

    return NoContent();
}


// ==== helper para mapear unique-violation a 409 ====
private static bool IsUniqueViolation(DbUpdateException ex)
{
    var m = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
    return m.Contains("unique") || m.Contains("duplicate") || m.Contains("uq_") || m.Contains("idx_unique");
}



































































































    // ===  ENDPOINTS ASÍNCRONAS  ===
    
    // // POST /api/niveles/async
    // [HttpPost("async")]
    // public async Task<IActionResult> CrearNivelAsync(
    //     [FromBody] Nivel nivel,
    //     [FromQuery] string? queue = "default",
    //     [FromQuery] int priority = 1,
    //     [FromQuery] DateTimeOffset? notBeforeUtc = null,
    //     CancellationToken ct = default)
    // {
    //     // Evita duplicar por Numero (índice único)
    //     if (await _db.Niveles.AnyAsync(x => x.Numero == nivel.Numero, ct))
    //         return Accepted(new { mensaje = $"Nivel ya existe (Numero {nivel.Numero} duplicado)" });

    //     var tx = new Transaccion
    //     {
    //         TipoOperacion = "POST",
    //         Entidad       = "Nivel",
    //         Payload       = JsonSerializer.Serialize(nivel),
    //         Estado        = "EN_COLA",
    //         Priority      = Math.Clamp(priority, 0, 2),
    //         NotBefore     = notBeforeUtc ?? DateTimeOffset.UtcNow
    //     };
    //     tx.CallbackUrl    ??= _cfg["Webhook:DefaultUrl"];
    //     tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
    //     tx.IdempotencyKey ??= tx.Id.ToString();

    //     await _qm.EnqueueAsync(tx, queue, ct);
    //     return Accepted(new { id = tx.Id, estado = tx.Estado });
    // }
[HttpPost("async")]
public async Task<IActionResult> CrearNivelAsync(
    [FromBody] Nivel nivel,
    [FromQuery] string? queue = null,

    [FromQuery] int priority = 1,
    [FromQuery] DateTimeOffset? notBeforeUtc = null,
    CancellationToken ct = default)
{
    if (await _db.Niveles.AnyAsync(x => x.Numero == nivel.Numero, ct))
        return Accepted(new { mensaje = $"Nivel ya existe (Numero {nivel.Numero} duplicado)" });

    var tx = new Transaccion
    {
        TipoOperacion = "POST",
        Entidad       = "Nivel",
        Payload       = JsonSerializer.Serialize(nivel),
        Estado        = "EN_COLA",
        Priority      = Math.Clamp(priority, 0, 2),
        NotBefore     = notBeforeUtc ?? DateTimeOffset.UtcNow
    };
    tx.CallbackUrl    ??= _cfg["Webhook:DefaultUrl"];
    tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
    tx.IdempotencyKey ??= tx.Id.ToString();

    // 👇 capturamos el nombre real de la cola
    var chosenQueue = await _qm.EnqueueAsync(tx, queue, ct);

    return Accepted(new
    {
        id = tx.Id,
        estado = tx.Estado,
        queue = chosenQueue   // 👈 ahora devuelve la cola también
    });
}

    // PUT /api/niveles/async/numero/5077
    [HttpPut("async/numero/{numero:int}")]
    public async Task<IActionResult> ActualizarNivelPorNumeroAsync(
        int numero,
        [FromBody] Nivel nivel,                // el cliente NO manda Id
        [FromQuery] string? queue = null,
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        nivel.Id = 0;                 // no exponer Id
        nivel.Numero = numero;        // forzar Numero desde ruta

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad       = "Nivel",
            Payload       = JsonSerializer.Serialize(nivel),
            Estado        = "EN_COLA",
            Priority      = Math.Clamp(priority, 0, 2),
            NotBefore     = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl    ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE /api/niveles/async/numero/5077
    [HttpDelete("async/numero/{numero:int}")]
    public async Task<IActionResult> EliminarNivelPorNumeroAsync(
        int numero,
        [FromQuery] string? queue = null,
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var payload = new { Numero = numero };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad       = "Nivel",
            Payload       = JsonSerializer.Serialize(payload),
            Estado        = "EN_COLA",
            Priority      = Math.Clamp(priority, 0, 2),
            NotBefore     = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl    ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // GET /api/niveles/estado/{id}
    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        var tx = await _store.GetAsync(id, ct);
        return tx is null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(new { id = tx.Id, estado = tx.Estado });
    }

    /* ====== (Opcional) RUTAS ANTIGUAS POR Id – si aún las necesitas, deja esto; si no, bórralo ======

    // PUT /api/niveles/async/{id:int}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> ActualizarNivelAsync(
        int id, [FromBody] Nivel nivel, [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1, [FromQuery] DateTimeOffset? notBeforeUtc = null, CancellationToken ct = default)
    {
        nivel.Id = id;
        var tx = new Transaccion { TipoOperacion = "PUT", Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel), Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2), NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow };
        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE /api/niveles/async/{id:int}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> EliminarNivelAsync(
        int id, [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1, [FromQuery] DateTimeOffset? notBeforeUtc = null, CancellationToken ct = default)
    {
        var tx = new Transaccion { TipoOperacion = "DELETE", Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(new { Id = id }), Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2), NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow };
        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }
    ================================================================================================== */
}

 
