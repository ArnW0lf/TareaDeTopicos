// File: Controllers/AdminController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Services.Processors;


namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IConnectionMultiplexer _mux;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly DeadLetterService _dlq;
    private readonly VisibilityReclaimer _reclaimer;
    private readonly IQueueProcessor _processor;
    private readonly IConfiguration _cfg;
    private readonly string _prefix;
    private readonly IConnectionMultiplexer _redis;
    private readonly WorkerHost _workerHost;

    public AdminController(
        IConnectionMultiplexer mux,
        QueueManager qm,
        ITransaccionStore store,
        DeadLetterService dlq,
        VisibilityReclaimer reclaimer,
        IEnumerable<IQueueProcessor> processors, // inyecta todos; tomamos Default para process-once
        IConfiguration cfg,
        IConnectionMultiplexer redis,
        WorkerHost workerHost) 
    {
        _mux = mux;
        _qm = qm;
        _store = store;
        _dlq = dlq;
        _reclaimer = reclaimer;
        _processor = processors.FirstOrDefault(p => p is DefaultProcessor) ?? processors.First();
        _cfg = cfg;
        _prefix = _cfg.GetValue<string>("RedisQueue:KeyPrefix") ?? "q:";
        _redis = redis;
        _workerHost = workerHost;
    }

    // --- PAUSE / RESUME ---
    [HttpPost("queues/{name}/pause")]
    public async Task<IActionResult> Pause(string name)
    {
        var db = _mux.GetDatabase();
        await db.StringSetAsync($"{_prefix}{name}:paused", "1");
        return Ok(new { queue = name, paused = true });
    }

    [HttpPost("queues/{name}/resume")]
    public async Task<IActionResult> Resume(string name)
    {
        var db = _mux.GetDatabase();
        await db.StringSetAsync($"{_prefix}{name}:paused", "0");
        return Ok(new { queue = name, paused = false });
    }

    // --- STATS ---
    [HttpGet("queues/{name}/stats")]
    public async Task<IActionResult> Stats(string name)
    {
        var db = _mux.GetDatabase();

        var priorities = _cfg.GetSection("Queues")
            .Get<List<TAREATOPICOS.ServicioA.Options.QueueItemOptions>>()
            ?.FirstOrDefault(q => (q.Name ?? "default") == name)?.Priorities ?? 3;

        var depths = new List<object>();
        for (int p = 0; p < priorities; p++)
        {
            var len = await db.ListLengthAsync($"{_prefix}{name}:p:{p}");
            depths.Add(new { priority = p, depth = len });
        }

        var dlqLen = await db.ListLengthAsync($"{_prefix}{name}:dlq");
        var inflightLen = await db.SortedSetLengthAsync($"{_prefix}{name}:inflight");

        var pausedRaw = await db.StringGetAsync($"{_prefix}{name}:paused");
        var paused = pausedRaw.HasValue && pausedRaw.ToString() == "1";

        return Ok(new
        {
            queue = name,
            paused,
            priorities,
            depths,
            inflight = inflightLen,
            dlq = dlqLen
        });
    }

    // --- DLQ replay ---
    [HttpPost("queues/{name}/dlq/replay")]
    public async Task<IActionResult> DlqReplay(string name, [FromQuery] int max = 50, CancellationToken ct = default)
    {
        var count = await _dlq.ReplayAsync(name, Math.Max(1, max), ct);
        return Ok(new { queue = name, replayed = count });
    }

    // --- Reclaim in-flight expirados ---
    [HttpPost("queues/{name}/reclaim")]
    public async Task<IActionResult> Reclaim(string name, CancellationToken ct = default)
    {
        var count = await _reclaimer.ReclaimExpiredAsync(name, ct);
        return Ok(new { queue = name, reclaimed = count });
    }

    // --- Run-now: reencolar con prioridad alta YA ---
    [HttpPost("queues/{name}/run-now/{id:guid}")]
    public async Task<IActionResult> RunNow(string name, Guid id, CancellationToken ct = default)
    {
        var tx = await _store.GetAsync(id, ct);
        if (tx is null) return NotFound(new { mensaje = "Transacción no encontrada" });

        tx.Priority = 0;
        tx.NotBefore = DateTimeOffset.UtcNow;

        await _qm.EnqueueAsync(tx, name, ct);
        return Ok(new { queue = name, id = tx.Id, enqueued = true, priority = tx.Priority });
    }

    // --- Process-once: procesa 1 tarea en el hilo del request (solo demo) ---
    [HttpPost("queues/{name}/process-once")]
    public async Task<IActionResult> ProcessOnce(string name, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();

        var priorities = _cfg.GetSection("Queues")
            .Get<List<TAREATOPICOS.ServicioA.Options.QueueItemOptions>>()
            ?.FirstOrDefault(q => (q.Name ?? "default") == name)?.Priorities ?? 3;

        string? raw = null;
        int pulledPrio = -1;

        for (int p = 0; p < priorities; p++)
        {
            var val = await db.ListLeftPopAsync($"{_prefix}{name}:p:{p}");
            if (val.HasValue)
            {
                raw = val.ToString();
                pulledPrio = p;
                break;
            }
        }

        if (raw is null)
            return Ok(new { queue = name, processed = 0, message = "no hay tareas" });

        Transaccion? tx;
        try { tx = JsonSerializer.Deserialize<Transaccion>(raw); }
        catch { tx = null; }

        if (tx is null)
            return Ok(new { queue = name, processed = 0, message = "mensaje corrupto descartado" });

        try
        {
            await _store.UpdateEstadoAsync(tx.Id, "PROCESANDO", null, ct);
            await _processor.ProcessAsync(tx, ct);
            await _store.UpdateEstadoAsync(tx.Id, "COMPLETADO", null, ct);

            return Ok(new
            {
                queue = name,
                processed = 1,
                id = tx.Id,
                pulledPriority = pulledPrio,
                status = "COMPLETADO"
            });
        }
        catch (Exception ex)
        {
            tx.Attempt++;
            await _dlq.SendToDlqAsync(name, tx, ex, ct);
            await _store.UpdateEstadoAsync(tx.Id, "ERROR", ex.Message, ct);

            return Ok(new
            {
                queue = name,
                processed = 0,
                id = tx.Id,
                error = ex.Message,
                sentTo = "DLQ"
            });
        }
    }
