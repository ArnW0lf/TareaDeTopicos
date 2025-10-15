using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class PeriodosAcademicosController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;

    public PeriodosAcademicosController(ServicioAContext context, QueueManager qm, ITransaccionStore store, IConfiguration cfg)
    {
        _context = context;
        _qm = qm;
        _store = store;
        _cfg = cfg;
    }

    // === SÍNCRONOS ===

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PeriodoAcademicoRequestDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.PeriodosAcademicos
            .AsNoTracking()
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync(ct);

        return Ok(list.Select(ToDTO));
    }

    [HttpGet("{gestion}")]
    public async Task<ActionResult<PeriodoAcademicoRequestDto>> GetByGestion(string gestion, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Gestion == gestion, ct);

        return p is null ? NotFound() : Ok(ToDTO(p));
    }

    [HttpPost]
    public async Task<ActionResult<PeriodoAcademicoRequestDto>> Create([FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        if (dto.FechaFin < dto.FechaInicio)
            return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");

        var existe = await _context.PeriodosAcademicos
            .AsNoTracking()
            .AnyAsync(p => p.Gestion == dto.Gestion, ct);

        if (existe)
            return Conflict($"Ya existe un periodo con la gestión '{dto.Gestion}'");

        var p = new PeriodoAcademico
        {
            Gestion = dto.Gestion,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin
        };

        _context.PeriodosAcademicos.Add(p);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetByGestion), new { gestion = p.Gestion }, ToDTO(p));
    }

    [HttpPut("{gestion}")]
    public async Task<IActionResult> Update(string gestion, [FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.FirstOrDefaultAsync(x => x.Gestion == gestion, ct);
        if (p is null) return NotFound();

        if (dto.FechaFin < dto.FechaInicio)
            return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");

        p.FechaInicio = dto.FechaInicio;
        p.FechaFin = dto.FechaFin;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{gestion}")]
    public async Task<IActionResult> Delete(string gestion, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.FirstOrDefaultAsync(x => x.Gestion == gestion, ct);
        if (p is null) return NotFound();

        _context.PeriodosAcademicos.Remove(p);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // === ASÍNCRONOS ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearPeriodoAsync(
        [FromBody] PeriodoAcademicoRequestDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "PeriodoAcademico",
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

    [HttpPut("async/{gestion}")]
    public async Task<IActionResult> ActualizarPeriodoAsync(
        string gestion,
        [FromBody] PeriodoAcademicoRequestDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        dto.Gestion = gestion;

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "PeriodoAcademico",
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

    [HttpDelete("async/{gestion}")]
    public async Task<IActionResult> EliminarPeriodoAsync(
        string gestion,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var payload = new { Gestion = gestion };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "PeriodoAcademico",
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

    // === Mapper ===
    private static PeriodoAcademicoRequestDto ToDTO(PeriodoAcademico p) => new()
    {
        Id = p.Id,
        Gestion = p.Gestion,
        FechaInicio = p.FechaInicio,
        FechaFin = p.FechaFin
    };
}