using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Consulta el estado operativo (paused) de las colas desde Redis.
/// </summary>
public class QueueStateService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _prefix;

    public QueueStateService(IConnectionMultiplexer redis, IConfiguration cfg)
    {
        _redis = redis;
        _prefix = cfg.GetValue<string>("RedisQueue:KeyPrefix") ?? "q:";
    }

    /// <summary>Indica si la cola está pausada (paused=1 en Redis).</summary>
    public async Task<bool> IsPausedAsync(string queue)
    {
        var db = _redis.GetDatabase();
        var pausedRaw = await db.StringGetAsync($"{_prefix}{queue}:paused");
        return pausedRaw.HasValue && pausedRaw.ToString() == "1";
    }
}