// PARA VER LOS DETALLES DE LA COLA 
// ========== LISTAR PENDIENTES (cola principal) ==========
[HttpGet("queues/{name}/peek")]
public async Task<IActionResult> Peek(
    string name,
    [FromQuery] int priority = -1,  // -1 = todas
    [FromQuery] int max = 20)
{
    var db = _mux.GetDatabase();
    var queuesSec = _cfg.GetSection("Queues")
        .Get<List<TAREATOPICOS.ServicioA.Options.QueueItemOptions>>();
    var prios = queuesSec?.FirstOrDefault(q => (q.Name ?? "default") == name)?.Priorities ?? 3;

    async Task<object[]> ReadListAsync(string key, int take)
    {
        var vals = await db.ListRangeAsync(key, 0, take - 1);
        return vals.Select(v => (object)new {
            raw = v.ToString()
        }).ToArray();
    }

    var result = new List<object>();

    if (priority >= 0)
    {
        var key = $"{_prefix}{name}:p:{priority}";
        var items = await ReadListAsync(key, max);
        return Ok(new { queue = name, priority, items });
    }

    for (int p = 0; p < prios; p++)
    {
        var key = $"{_prefix}{name}:p:{p}";
        var items = await ReadListAsync(key, max);
        result.Add(new { priority = p, items });
    }
    return Ok(new { queue = name, buckets = result });
}

// ========== LISTAR IN-FLIGHT (ordenados por vencimiento/score) ==========
[HttpGet("queues/{name}/inflight")]
public async Task<IActionResult> Inflight(string name, [FromQuery] int max = 20)
{
    var db = _mux.GetDatabase();
    var key = $"{_prefix}{name}:inflight";
    // Trae por score ascendente (los que vencen primero)
    var vals = await db.SortedSetRangeByRankAsync(key, 0, max - 1, Order.Ascending);
    var items = vals.Where(v => v.HasValue).Select(v => new { raw = v.ToString() }).ToArray();
    return Ok(new { queue = name, items });
}

