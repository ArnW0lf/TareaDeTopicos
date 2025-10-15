using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TAREATOPICOS.ServicioA.Data;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
public class ReadinessController : ControllerBase
{
    private readonly ServicioAContext _db;
    private readonly IConnectionMultiplexer _redis;

    public ReadinessController(ServicioAContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    [HttpGet("/readiness")]
    public async Task<IActionResult> Readiness(CancellationToken ct)
    {
        // Postgres OK?
        try { await _db.Niveles.AsNoTracking().Take(1).ToListAsync(ct); }
        catch { return StatusCode(503, new { db = "down" }); }

        // Redis OK?
        try { var _ = await _redis.GetDatabase().PingAsync(); }
        catch { return StatusCode(503, new { redis = "down" }); }

        return Ok(new { status = "ready" });
    }
}
