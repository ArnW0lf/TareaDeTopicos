using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Reclamación de "in-flight": si un worker muere y no ACKea,
/// los mensajes vencidos se devuelven a la cola.
/// Requiere que, al tomar un mensaje, se mueva a "inflight".
/// (Puedes integrar esto más adelante con WorkerService.)
/// </summary>
public sealed class VisibilityReclaimer
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<VisibilityReclaimer> _logger;
    private readonly string _prefix;
    private readonly TimeSpan _visibility;

    public VisibilityReclaimer(IConnectionMultiplexer mux, IConfiguration cfg, ILogger<VisibilityReclaimer> logger)
    {
        _mux = mux;
        _logger = logger;
        _prefix = cfg.GetValue<string>("RedisQueue:KeyPrefix") ?? "q:";
        var visSec = Math.Max(10, cfg.GetValue<int?>("RedisQueue:VisibilityTimeoutSeconds") ?? 60);
        _visibility = TimeSpan.FromSeconds(visSec);
    }

    private string InflightKey(string queue) => $"{_prefix}{queue}:inflight"; // zset score = deadline (unix ms)
    private string PriKey(string queue, int p) => $"{_prefix}{queue}:p:{p}";

    /// <summary>
    /// Escanea el zset de inflight y devuelve a la cola los mensajes cuyo deadline ya pasó.
    /// </summary>
    public async Task<int> ReclaimExpiredAsync(string queueName, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var inf = InflightKey(queueName);

        // Trae todos los expirados (score <= ahora)
        var expired = await db.SortedSetRangeByScoreAsync(inf, double.NegativeInfinity, now);
        if (expired.Length == 0) return 0;

        var reclaimed = 0;

        foreach (var raw in expired)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var tx = JsonSerializer.Deserialize<Transaccion>(raw!);
                if (tx is null) continue;

                // Remueve del inflight y reencola por prioridad original (o 1)
                await db.SortedSetRemoveAsync(inf, raw);
                var prio = tx.Priority >= 0 ? tx.Priority : 1;
                await db.ListRightPushAsync(PriKey(queueName, prio), JsonSerializer.Serialize(tx));
                reclaimed++;
            }
            catch
            {
                // Si está corrupto, lo sacamos del inflight para no ciclar
                await db.SortedSetRemoveAsync(inf, raw);
            }
        }

        if (reclaimed > 0)
            _logger.LogWarning("Reclaimer: {count} mensajes devueltos a {queue}", reclaimed, queueName);

        return reclaimed;
    }

    /// <summary>
    /// Registra un mensaje como "in-flight" con deadline = now + visibilityTimeout.
    /// (Llámenlo cuando tomen un mensaje; quítenlo al ACK.)
    /// </summary>
    public async Task MarkInFlightAsync(string queueName, Transaccion tx, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var deadline = DateTimeOffset.UtcNow.Add(_visibility).ToUnixTimeMilliseconds();
        await db.SortedSetAddAsync(InflightKey(queueName), JsonSerializer.Serialize(tx), deadline);
    }

    /// <summary>Quita un mensaje del inflight (después de ACK).</summary>
    public async Task RemoveFromInFlightAsync(string queueName, Transaccion tx, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var raw = JsonSerializer.Serialize(tx);
        await db.SortedSetRemoveAsync(InflightKey(queueName), raw);
    }
}
