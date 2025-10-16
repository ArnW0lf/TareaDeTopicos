using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Models;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/inscripciones")]
public class InscripcionesController : ControllerBase
{
    private readonly QueueManager _qm;
    private readonly IConfiguration _cfg;
    private readonly ILogger<InscripcionesController> _log;
    private readonly ServicioAContext _context;

    public InscripcionesController(QueueManager qm, IConfiguration cfg,
     ILogger<InscripcionesController> log,
    ServicioAContext context)
    {
        _qm = qm;
        _cfg = cfg;
        _log = log;
        _context = context;
    }

    // === DTOs ===
    public record MateriaGrupoDto(string MateriaCodigo, string Grupo);
    public record InscripcionCreateDto(string Registro, int PeriodoId, List<MateriaGrupoDto> Materias, string IdempotencyKey);
    // ===========================================
    // GET /api/inscripciones/materias-disponibles/{registro}
    // ===========================================
    [HttpGet("materias-disponibles/{registro}")]
    public async Task<IActionResult> ObtenerMateriasDisponibles(
        string registro,
        [FromServices] ServicioAContext db)
    {
        registro = registro.Trim().ToUpperInvariant();

        // Buscar estudiante
        var estudiante = await db.Estudiantes
            .Include(e => e.Carrera)
            .FirstOrDefaultAsync(e => e.Registro.ToUpper() == registro);

        if (estudiante == null)
            return NotFound(new { mensaje = $"No se encontró estudiante con registro '{registro}'." });

        // Buscar materias ya inscritas o cursadas
        var cursadas = await db.Inscripciones
            .Where(i => i.EstudianteId == estudiante.Id)
            .SelectMany(i => i.Detalles)
            .Select(d => d.GrupoMateria.MateriaId)
            .Distinct()
            .ToListAsync();

        // Buscar materias del plan de la carrera que aún no cursó
        var materiasPendientes = await db.PlanMaterias
            .Include(pm => pm.Materia)
            .Where(pm => pm.Plan.CarreraId == estudiante.CarreraId &&
                         !cursadas.Contains(pm.MateriaId))
            .Select(pm => new
            {
                pm.Materia.Codigo,
                pm.Materia.Nombre,
                pm.Materia.Creditos,
                pm.Semestre
            })
            .OrderBy(pm => pm.Semestre)
            .ToListAsync();

        if (!materiasPendientes.Any())
            return Ok(new { mensaje = "El estudiante ya cursó todas las materias del plan." });

        return Ok(materiasPendientes);
    }
    // ===========================================
    // GET /api/inscripciones/grupos/{materiaCodigo}
    // ===========================================
    [HttpGet("grupos/{materiaCodigo}")]
    public async Task<IActionResult> ObtenerGruposPorMateria(
        string materiaCodigo,
        [FromServices] ServicioAContext db)
    {
        materiaCodigo = materiaCodigo.Trim().ToUpperInvariant();

        // Buscar materia por código
        var materia = await db.Materias
            .FirstOrDefaultAsync(m => m.Codigo.ToUpper() == materiaCodigo);

        if (materia == null)
            return NotFound(new { mensaje = $"No se encontró la materia con código '{materiaCodigo}'." });

        // Buscar los grupos activos de esa materia
        var grupos = await db.GruposMaterias
            .Include(g => g.Docente)
            .Include(g => g.Horario)
            .Include(g => g.Aula)
            .Where(g => g.MateriaId == materia.Id && g.Estado == "ACTIVO")
            .Select(g => new
            {
                g.Id,
                g.Grupo,
                Docente = g.Docente.Nombre,
                Cupo = g.Cupo,
                Aula = g.Aula != null ? g.Aula.Codigo : "SIN AULA",
                Horario = g.Horario != null
                    ? $"{g.Horario.Dia} {g.Horario.HoraInicio:hh\\:mm} - {g.Horario.HoraFin:hh\\:mm}"
                    : "SIN HORARIO"
            })
            .OrderBy(g => g.Grupo)
            .ToListAsync();

        if (!grupos.Any())
            return NotFound(new { mensaje = $"No hay grupos disponibles para la materia '{materia.Nombre}'." });

        return Ok(new
        {
            Materia = materia.Nombre,
            Codigo = materia.Codigo,
            Grupos = grupos
        });
    }

