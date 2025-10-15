using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;

namespace TAREATOPICOS.ServicioA.Controllers.Asincrono;

[ApiController]
[Route("api/[controller]")]
public class InscripcionesAsyncController : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue; // encola la transacción (Redis)
    private readonly ITransaccionStore _store;    // guarda/lee estado (Redis)
    private readonly ServicioAContext _db;        // EF Core (solo para lecturas síncronas de negocio)

    public InscripcionesAsyncController(IBackgroundTaskQueue queue, ITransaccionStore store, ServicioAContext db)
    {
        _queue = queue;
        _store = store;
        _db = db;
    }

    // 1) Crear inscripción (ASÍNCRONO: encola y responde 202)
    // POST /api/inscripciones/async
    [HttpPost("async")]
    public async Task<IActionResult> CrearInscripcionAsync([FromBody] CrearInscripcionAsyncDto dto, CancellationToken ct)
    {
        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "CrearInscripcion",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 2) Agregar detalle (ASÍNCRONO)
    // POST /api/inscripciones/{id}/detalles/async
    [HttpPost("{id:int}/detalles/async")]
    public async Task<IActionResult> AgregarDetalleAsync(int id, [FromBody] AgregarDetalleAsyncDto dto, CancellationToken ct)
    {
        if (dto.InscripcionId != id) return BadRequest("El Id de la inscripción no coincide.");

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "AgregarDetalle",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 3a) Quitar detalle por DetalleId (ASÍNCRONO)
    // DELETE /api/inscripciones/{id}/detalles/{detalleId}/async
    [HttpDelete("{id:int}/detalles/{detalleId:int}/async")]
    public async Task<IActionResult> QuitarDetallePorIdAsync(int id, int detalleId, CancellationToken ct)
    {
        var dto = new QuitarDetallePorIdAsyncDto { InscripcionId = id, DetalleId = detalleId };

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "QuitarDetallePorId",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 3b) Quitar por GrupoMateriaId (ASÍNCRONO)
    // DELETE /api/inscripciones/{id}/detalles/async
    [HttpDelete("{id:int}/detalles/async")]
    public async Task<IActionResult> QuitarDetallePorGrupoAsync(int id, [FromBody] QuitarDetallePorGrupoAsyncDto dto, CancellationToken ct)
    {
        if (dto.InscripcionId != id) return BadRequest("El Id de la inscripción no coincide.");

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "QuitarDetallePorGrupo",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 4) Finalizar inscripción (ASÍNCRONO)
    // POST /api/inscripciones/{id}/finalizar/async
    [HttpPost("{id:int}/finalizar/async")]
    public async Task<IActionResult> FinalizarInscripcionAsync(int id, CancellationToken ct)
    {
        var dto = new FinalizarInscripcionAsyncDto { InscripcionId = id };

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "FinalizarInscripcion",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 5) Consultar estado de una transacción (ASÍNCRONO)
    // GET /api/inscripciones/estado/{txId}
    [HttpGet("estado/{txId:guid}")]
    public async Task<IActionResult> Estado(Guid txId, CancellationToken ct)
    {
        var tx = await _store.GetAsync(txId);
        if (tx is null) return NotFound(new { mensaje = "Transacción no encontrada" });
        return Ok(new { id = tx.Id, estado = tx.Estado });
    }

    // 6) Oferta (YA era asíncrono con EF Core)
    // GET /api/inscripciones/{id}/oferta
    [HttpGet("{id:int}/oferta")]
    public async Task<ActionResult<IEnumerable<GrupoOfertaDto>>> GetOferta(int id, CancellationToken ct)
    {
        var insc = await _db.Inscripciones
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (insc is null) return NotFound("Inscripción no existe.");

        var grupos = await _db.GruposMaterias
            .AsNoTracking()
            .Include(g => g.Materia)
            .Include(g => g.Docente)
            .Include(g => g.Aula)
            .Include(g => g.Horario)
            .Where(g => g.PeriodoId == insc.PeriodoId)
            .ToListAsync(ct);

        var result = grupos.Select(g => new GrupoOfertaDto
        {
            GrupoMateriaId = g.Id,
            MateriaCodigo  = g.Materia.Codigo,
            MateriaNombre  = g.Materia.Nombre,
            Grupo          = g.Grupo,
            Cupo           = g.Cupo,
            Docente        = g.Docente?.Nombre ?? "",
            Aula           = g.Aula?.Codigo ?? "",
            Horarios = g.Horario is null
                ? new List<HorarioOfertaDto>()
                : new List<HorarioOfertaDto> {
                    new HorarioOfertaDto {
                        Dia        = g.Horario.Dia,
                        HoraInicio = g.Horario.HoraInicio,
                        HoraFin    = g.Horario.HoraFin
                    }
                }
        }).ToList();

        return Ok(result);
    }
}
