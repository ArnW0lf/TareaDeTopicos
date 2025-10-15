using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;
using Microsoft.Extensions.Options;
using TAREATOPICOS.ServicioA; // aquí está QueueOptions
using TAREATOPICOS.ServicioA.Services.Options;

namespace TAREATOPICOS.ServicioA.Services;

 
public class QueueManager
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;
    private readonly WorkerHost _workerHost;
    private readonly List<QueueOptions> _queueOptions;

    // Para round robin
    private static readonly ConcurrentDictionary<string, int> _rrIndexes = new();

    public QueueManager(
        IBackgroundTaskQueue queue,
        ITransaccionStore store,
        WorkerHost workerHost,
        IOptionsSnapshot<List<QueueOptions>> queueOptions)
    {
        _queue = queue;
        _store = store;
        _workerHost = workerHost;
        _queueOptions = queueOptions.Value;
    }

public async Task<string> EnqueueAsync(
    Transaccion tx,
    string? queueName = null,
    CancellationToken ct = default)
{
    tx.Estado = "EN_COLA";
    await _store.AddAsync(tx, ct);

    // 🔄 Selección de cola
    var allQueues = _workerHost.ListQueues().Keys.ToList();

    List<string> queues;
    if (string.IsNullOrWhiteSpace(queueName) || queueName == "balanced")
    {
        // Si hay más de 1 cola, balancea entre todas EXCEPTO default
        queues = allQueues.Where(x => x != "default").ToList();

        // 🚑 fallback: si no hay ninguna más, usar default
        if (queues.Count == 0)
            queues = new List<string> { "default" };
    }
    else
    {
        queues = new List<string> { queueName };
    }

    if (queues.Count == 0)
        throw new InvalidOperationException("No hay colas disponibles.");

    // ⚖️ Round Robin
    var key = string.Join("|", queues.OrderBy(x => x));
    var index = _rrIndexes.AddOrUpdate(key, 0, (_, old) => (old + 1) % queues.Count);
    var chosenQueue = queues[index];

    // 🚦 Admission control
    var opts = _queueOptions.FirstOrDefault(o => o.Name == chosenQueue)
               ?? new QueueOptions { Name = chosenQueue };

    var backlog = await _queue.GetBacklogSizeAsync(chosenQueue);

    if (backlog >= opts.MaxPendingTasks)
    {
        switch (opts.RejectPolicy.ToLowerInvariant())
        {
            case "reject":
                throw new QueueFullException(chosenQueue, backlog, opts.MaxPendingTasks);

            case "deadletter":
                await _queue.EnqueueAsync(tx, $"dlq:{chosenQueue}", ct);
                return $"dlq:{chosenQueue}";

            case "block":
                while (backlog >= opts.MaxPendingTasks)
                {
                    await Task.Delay(100, ct);
                    backlog = await _queue.GetBacklogSizeAsync(chosenQueue);
                }
                break;
        }
    }

    // ✅ Encolar
    await _queue.EnqueueAsync(tx, chosenQueue, ct);

    return chosenQueue; // 👈 devolvemos la cola usada
}
 
    public Task<Transaccion?> TryDequeueAsync(
        CancellationToken ct = default,
        string? queueName = null)
        => _queue.TryDequeueAsync(ct, queueName);
}
