using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class EstudiantesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IConfiguration _cfg;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;

    public EstudiantesController(ServicioAContext context, IConfiguration cfg, QueueManager qm, ITransaccionStore store)
    {
        _context = context;
        _cfg = cfg;
        _qm = qm;
        _store = store;
    }

    // === SÍNCRONOS ===

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EstudianteResponseDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Estudiantes
            .AsNoTracking()
            .Include(e => e.Carrera)
            .ToListAsync(ct);

        return Ok(list.Select(ToResponseDTO));
    }

    [HttpGet("{registro}")]
    public async Task<ActionResult<EstudianteRequestDto>> GetByRegistro(string registro, CancellationToken ct)
    {
        var e = await _context.Estudiantes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Registro == registro, ct);

        return e is null ? NotFound() : Ok(ToDTO(e));
    }

    [HttpPost]
    public async Task<ActionResult<EstudianteRequestDto>> Create([FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var e = new Estudiante
        {
            Registro = dto.Registro,
            Ci = dto.Ci,
            Nombre = dto.Nombre,
            Email = dto.Email,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            CarreraId = dto.CarreraId,
            PasswordHash = passwordHash
        };

        _context.Estudiantes.Add(e);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetByRegistro), new { registro = e.Registro }, ToDTO(e));
    }

    [HttpPut("{registro}")]
    public async Task<IActionResult> Update(string registro, [FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Registro == registro, ct);
        if (e is null) return NotFound();

        e.Ci = dto.Ci;
        e.Nombre = dto.Nombre;
        e.Email = dto.Email;
        e.Telefono = dto.Telefono;
        e.Direccion = dto.Direccion;
        e.Estado = dto.Estado;
        e.CarreraId = dto.CarreraId;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            e.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{registro}")]
    public async Task<IActionResult> Delete(string registro, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Registro == registro, ct);
        if (e is null) return NotFound();

        _context.Estudiantes.Remove(e);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // === ASÍNCRONOS ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearEstudianteAsync(
        [FromBody] EstudianteRequestDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Estudiante",
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
    public async Task<IActionResult> ActualizarEstudianteAsync(
        string registro,
        [FromBody] EstudianteRequestDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        dto.Registro = registro;

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Estudiante",
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
    public async Task<IActionResult> EliminarEstudianteAsync(
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
            Entidad = "Estudiante",
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

    // === LOGIN (NO TOCAR) ===
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto login)
    {
        var estudiante = _context.Estudiantes
            .FirstOrDefault(e => e.Registro == login.Registro);

        if (estudiante == null || !BCrypt.Net.BCrypt.Verify(login.Password, estudiante.PasswordHash))
        {
            return Unauthorized("Registro o contraseña inválidos ❌");
        }

        var key = Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]);
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Registro", estudiante.Registro),
                new Claim("Nombre", estudiante.Nombre)
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Issuer = _cfg["Jwt:Issuer"],
            Audience = _cfg["Jwt:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Ok(new { token = jwt });
    }

    // === Mappers ===
    private static EstudianteRequestDto ToDTO(Estudiante e) => new()
    {
        Id = e.Id,
        Registro = e.Registro,
        Ci = e.Ci,
        Nombre = e.Nombre,
        Email = e.Email,
        Telefono = e.Telefono,
        Direccion = e.Direccion,
        CarreraId = e.CarreraId
    };

    private static EstudianteResponseDto ToResponseDTO(Estudiante e) => new()
    {
        Registro = e.Registro,
        Ci = e.Ci,
        Nombre = e.Nombre,
        Email = e.Email,
        Telefono = e.Telefono,
        Direccion = e.Direccion,
        Estado = e.Estado,
        Carrera = e.Carrera == null ? null : new CarreraDto
        {
            Id = e.Carrera.Id,
            Nombre = e.Carrera.Nombre
        }
    };
}