// ========== LISTAR DLQ ==========
[HttpGet("queues/{name}/dlq/list")]
public async Task<IActionResult> DlqList(string name, [FromQuery] int max = 20)
{
    var db = _mux.GetDatabase();
    var key = $"{_prefix}{name}:dlq";
    var vals = await db.ListRangeAsync(key, 0, max - 1);
    // En DLQ tú sueles guardar { tx: {...}, error: "...", at: "..." }
    var items = vals.Select(v => new { raw = v.ToString() }).ToArray();
    return Ok(new { queue = name, items });
}
// ========== VER REDIS ==========
[HttpGet("redis/dump")]
public async Task<IActionResult> DumpRedisKey([FromQuery] string key)
{
    var db = _redis.GetDatabase();  // IConnectionMultiplexer ya está inyectado
    var values = await db.ListRangeAsync(key, 0, -1);
    var result = values.Select(x => x.ToString()).ToList();
    return Ok(result);
}

[HttpGet("admin/queues/{queue}/dlq/count")]
public async Task<IActionResult> GetDlqCount(string queue)
{
    var db = _redis.GetDatabase();
    var key = $"{_prefix}{queue}:dlq";
    var count = await db.ListLengthAsync(key);
    return Ok(new { queue, dlqCount = count });
}

// === LISTAR COLAS Y CONCURRENCIA ===
[HttpGet("queues")]
public IActionResult ListQueues()
{
    var queues = _workerHost.ListQueues();
    return Ok(queues);
}

// === CREAR COLA NUEVA ===
[HttpPost("queues/{name}")]
public IActionResult AddQueue(string name, [FromQuery] int workers = 1)
{
    if (_workerHost.AddQueue(name, workers))
        return Ok(new { queue = name, workers, created = true });

    return Conflict(new { queue = name, message = "Ya existe" });
}

// === AJUSTAR WORKERS ===
[HttpPatch("queues/{name}/scale")]
public IActionResult ScaleQueue(string name, [FromQuery] int workers)
{
    if (_workerHost.ScaleQueue(name, workers))
        return Ok(new { queue = name, workers, scaled = true });

    return NotFound(new { queue = name, message = "No existe" });
}

