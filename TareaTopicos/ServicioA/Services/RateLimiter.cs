using System.Collections.Concurrent;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Rate/Concurrency limiter simple por cola:
/// - MaxInFlight: tope de elementos en proceso (semaforos por cola).
/// (Para TPS/burst podrías extenderlo luego con contadores por ventana).
/// </summary>
public sealed class RateLimiter
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _inflight = new();
    private readonly ConcurrentDictionary<string, int> _caps = new();

    public void Configure(string queueName, int maxInFlight)
    {
        _caps[queueName] = Math.Max(1, maxInFlight);
        _inflight.AddOrUpdate(queueName, _ => new SemaphoreSlim(_caps[queueName], _caps[queueName]),
                                        (_, s) => {
                                            // ajustar capacidad (crear nuevo semáforo si cambia)
                                            if (s.CurrentCount + (0) != _caps[queueName])
                                            {
                                                s.Dispose();
                                                return new SemaphoreSlim(_caps[queueName], _caps[queueName]);
                                            }
                                            return s;
                                        });
    }

    public async Task AcquireAsync(string queueName, CancellationToken ct)
    {
        if (!_inflight.TryGetValue(queueName, out var sem))
        {
            // default 50 si no fue configurado
            sem = _inflight.GetOrAdd(queueName, _ => new SemaphoreSlim(50, 50));
            _caps[queueName] = 50;
        }
        await sem.WaitAsync(ct);
    }

    public void Release(string queueName)
    {
        if (_inflight.TryGetValue(queueName, out var sem))
            sem.Release();
    }

    public int GetMaxInFlight(string queueName) => _caps.TryGetValue(queueName, out var v) ? v : 50;
}
