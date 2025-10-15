using Microsoft.Extensions.Logging;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Administra N hilos (tareas) WorkerService para UNA cola.
/// Permite ajustar concurrencia en caliente (incremental/decremental).
/// </summary>
public sealed class WorkerPool : IAsyncDisposable
{
    private readonly string _queueName;
    private readonly Func<string, WorkerService> _workerFactory;
    private readonly ILogger<WorkerPool> _logger;

    private readonly List<(Task task, CancellationTokenSource cts)> _workers = new();
    private readonly object _lock = new();

    public int Concurrency => _workers.Count;

    public WorkerPool(string queueName, Func<string, WorkerService> workerFactory, ILogger<WorkerPool> logger)
    {
        _queueName = queueName;
        _workerFactory = workerFactory;
        _logger = logger;
    }

    public void SetConcurrency(int desired)
    {
        lock (_lock)
        {
            var current = _workers.Count;
            if (desired == current) return;

            if (desired > current)
            {
                var toAdd = desired - current;
                for (int i = 0; i < toAdd; i++)
                {
                    var cts = new CancellationTokenSource();
                    var worker = _workerFactory(_queueName);
                    var task = worker.RunAsync(cts.Token);
                    _workers.Add((task, cts));
                }
                _logger.LogInformation("Pool {Queue} ↑ concurrencia {Old}→{New}", _queueName, current, desired);
            }
            else
            {
                var toStop = current - desired;
                for (int i = 0; i < toStop; i++)
                {
                    var last = _workers[^1];
                    _workers.RemoveAt(_workers.Count - 1);
                    last.cts.Cancel();
                }
                _logger.LogInformation("Pool {Queue} ↓ concurrencia {Old}→{New}", _queueName, current, desired);
            }
        }
    }

    public async Task StopAsync()
    {
        List<(Task task, CancellationTokenSource cts)> snapshot;
        lock (_lock) snapshot = _workers.ToList();

        foreach (var (_, cts) in snapshot) cts.Cancel();
        await Task.WhenAll(snapshot.Select(w => w.task).ToArray());

        lock (_lock) _workers.Clear();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
