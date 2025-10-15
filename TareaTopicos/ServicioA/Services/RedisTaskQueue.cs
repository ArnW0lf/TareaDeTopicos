using System.Text.Json;
using StackExchange.Redis;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Cola simple en Redis usando Listas (por ahora sin visibilidad/ACK).
/// Claves: q:{queueName}:p:{priority}
/// </summary>
public class RedisTaskQueue : IBackgroundTaskQueue
{
    private readonly IConnectionMultiplexer _mux;
    private readonly string _keyPrefix;
    private const string DefaultQueue = "default";

    public RedisTaskQueue(IConnectionMultiplexer mux, IConfiguration cfg)
    {
        _mux = mux;
        _keyPrefix = cfg.GetValue<string>("RedisQueue:KeyPrefix") ?? "q:";
    }

    private string KeyFor(string queueName, int priority)
        => $"{_keyPrefix}{(string.IsNullOrWhiteSpace(queueName) ? DefaultQueue : queueName)}:p:{priority}";

    public async Task EnqueueAsync(Transaccion transaccion, string? queueName = null, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var key = KeyFor(queueName ?? DefaultQueue, transaccion.Priority);
        var payload = JsonSerializer.Serialize(transaccion);
        // Push a la derecha = productor
        await db.ListRightPushAsync(key, payload);
    }

    public async Task<Transaccion?> TryDequeueAsync(CancellationToken ct = default, string? queueName = null)
    {
        var db = _mux.GetDatabase();
        var qname = string.IsNullOrWhiteSpace(queueName) ? DefaultQueue : queueName;

        // Prioridades 0..2 (0 = alta). Siéntete libre de parametrizar luego.
        foreach (var p in new[] { 0, 1, 2 })
        {
            var key = KeyFor(qname, p);
            // Pop a la izquierda = consumidor
            var value = await db.ListLeftPopAsync(key);
            if (value.HasValue)
            {
                try
                {
                    var tx = JsonSerializer.Deserialize<Transaccion>(value!);
                    return tx;
                }
                catch
                {
                    // Si hay basura en la cola, la descartamos (o podrías enviar a DLQ).
                }
            }
        }

        return null;
    }
    public async Task<long> GetBacklogSizeAsync(string queueName)
{
    var db = _mux.GetDatabase();
    var qname = string.IsNullOrWhiteSpace(queueName) ? DefaultQueue : queueName;

    long total = 0;
    foreach (var p in new[] { 0, 1, 2 })
    {
        var key = KeyFor(qname, p);
        total += await db.ListLengthAsync(key);
    }
    return total;
}

}
