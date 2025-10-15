using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class PlanesDeEstudioController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;

    public PlanesDeEstudioController(ServicioAContext context, QueueManager qm, ITransaccionStore store, IConfiguration cfg)
    {
        _context = context;
        _qm = qm;
        _store = store;
        _cfg = cfg;
    }

    // === SÍNCRONOS ===

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanDeEstudioDto>>> GetAll(CancellationToken ct = default)
    {
        var items = await _context.PlanesEstudio
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

        return Ok(items.Select(ToDTO));
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<PlanDeEstudioDto>> GetByCodigo(string codigo, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Codigo == codigo, ct);

        return plan is null ? NotFound() : Ok(ToDTO(plan));
    }

    [HttpPost]
    public async Task<ActionResult<PlanDeEstudioDto>> Create([FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo))
            return BadRequest("El campo 'Codigo' es obligatorio.");

        var existe = await _context.PlanesEstudio
            .AsNoTracking()
            .AnyAsync(p => p.Codigo == dto.Codigo, ct);

        if (existe)
            return Conflict($"Ya existe un plan con el código '{dto.Codigo}'");

        var entity = new PlanDeEstudio
        {
            Nombre = dto.Nombre,
            Codigo = dto.Codigo,
            Fecha = dto.Fecha,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            CarreraId = dto.CarreraId
        };

        _context.PlanesEstudio.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetByCodigo), new { codigo = entity.Codigo }, ToDTO(entity));
    }

    [HttpPut("{codigo}")]
    public async Task<IActionResult> Update(string codigo, [FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio.FirstOrDefaultAsync(p => p.Codigo == codigo, ct);
        if (plan is null) return NotFound();

        plan.Nombre = dto.Nombre;
        plan.Fecha = dto.Fecha;
        plan.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? plan.Estado : dto.Estado;
        plan.CarreraId = dto.CarreraId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{codigo}")]
    public async Task<IActionResult> Delete(string codigo, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio.FirstOrDefaultAsync(p => p.Codigo == codigo, ct);
        if (plan is null) return NotFound();

        _context.PlanesEstudio.Remove(plan);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{codigo}/materias")]
    public async Task<ActionResult<IEnumerable<MateriaRequestDto>>> GetMateriasDePlan(string codigo, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Codigo == codigo, ct);

        if (plan is null)
            return NotFound($"No existe un PlanDeEstudio con Código={codigo}");

        var materias = await _context.PlanMaterias
            .AsNoTracking()
            .Where(pm => pm.PlanId == plan.Id)
            .Include(pm => pm.Materia)
            .Select(pm => new MateriaRequestDto
            {
                Id = pm.Materia.Id,
                Codigo = pm.Materia.Codigo,
                Nombre = pm.Materia.Nombre,
                Creditos = pm.Materia.Creditos,
                NivelId = pm.Materia.NivelId
            })
            .OrderBy(m => m.Nombre)
            .ToListAsync(ct);

        return Ok(materias);
    }

    // === ASÍNCRONOS ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearPlanAsync(
        [FromBody] PlanDeEstudioDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "PlanDeEstudio",
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

    [HttpPut("async/{codigo}")]
    public async Task<IActionResult> ActualizarPlanAsync(
        string codigo,
        [FromBody] PlanDeEstudioDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        dto.Codigo = codigo;

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "PlanDeEstudio",
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

    [HttpDelete("async/{codigo}")]
    public async Task<IActionResult> EliminarPlanAsync(
        string codigo,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var payload = new { Codigo = codigo };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "PlanDeEstudio",
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
    private static PlanDeEstudioDto ToDTO(PlanDeEstudio p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Codigo = p.Codigo,
        Fecha = p.Fecha,
        Estado = p.Estado,
        CarreraId = p.CarreraId
    };
}