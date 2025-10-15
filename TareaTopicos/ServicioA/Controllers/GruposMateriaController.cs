using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class GruposMateriaController : ControllerBase
{
    private readonly ServicioAContext _context;
    public GruposMateriaController(ServicioAContext context) => _context = context;
/*
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GrupoMateriaResponseDto>>> Get([FromQuery] int? periodoId, [FromQuery] int? materiaId, [FromQuery] bool soloActivos = true, CancellationToken ct = default)
    {
        var q = _context.GruposMaterias.AsNoTracking();

        if (periodoId.HasValue) q = q.Where(g => g.PeriodoId == periodoId.Value);
        if (materiaId.HasValue) q = q.Where(g => g.MateriaId == materiaId.Value);
        if (soloActivos) q = q.Where(g => g.Estado == "ACTIVO");

        var items = await q.OrderBy(g => g.MateriaId).ThenBy(g => g.Grupo).ToListAsync(ct);
        return Ok(items.Select(ToDTO));
    }

    */
    /*
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GrupoMateriaRequestDto>> GetById(int id, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return g is null ? NotFound() : Ok(ToDTO(g));
    }
    */

    [HttpGet]
public async Task<ActionResult<IEnumerable<GrupoMateriaResponseDto>>> GetTodos(CancellationToken ct = default)
{
    var grupos = await _context.GruposMaterias
        .AsNoTracking()
        .Include(x => x.Materia)
            .ThenInclude(m => m.Nivel)
        .Include(x => x.Docente)
        .Include(x => x.Periodo)
        .Include(x => x.Horario)
        .Include(x => x.Aula)
        .OrderBy(x => x.MateriaId)
        .ThenBy(x => x.Grupo)
        .ToListAsync(ct);

    var completos = grupos
        .Where(g => g.Materia != null &&
                    g.Materia.Nivel != null &&
                    g.Docente != null &&
                    g.Periodo != null &&
                    g.Horario != null &&
                    g.Aula != null)
        .Select(ToResponseDTO)
        .ToList();

    return Ok(completos);
}
    [HttpGet("{id:int}")]
public async Task<ActionResult<GrupoMateriaResponseDto>> GetById(int id, CancellationToken ct)
{
    var g = await _context.GruposMaterias
        .AsNoTracking()
        .Include(x => x.Materia)
            .ThenInclude(m => m.Nivel)
        .Include(x => x.Docente)
        .Include(x => x.Periodo)
        .Include(x => x.Horario)
        .Include(x => x.Aula)
        .FirstOrDefaultAsync(x => x.Id == id, ct);

    if (g is null)
        return NotFound();

    // Validación defensiva para evitar NullReferenceException
    if (g.Materia is null || g.Materia.Nivel is null || g.Docente is null || g.Periodo is null || g.Horario is null || g.Aula is null)
        return BadRequest($"GrupoMateria con ID {id} tiene relaciones incompletas.");

    return Ok(ToResponseDTO(g));
}
    [HttpPost]
    public async Task<ActionResult<GrupoMateriaRequestDto>> Create([FromBody] GrupoMateriaRequestDto dto, CancellationToken ct)
    {
        var entity = new GrupoMateria
        {
            Grupo = dto.Grupo,
            Cupo = dto.Cupo,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            MateriaId = dto.MateriaId,
            DocenteId = dto.DocenteId,
            PeriodoId = dto.PeriodoId,
            HorarioId = dto.HorarioId,
            AulaId = dto.AulaId
        };
        _context.GruposMaterias.Add(entity);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GrupoMateriaRequestDto dto, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();

        g.Grupo = dto.Grupo;
        g.Cupo = dto.Cupo;
        g.Estado = dto.Estado;
        g.MateriaId = dto.MateriaId;
        g.DocenteId = dto.DocenteId;
        g.PeriodoId = dto.PeriodoId;
        g.HorarioId = dto.HorarioId;
        g.AulaId = dto.AulaId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();
        _context.GruposMaterias.Remove(g);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static GrupoMateriaRequestDto ToDTO(GrupoMateria g) => new()
    {
        Id = g.Id,
        Grupo = g.Grupo,
        Cupo = g.Cupo,
        Estado = g.Estado,
        MateriaId = g.MateriaId,
        DocenteId = g.DocenteId,
        PeriodoId = g.PeriodoId,
        HorarioId = g.HorarioId,
        AulaId = g.AulaId
    };


    //PARA LECTURA DE OBJETOS DENTRO DE INSCRIPCION
   private static GrupoMateriaResponseDto ToResponseDTO(GrupoMateria m) => new()
     {
        Id = m.Id,
        Grupo = m.Grupo,
        Cupo = m.Cupo,
        Estado = m.Estado,
        Materia = new MateriaResponseDto
        {
         Id = m.Materia.Id,
         Codigo = m.Materia.Codigo,
         Nombre = m.Materia.Nombre,
         Creditos = m.Materia.Creditos,
             Nivel = new NivelDto
             {
              Id = m.Materia.Nivel.Id,
              Numero = m.Materia.Nivel.Numero,
              Nombre = m.Materia.Nivel.Nombre
             }
       },
        Docente = new DocenteDto
        {
            Id = m.Docente.Id,
            Registro = m.Docente.Registro,
            Ci = m.Docente.Ci,
            Nombre = m.Docente.Nombre,
            Telefono = m.Docente.Telefono,
            Estado = m.Docente.Estado
        },
        Periodo = new PeriodoAcademicoResponseDto
        {
            Gestion = m.Periodo.Gestion,
            FechaInicio = m.Periodo.FechaInicio,
            FechaFin = m.Periodo.FechaFin
        },
        Horario = new HorarioDto
        {
            Id = m.Horario.Id,
            Dia = m.Horario.Dia,
            HoraInicio = m.Horario.HoraInicio,
            HoraFin = m.Horario.HoraFin
        },
        Aula = new AulaDto
        { 
            Id = m.Aula.Id,
            Codigo = m.Aula.Codigo,
            Capacidad = m.Aula.Capacidad,
            Ubicacion = m.Aula.Ubicacion
        }
     };  
}
