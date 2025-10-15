using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class DocentesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;

    public DocentesController(ServicioAContext context, QueueManager qm, ITransaccionStore store, IConfiguration cfg)
    {
        _context = context;
        _qm = qm;
        _store = store;
        _cfg = cfg;
    }

    // === ENDPOINTS SÍNCRONOS ===

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocenteDto>>> GetAll(CancellationToken ct = default)
    {
        var docentes = await _context.Docentes
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .ToListAsync(ct);

        return Ok(docentes.Select(ToDto));
    }

    [HttpGet("{registro}")]
    public async Task<ActionResult<DocenteDto>> GetByRegistro(string registro, CancellationToken ct = default)
    {
        var docente = await _context.Docentes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Registro == registro, ct);

        return docente is null ? NotFound() : Ok(ToDto(docente));
    }

    [HttpPost]
    public async Task<ActionResult<DocenteDto>> Create([FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        var entity = new Docente
        {
            Registro = dto.Registro,
            Ci = dto.Ci,
            Nombre = dto.Nombre,
            Telefono = dto.Telefono,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado
        };

        _context.Docentes.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetByRegistro), new { registro = entity.Registro }, ToDto(entity));
    }

    [HttpPut("{registro}")]
    public async Task<IActionResult> Update(string registro, [FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Registro == registro, ct);
        if (docente is null) return NotFound();

        docente.Ci = dto.Ci;
        docente.Nombre = dto.Nombre;
        docente.Telefono = dto.Telefono;
        docente.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? docente.Estado : dto.Estado;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{registro}")]
    public async Task<IActionResult> Delete(string registro, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Registro == registro, ct);
        if (docente is null) return NotFound();

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // === ENDPOINTS ASÍNCRONOS ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearDocenteAsync(
        [FromBody] DocenteDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Docente",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpPut("async/{registro}")]
    public async Task<IActionResult> ActualizarDocenteAsync(
        string registro,
        [FromBody] DocenteDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        dto.Registro = registro;

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Docente",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpDelete("async/{registro}")]
    public async Task<IActionResult> EliminarDocenteAsync(
        string registro,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var payload = new { Registro = registro };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Docente",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        var tx = await _store.GetAsync(id, ct);
        return tx is null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(new { id = tx.Id, estado = tx.Estado });
    }

    // === Mapeo DTO ===
    private static DocenteDto ToDto(Docente d) => new()
    {
        Id = d.Id,
        Registro = d.Registro,
        Ci = d.Ci,
        Nombre = d.Nombre,
        Telefono = d.Telefono,
        Estado = d.Estado
    };
}