// === ELIMINAR COLA ===
[HttpDelete("queues/{name}")]
public async Task<IActionResult> RemoveQueue(string name)
{
    if (await _workerHost.RemoveQueueAsync(name))
        return Ok(new { queue = name, removed = true });

    return NotFound(new { queue = name, message = "No existe" });
}
// === MIGRAR WORKERS ENTRE COLAS ===
//migra n hilos de una cola a otra cola
[HttpPost("queues/migrate")]
public IActionResult MigrateWorkers(
    [FromQuery] string from,
    [FromQuery] string to,
    [FromQuery] int count)
{
    if (count <= 0)
        return BadRequest(new { message = "La cantidad debe ser mayor a 0" });

    if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        return BadRequest(new { message = "La cola origen y destino deben ser distintas" });

    var queues = _workerHost.ListQueues();

    if (!queues.ContainsKey(from))
        return NotFound(new { queue = from, message = "Cola origen no existe" });
    if (!queues.ContainsKey(to))
        return NotFound(new { queue = to, message = "Cola destino no existe" });

    var fromCurrent = queues[from];
    var toCurrent = queues[to];

    if (fromCurrent < count)
        return BadRequest(new { message = $"Cola {from} solo tiene {fromCurrent} workers" });

    // Quitar de origen
    _workerHost.ScaleQueue(from, fromCurrent - count);
    // Agregar a destino
    _workerHost.ScaleQueue(to, toCurrent + count);

    return Ok(new
    {
        from,
        to,
        moved = count,
        fromWorkers = fromCurrent - count,
        toWorkers = toCurrent + count
    });
}
// === BALANCEAR WORKERS ENTRE TODAS LAS COLAS ===
[HttpPost("queues/balance")]
public IActionResult BalanceWorkers()
{
    var queues = _workerHost.ListQueues();

    if (queues.Count == 0)
        return BadRequest(new { message = "No hay colas para balancear" });

    // promedio redondeado
    var avg = (int)Math.Round(queues.Values.Average());

    foreach (var kv in queues)
    {
        _workerHost.ScaleQueue(kv.Key, avg);
    }

    return Ok(new
    {
        balancedTo = avg,
        queues = _workerHost.ListQueues()
    });
}
// === REINTENTAR UNA TAREA PUNTUAL DEL DLQ ===
[HttpPost("queues/{name}/dlq/retry/{id:guid}")]
public async Task<IActionResult> DlqRetry(string name, Guid id, CancellationToken ct)
{
    var db = _mux.GetDatabase();
    var key = $"{_prefix}{name}:dlq";
    var vals = await db.ListRangeAsync(key, 0, -1);

    foreach (var v in vals)
    {
        if (!v.HasValue) continue;
        var raw = v.ToString();
        if (raw?.Contains(id.ToString()) == true)
        {
            // Eliminar del DLQ
            await db.ListRemoveAsync(key, v);

            // Reencolar en la cola normal con prioridad alta
            var tx = JsonSerializer.Deserialize<Transaccion>(raw!);
            if (tx != null)
            {
                tx.Priority = 0;
                tx.NotBefore = DateTimeOffset.UtcNow;
                await _qm.EnqueueAsync(tx, name, ct);
            }

            return Ok(new { queue = name, id, retried = true });
        }
    }

    return NotFound(new { queue = name, id, message = "No encontrado en DLQ" });
}
// === ELIMINAR UNA TAREA PUNTUAL DEL DLQ ===
[HttpDelete("queues/{name}/dlq/{id:guid}")]
public async Task<IActionResult> DlqDelete(string name, Guid id)
{
    var db = _mux.GetDatabase();
    var key = $"{_prefix}{name}:dlq";
    var vals = await db.ListRangeAsync(key, 0, -1);

    foreach (var v in vals)
    {
        if (!v.HasValue) continue;
        var raw = v.ToString();
        if (raw?.Contains(id.ToString()) == true)
        {
            await db.ListRemoveAsync(key, v);
            return Ok(new { queue = name, id, deleted = true });
        }
    }

    return NotFound(new { queue = name, id, message = "No encontrado en DLQ" });
}
// === MOVER TAREAS MANUALMENTE ENTRE COLAS ===
[HttpPost("queues/move")]
public async Task<IActionResult> MoveTasks(
    [FromQuery] string from,
    [FromQuery] string to,
    [FromQuery] int count,
    CancellationToken ct)
{
    if (count <= 0)
        return BadRequest(new { message = "La cantidad debe ser mayor a 0" });

    if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        return BadRequest(new { message = "La cola origen y destino deben ser distintas" });

    var db = _mux.GetDatabase();
    var fromKey = $"{_prefix}{from}:p:0"; // prioridad alta por defecto
    var moved = 0;

    for (int i = 0; i < count; i++)
    {
        var raw = await db.ListLeftPopAsync(fromKey);
        if (!raw.HasValue) break;

        var tx = JsonSerializer.Deserialize<Transaccion>(raw!);
        if (tx != null)
        {
            await _qm.EnqueueAsync(tx, to, ct);
            moved++;
        }
    }

    return Ok(new { from, to, moved });
}
    // =======================
    // CREAR COLA ESPECIALIZADA
    // =======================
    [HttpPost("queues")]
    public IActionResult AddQueue(
        [FromQuery] string name,
        [FromQuery] int workers,
        [FromQuery] string processor = "DefaultProcessor")
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Debe especificar un nombre de cola" });

        if (workers <= 0)
            return BadRequest(new { message = "Workers debe ser > 0" });

        var ok = _workerHost.AddQueue(name, workers, processor);
        if (!ok)
            return Conflict(new { message = $"Cola {name} ya existe" });

        return Ok(new { name, workers, processor });
    }
// }
[HttpGet("queues/{name}/transactions")]
public async Task<IActionResult> ListTransactions(string name, [FromQuery] int max = 50)
{
    var db = _mux.GetDatabase();
    var priorities = _cfg.GetSection("Queues")
        .Get<List<TAREATOPICOS.ServicioA.Options.QueueItemOptions>>()
        ?.FirstOrDefault(q => (q.Name ?? "default") == name)?.Priorities ?? 3;

    var result = new List<object>();

    for (int p = 0; p < priorities; p++)
    {
        var key = $"q:{name}:p:{p}";
        var vals = await db.ListRangeAsync(key, 0, max - 1);
        foreach (var v in vals)
        {
            if (v.HasValue)
            {
                try
                {
                    var tx = System.Text.Json.JsonSerializer.Deserialize<Transaccion>(v.ToString()!);
                    result.Add(new { priority = p, tx });
                }
                catch
                {
                    result.Add(new { priority = p, raw = v.ToString() });
                }
            }
        }
    }

    return Ok(new { queue = name, transactions = result });
}

}
