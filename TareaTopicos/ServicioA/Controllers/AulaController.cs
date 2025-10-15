using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AulasController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AulasController> _logger;

    public AulasController(
        ServicioAContext context,
        QueueManager qm,
        ITransaccionStore store,
        IConfiguration cfg,
        ILogger<AulasController> logger)
    {
        _context = context;
        _qm = qm;
        _store = store;
        _cfg = cfg;
        _logger = logger;
    }

   // === ENDPOINTS SÍNCRONOS ===

[HttpGet]
public async Task<ActionResult<IEnumerable<AulaDto>>> Get(CancellationToken ct = default)
{
    _logger.LogInformation("📥 GET /api/aulas → obteniendo todas las aulas");

    var items = await _context.Aulas
        .AsNoTracking()
        .OrderBy(a => a.Codigo)
        .ToListAsync(ct);

    _logger.LogInformation("📤 Devueltas {Count} aulas", items.Count);
    return Ok(items.Select(ToDTO));
}

[HttpGet("{id:int}")]
public async Task<ActionResult<AulaDto>> GetById(int id, CancellationToken ct = default)
{
    _logger.LogInformation("📥 GET /api/aulas/{Id}", id);

    var aula = await _context.Aulas
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id, ct);

    if (aula is null)
    {
        _logger.LogWarning("⚠️ Aula con Id {Id} no encontrada", id);
        return NotFound(new { mensaje = $"Aula Id {id} no encontrada" });
    }

    _logger.LogInformation("📤 Aula encontrada: {Codigo}", aula.Codigo);
    return Ok(ToDTO(aula));
}

// ✅ NUEVO: GET por código (simetría)
[HttpGet("codigo/{codigo}")]
public async Task<ActionResult<AulaDto>> GetByCodigo(string codigo, CancellationToken ct = default)
{
    _logger.LogInformation("📥 GET /api/aulas/codigo/{Codigo}", codigo);

    var aula = await _context.Aulas
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Codigo == codigo, ct);

    if (aula is null)
    {
        _logger.LogWarning("⚠️ Aula con código {Codigo} no encontrada", codigo);
        return NotFound(new { mensaje = $"Aula código {codigo} no encontrada" });
    }

    return Ok(ToDTO(aula));
}
[HttpPost]
public async Task<ActionResult<AulaDto>> Create([FromBody] AulaDto dto, CancellationToken ct = default)
{
    // Validación básica
    if (string.IsNullOrWhiteSpace(dto.Codigo))
        return BadRequest(new { mensaje = "Codigo es requerido" });
    if (string.IsNullOrWhiteSpace(dto.Ubicacion))
        return BadRequest(new { mensaje = "Ubicacion es requerida" });
    if (dto.Capacidad <= 0)
        return BadRequest(new { mensaje = "Capacidad debe ser > 0" });

    // Normaliza (opcional)
    dto.Codigo = dto.Codigo.Trim();

    // Idempotencia: si ya existe, devuelve 200 con el existente (no 409)
    var existente = await _context.Aulas
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Codigo == dto.Codigo, ct);

    if (existente is not null)
        return Ok(ToDTO(existente));

    var entity = new Aula
    {
        Codigo = dto.Codigo,
        Capacidad = dto.Capacidad,
        Ubicacion = dto.Ubicacion
    };

    try
    {
        _context.Aulas.Add(entity);
        await _context.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
    {
        // Si otro hilo lo insertó justo antes, responde OK con el existente
        var ya = await _context.Aulas.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Codigo == dto.Codigo, ct);
        if (ya is not null) return Ok(ToDTO(ya));
        throw;
    }

    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
}

[HttpPut("codigo/{codigo}")]
public async Task<IActionResult> UpdateByCodigo(string codigo, [FromBody] AulaDto dto, CancellationToken ct = default)
{
    _logger.LogInformation("✏️ PUT /api/aulas/codigo/{Codigo}", codigo);

    var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Codigo == codigo, ct);
    if (aula is null)
    {
        _logger.LogWarning("ℹ️ Aula con código {Codigo} no existe (PUT sin efecto)", codigo);
        // <- Cambiado: 200 OK con mensaje, en lugar de 404
        return Ok(new { mensaje = $"Aula código {codigo} no existe" });
    }

    // Validación básica (se mantiene)
    if (dto.Capacidad <= 0)
        return BadRequest(new { mensaje = "Capacidad debe ser > 0" });
    if (string.IsNullOrWhiteSpace(dto.Ubicacion))
        return BadRequest(new { mensaje = "Ubicacion es requerida" });

    // Idempotencia: si no hay cambios reales, devolver el actual
    if (aula.Capacidad == dto.Capacidad && aula.Ubicacion == dto.Ubicacion)
    {
        _logger.LogInformation("ℹ️ Aula {Codigo} ya tenía los mismos datos", codigo);
        return Ok(ToDTO(aula));
    }

    // Actualizar
    aula.Capacidad = dto.Capacidad;
    aula.Ubicacion = dto.Ubicacion;
    await _context.SaveChangesAsync(ct);

    _logger.LogInformation("✅ Aula {Codigo} actualizada", codigo);
    return Ok(ToDTO(aula));
}