    // ===========================================
    // POST /api/inscripciones/async
    // ===========================================
    [HttpPost("async")]
    public async Task<IActionResult> CrearAsync(
        [FromBody] InscripcionCreateDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Registro) || string.IsNullOrWhiteSpace(dto.IdempotencyKey) || dto.PeriodoId <= 0 ||
            dto.Materias == null || !dto.Materias.Any())
            return BadRequest(new { mensaje = "Registro, IdempotencyKey, PeriodoId y al menos una materia son requeridos." });

        try
        {
            // Idempotencia: Buscar si ya existe una transacción con esta clave
            var txExistente = await _context.Transacciones
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdempotencyKey == dto.IdempotencyKey, ct);

            if (txExistente != null)
            {
                _log.LogInformation("🔁 Petición idempotente detectada. Devolviendo transacción existente {TxId}", txExistente.Id);
                return Conflict(new { mensaje = "Esta solicitud ya fue procesada.", transactionId = txExistente.Id });
            }

            // 🔹 Buscar estudiante
            var estudianteId = await _context.Estudiantes
                .Where(e => e.Registro == dto.Registro)
                .Select(e => e.Id)
                .FirstOrDefaultAsync(ct);

            if (estudianteId == 0)
                return NotFound(new { mensaje = $"Estudiante {dto.Registro} no encontrado." });

            // 🔹 Crear inscripción visible inmediatamente (PENDIENTE)
            var inscripcion = new Inscripcion
            {
                EstudianteId = estudianteId,
                PeriodoId = dto.PeriodoId,
                Fecha = DateTime.UtcNow,
                Estado = "PENDIENTE"
            };

            _context.Inscripciones.Add(inscripcion);
            await _context.SaveChangesAsync(ct);

            // 🔹 Armar payload con el Id recién creado
            var payloadObj = new
            {
                Registro = dto.Registro.Trim().ToUpperInvariant(),
                PeriodoId = dto.PeriodoId,
                Materias = dto.Materias.Select(m => new
                {
                    MateriaCodigo = m.MateriaCodigo.Trim().ToUpperInvariant(),
                    Grupo = m.Grupo.Trim().ToUpperInvariant()
                }),
                InscripcionId = inscripcion.Id   // 🔥 CLAVE: el Processor lo usará para actualizar
            };

            // 🔹 Crear transacción asíncrona
            var tx = new Transaccion
            {
                TipoOperacion = "POST",
                Entidad = "Inscripcion",
                Payload = JsonSerializer.Serialize(payloadObj),
                Estado = "EN_COLA",
                Priority = Math.Clamp(priority, 0, 2),
                NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
                CallbackUrl = _cfg["Webhook:DefaultUrl"],
                CallbackSecret = _cfg["Webhook:DefaultSecret"],
                CreatedAt = DateTimeOffset.UtcNow,
                IdempotencyKey = dto.IdempotencyKey
            };
            _context.Transacciones.Add(tx);
            await _context.SaveChangesAsync(ct);
            await _qm.EnqueueAsync(tx, queue, ct);

            _log.LogInformation("🟡 Inscripción PENDIENTE encolada (TxId={TxId}, Registro={Registro})", tx.Id, dto.Registro);

            return Ok(new
            {
                mensaje = "Solicitud encolada (pendiente de procesamiento).",
                estado = "PENDIENTE",
                transactionId = tx.Id,
                inscripcion = new
                {
                    id = inscripcion.Id,
                    registro = dto.Registro,
                    periodoId = dto.PeriodoId,
                    materias = dto.Materias.Select(m => new { codigo = m.MateriaCodigo, grupo = m.Grupo }),
                    fecha = inscripcion.Fecha
                }
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "💥 Error al crear inscripción asincrónica para {Registro}", dto.Registro);
            return StatusCode(500, new
            {
                mensaje = "Error interno del servidor.",
                detalle = ex.Message
            });
        }
    }
    // ===========================================
    // GET /api/inscripciones/estado-inscripcion/{registro}
    // ===========================================
    // [HttpGet("estado-inscripcion/{registro}")]
    // public async Task<IActionResult> GetEstadoInscripcion(string registro, [FromServices] ServicioAContext db)
    // {
    //     if (string.IsNullOrWhiteSpace(registro))
    //         return BadRequest(new { mensaje = "El registro del estudiante es requerido." });

    //     registro = registro.Trim().ToUpperInvariant();
    //     _log.LogInformation("🔍 Consultando estado de inscripción del estudiante {Registro}", registro);

    //     var estudiante = await db.Estudiantes
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(e => e.Registro == registro);

    //     if (estudiante == null)
    //         return NotFound(new { mensaje = $"No existe estudiante con registro {registro}." });

    //     // 🧠 Importante: usar ToListAsync() antes del Select que contiene el array
    //     var inscripciones = await db.Inscripciones
    //         .AsNoTracking()
    //         .Include(i => i.Detalles)
    //             .ThenInclude(d => d.GrupoMateria)
    //                 .ThenInclude(g => g.Materia)
    //         .Where(i => i.EstudianteId == estudiante.Id)
    //         .OrderByDescending(i => i.Fecha)
    //         .ToListAsync(); // ✅ materializamos primero en memoria

    //     var resultado = inscripciones.Select(i => new
    //     {
    //         i.Id,
    //         i.Estado,
    //         i.Fecha,
    //         i.PeriodoId,
    //         Materias = i.Detalles.Any()
    //             ? i.Detalles.Select(d => new
    //             {
    //                 d.GrupoMateria.Materia.Codigo,
    //                 d.GrupoMateria.Materia.Nombre,
    //                 d.GrupoMateria.Grupo,
    //                 d.Estado
    //             })
    //             : new[]
    //             {
    //                 new
    //                 {
    //                     Codigo = "(pendiente)",
    //                     Nombre = "(sin procesar)",
    //                     Grupo = "-",
    //                     Estado = "PENDIENTE"
    //                 }
    //             }
    //     }).ToList();

    //     if (!resultado.Any())
    //         return Ok(new { mensaje = "El estudiante no tiene inscripciones registradas." });

    //     _log.LogInformation("📋 {Cantidad} inscripciones encontradas para {Registro}", resultado.Count, registro);
    //     return Ok(resultado);
    // }

    // ===========================================
    // GET /api/inscripciones/estado-inscripcion/{registro}
    // ===========================================
    [HttpGet("estado-inscripcion/{registro}")]
    public async Task<IActionResult> GetEstadoInscripcion(string registro, [FromServices] ServicioAContext db)
    {
        if (string.IsNullOrWhiteSpace(registro))
            return BadRequest(new { mensaje = "El registro del estudiante es requerido." });

        registro = registro.Trim().ToUpperInvariant();
        _log.LogInformation("🔍 Consultando estado de inscripción del estudiante {Registro}", registro);

        var estudiante = await db.Estudiantes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Registro == registro);

        if (estudiante == null)
            return NotFound(new { mensaje = $"No existe estudiante con registro {registro}." });

        var inscripciones = await db.Inscripciones
            .AsNoTracking()
            .Include(i => i.Detalles)
                .ThenInclude(d => d.GrupoMateria)
                    .ThenInclude(g => g.Materia)
            .Where(i => i.EstudianteId == estudiante.Id)
            .OrderByDescending(i => i.Fecha)
            .ToListAsync();

        var resultado = new List<object>();

        foreach (var i in inscripciones)
        {
            // ✅ Si ya tiene detalles confirmados
            if (i.Detalles.Any())
            {
                resultado.Add(new
                {
                    i.Id,
                    i.Estado,
                    i.Fecha,
                    i.PeriodoId,
                    Materias = i.Detalles.Select(d => new
                    {
                        d.GrupoMateria.Materia.Codigo,
                        d.GrupoMateria.Materia.Nombre,
                        d.GrupoMateria.Grupo,
                        d.Estado
                    })
                });
                continue;
            }

            // ✅ Si no tiene detalles, buscar la transacción asociada (InscripcionId exacto)
            var tx = await db.Transacciones
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(t =>
                    t.Entidad == "Inscripcion" &&
                    t.Payload.Contains($"\"InscripcionId\":{i.Id}"));

            if (tx != null && !string.IsNullOrWhiteSpace(tx.Payload))
            {
                try
                {
                    using var doc = JsonDocument.Parse(tx.Payload);
                    if (doc.RootElement.TryGetProperty("Materias", out var materiasJson))
                    {
                        var materiasSolicitadas = new List<(string Codigo, string Grupo)>();
                        foreach (var m in materiasJson.EnumerateArray())
                        {
                            var codigo = m.GetProperty("MateriaCodigo").GetString() ?? "(desconocido)";
                            var grupo = m.GetProperty("Grupo").GetString() ?? "-";
                            materiasSolicitadas.Add((codigo, grupo));
                        }

                        // Optimización N+1: Buscar todos los nombres de materia en una sola consulta
                        var codigosMaterias = materiasSolicitadas.Select(m => m.Codigo).Distinct().ToList();
                        var nombresMaterias = await db.Materias
                            .AsNoTracking()
                            .Where(mat => codigosMaterias.Contains(mat.Codigo.ToUpper()))
                            .ToDictionaryAsync(mat => mat.Codigo.ToUpper(), mat => mat.Nombre);

                        var materiasConNombre = materiasSolicitadas.Select(m =>
                        {
                            nombresMaterias.TryGetValue(m.Codigo, out var nombre);
                            return new
                            {
                                Codigo = m.Codigo,
                                Nombre = nombre ?? "(pendiente de confirmación)",
                                Grupo = m.Grupo,
                                Estado = "PENDIENTE"
                            };
                        }).ToList();

                        resultado.Add(new
                        {
                            i.Id,
                            i.Estado,
                            i.Fecha,
                            i.PeriodoId,
                            Materias = materiasConNombre
                        });
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning("⚠️ Error leyendo payload de Transacción para {Registro}: {Msg}", registro, ex.Message);
                }
            }

            // 🚨 Si no hay transacción o no se pudo leer el payload
            resultado.Add(new
            {
                i.Id,
                i.Estado,
                i.Fecha,
                i.PeriodoId,
                Materias = new[]
                {
                new
                {
                    Codigo = "(pendiente)",
                    Nombre = "(sin procesar)",
                    Grupo = "-",
                    Estado = "PENDIENTE"
                }
            }
            });
        }

        if (!resultado.Any())
            return Ok(new { mensaje = "El estudiante no tiene inscripciones registradas." });

        _log.LogInformation("📋 {Cantidad} inscripciones encontradas para {Registro}", resultado.Count, registro);
        return Ok(resultado);
    }

    // ===========================================
    // DELETE /api/inscripciones/{inscripcionId}
    // ===========================================
    [HttpDelete("{inscripcionId:int}")]
    [Authorize] // 🔐 ¡Importante! Protegemos el endpoint.
    public async Task<IActionResult> CancelarInscripcion(int inscripcionId, CancellationToken ct)
    {
        // 1. Obtener el registro del estudiante desde el token JWT.
        var registroEstudiante = User.Claims.FirstOrDefault(c => c.Type == "Registro")?.Value; // ✨ CORREGIDO: Usar "Registro" para que coincida con el token.
        if (string.IsNullOrEmpty(registroEstudiante))
        {
            return Unauthorized(new { mensaje = "Token inválido o no contiene el registro del estudiante." });
        }

        _log.LogInformation("📥 Solicitud de cancelación para InscripcionId {InscripcionId} por parte de {Registro}", inscripcionId, registroEstudiante);

        // 2. Iniciar una transacción para garantizar la atomicidad (o se hace todo, o no se hace nada).
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // 3. Buscar la inscripción, incluyendo sus detalles, el estudiante y las materias para la validación y logs.
            var inscripcion = await _context.Inscripciones
                .Include(i => i.Estudiante)
                .Include(i => i.Detalles)
                    .ThenInclude(d => d.GrupoMateria)
                        .ThenInclude(g => g.Materia)
                .FirstOrDefaultAsync(i => i.Id == inscripcionId, ct);

            if (inscripcion == null)
            {
                return NotFound(new { mensaje = "Inscripción no encontrada." });
            }

            // 4. Validar que el estudiante autenticado es el dueño de la inscripción.
            if (inscripcion.Estudiante.Registro != registroEstudiante)
            {
                _log.LogWarning("🚫 Acceso denegado: {RegistroA} intentó cancelar inscripción de {RegistroB}", registroEstudiante, inscripcion.Estudiante.Registro);
                return Forbid("No tienes permiso para cancelar esta inscripción.");
            }

            // 5. Devolver los cupos a los grupos de materias correspondientes.
            foreach (var detalle in inscripcion.Detalles)
            {
                detalle.GrupoMateria.Cupo++;
                _log.LogInformation("📈 Cupo devuelto para {Codigo}-{Grupo}. Nuevo cupo: {Cupo}", detalle.GrupoMateria.Materia.Codigo, detalle.GrupoMateria.Grupo, detalle.GrupoMateria.Cupo);
            }

            // 6. Eliminar los detalles de la inscripción y luego la inscripción principal.
            _context.DetallesInscripciones.RemoveRange(inscripcion.Detalles);
            _context.Inscripciones.Remove(inscripcion);

            await _context.SaveChangesAsync(ct);
            await dbTransaction.CommitAsync(ct); // Confirmar todos los cambios en la base de datos.

            _log.LogInformation("✅ Inscripción {InscripcionId} cancelada exitosamente por {Registro}", inscripcionId, registroEstudiante);
            return Ok(new { mensaje = "Inscripción cancelada exitosamente." });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(ct); // Si algo falla, revertir todo.
            _log.LogError(ex, "💥 Error al cancelar la inscripción {InscripcionId}", inscripcionId);
            return StatusCode(500, new { mensaje = "Error interno al cancelar la inscripción." });
        }
    }
}