[HttpDelete("codigo/{codigo}")]
public async Task<IActionResult> DeleteByCodigo(string codigo, CancellationToken ct = default)
{
    _logger.LogInformation("🗑 DELETE /api/aulas/codigo/{Codigo}", codigo);

    var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Codigo == codigo, ct);
    if (aula is null)
    {
        _logger.LogWarning("⚠️ Aula con código {Codigo} no encontrada para eliminar", codigo);
        // Idempotencia: no se considera error si ya no existe
        return Ok(new { mensaje = $"Aula código {codigo} ya estaba eliminada o no existe" });
    }

    _context.Aulas.Remove(aula);
    await _context.SaveChangesAsync(ct);

    _logger.LogInformation("✅ Aula {Codigo} eliminada", codigo);
    return Ok(new { mensaje = $"Aula código {codigo} eliminada correctamente" });
}


// === Helper local para mapear unique-violation a 409 ===
private static bool IsUniqueViolation(DbUpdateException ex)
{
    var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
    // Ajusta si tienes nombre de índice/constraint específico
    return msg.Contains("unique") || msg.Contains("duplicate") || msg.Contains("uq_") || msg.Contains("idx_unique");
}

















































    // === ENDPOINTS ASÍNCRONOS ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearAulaAsync(
        [FromBody] AulaDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("📥 POST /api/aulas/async → encolando creación de aula {Codigo}", dto.Codigo);

        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Aula",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        _logger.LogInformation("✅ Tx {TxId} encolada (POST Aula)", tx.Id);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

   [HttpPut("async/codigo/{codigo}")]
public async Task<IActionResult> ActualizarAulaAsyncPorCodigo(
    string codigo,
    [FromBody] AulaDto dto,
    [FromQuery] string? queue = "default",
    [FromQuery] int priority = 1,
    [FromQuery] DateTimeOffset? notBeforeUtc = null,
    CancellationToken ct = default)
{
    _logger.LogInformation("📥 PUT /api/aulas/async/codigo/{Codigo} → encolando actualización", codigo);

    // Forzar que el Codigo del body sea el mismo que el de la ruta
    dto.Codigo = codigo;

    var tx = new Transaccion
    {
        TipoOperacion  = "PUT",
        Entidad        = "Aula",
        Payload        = JsonSerializer.Serialize(dto),
        Estado         = "EN_COLA",
        Priority       = Math.Clamp(priority, 0, 2),
        NotBefore      = notBeforeUtc ?? DateTimeOffset.UtcNow,
        CallbackUrl    = _cfg["Webhook:DefaultUrl"],
        CallbackSecret = _cfg["Webhook:DefaultSecret"],
        IdempotencyKey = Guid.NewGuid().ToString()
    };

    await _qm.EnqueueAsync(tx, queue, ct);
    _logger.LogInformation("✅ Tx {TxId} encolada (PUT Aula)", tx.Id);

    return Accepted(new { id = tx.Id, estado = tx.Estado });
}

    [HttpDelete("async/codigo/{codigo}")]
    public async Task<IActionResult> EliminarAulaAsyncPorCodigo(
        string codigo,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("📥 DELETE /api/aulas/async/codigo/{Codigo} → encolando eliminación", codigo);

        var payload = new { Codigo = codigo };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Aula",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        _logger.LogInformation("✅ Tx {TxId} encolada (DELETE Aula)", tx.Id);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("📥 GET /api/aulas/estado/{Id}", id);

        var tx = await _store.GetAsync(id, ct);
        if (tx is null)
        {
            _logger.LogWarning("⚠️ Transacción {Id} no encontrada", id);
            return NotFound(new { mensaje = "Transacción no encontrada" });
        }

        _logger.LogInformation("📤 Tx {Id} encontrada con estado {Estado}", tx.Id, tx.Estado);
        return Ok(new { id = tx.Id, estado = tx.Estado });
    }

    // === Mapeo DTO ===
    private static AulaDto ToDTO(Aula a) => new()
    {
        Id = a.Id,
        Codigo = a.Codigo,
        Capacidad = a.Capacidad,
        Ubicacion = a.Ubicacion
    };
}
