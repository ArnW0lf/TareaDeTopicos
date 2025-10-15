// File: Services/WorkerService.cs
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services.Processors;

namespace TAREATOPICOS.ServicioA.Services;

 
public sealed class WorkerService
{
    private readonly string _queueName;
    private readonly QueueManager _queueManager;
    private readonly ITransaccionStore _store;
    private readonly IQueueProcessor _processor;
    private readonly DeadLetterService _dlq;
    private readonly RateLimiter _limiter;
    private readonly ILogger<WorkerService> _logger;
    private readonly QueueStateService _queueState;

     
    private readonly CallbackService _callback;

    private readonly int _defaultMaxRetries;
    private readonly int _baseBackoffMs;

    public WorkerService(
        string queueName,
        QueueManager queueManager,
        ITransaccionStore store,
        IQueueProcessor processor,
        DeadLetterService dlq,
        RateLimiter limiter,
        ILogger<WorkerService> logger,
        CallbackService callback, 
        QueueStateService queueState, 
        int defaultMaxRetries = 5,
        int baseBackoffMs = 300
    )
    {
        _queueName = queueName;
        _queueManager = queueManager;
        _store = store;
        _processor = processor;
        _dlq = dlq;
        _limiter = limiter;
        _logger = logger;
        _callback = callback; 
        _queueState = queueState; 
        _defaultMaxRetries = defaultMaxRetries;
        _baseBackoffMs = baseBackoffMs;
    }

    /// <summary>Ejecuta el loop hasta que se cancele.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("WorkerService iniciado para cola {Queue}", _queueName);

        while (!ct.IsCancellationRequested)
        {
            bool acquired = false;
            try
            {

if (await _queueState.IsPausedAsync(_queueName))
        {
            _logger.LogInformation("Cola {Queue} está pausada. Esperando...", _queueName);
            await Task.Delay(500, ct);
            continue;
        }


                await _limiter.AcquireAsync(_queueName, ct);
                acquired = true;

                var tx = await _queueManager.TryDequeueAsync(ct, _queueName);

                if (tx is null)
                {
                    _limiter.Release(_queueName);
                    acquired = false;
                    await Task.Delay(200, ct);
                    continue;
                }

                try
                {
                    await _store.UpdateEstadoAsync(tx.Id, "PROCESANDO", null, ct);

                    // Ejecución diferida
                    if (tx.NotBefore > DateTimeOffset.UtcNow)
                    {
                        var waitFor = tx.NotBefore - DateTimeOffset.UtcNow;
                        _logger.LogInformation("Tx {Tx} diferida hasta {At} (esperando {Wait}ms)", tx.Id, tx.NotBefore, (int)waitFor.TotalMilliseconds);

                        _limiter.Release(_queueName);
                        acquired = false;

                        await Task.Delay(waitFor, ct);

                        await _queueManager.EnqueueAsync(tx, _queueName, ct);
                        await _store.UpdateEstadoAsync(tx.Id, "EN_COLA", null, ct);
                        continue;
                    }

                    // Procesar negocio
                    await _processor.ProcessAsync(tx, ct);

            //   luego de ProcessAsync(tx), decide por estado
if (string.Equals(tx.Estado, "SKIP", StringComparison.OrdinalIgnoreCase))
{
    // → ACK sin reintento ni DLQ
    await _store.UpdateEstadoAsync(tx.Id, "SKIP", null, ct);
    await _store.MarkFinalizadoAsync(tx.Id, DateTimeOffset.UtcNow, ct);
    _logger.LogWarning("Tx {Tx} SKIP", tx.Id);

    // Callback SKIP (no debe mandar a DLQ si falla)
    try { await _callback.SendAsync(tx, "SKIP", ct); }
    catch (Exception cbEx) { _logger.LogWarning(cbEx, "Callback SKIP falló para {Tx}", tx.Id); }

    continue; // pasa al siguiente mensaje
}

//   TERMINAL OK (camino normal)
await _store.UpdateEstadoAsync(tx.Id, "COMPLETADO", null, ct);
await _store.MarkFinalizadoAsync(tx.Id, DateTimeOffset.UtcNow, ct);
_logger.LogInformation("Tx {Tx} COMPLETADO", tx.Id);

// Callback OK (recomendado NO mandar a DLQ si el webhook falla)
try { await _callback.SendAsync(tx, "OK", ct); }
catch (Exception cbEx) { _logger.LogWarning(cbEx, "Callback OK falló para {Tx}", tx.Id); }

 
                }
                catch (Exception ex)
                {
                    // Error: decidir reintento o DLQ
                    tx.Attempt++;
                    var maxRetries = tx.MaxRetries > 0 ? tx.MaxRetries : _defaultMaxRetries;

                    if (tx.Attempt <= maxRetries)
                    {
                        var delay = CalcBackoffMs(tx.Attempt, _baseBackoffMs);
                        _logger.LogWarning(ex, "Tx {Tx} fallo intento {Attempt}/{Max}. Reintentando en {Delay}ms", tx.Id, tx.Attempt, maxRetries, delay);

                        await _store.UpdateEstadoAsync(tx.Id, "EN_COLA", ex.Message, ct);

                        _limiter.Release(_queueName);
                        acquired = false;

                        await Task.Delay(delay, ct);
                        await _queueManager.EnqueueAsync(tx, _queueName, ct);
                        continue;
                    }

                    //   TERMINAL ERROR → DLQ
                    _logger.LogError(ex, "Tx {Tx} a DLQ tras {Attempts} intentos", tx.Id, tx.Attempt);
                    await _dlq.SendToDlqAsync(_queueName, tx, ex, ct);
                    await _store.UpdateEstadoAsync(tx.Id, "ERROR", ex.Message, ct);
                    await _store.MarkFinalizadoAsync(tx.Id, DateTimeOffset.UtcNow, ct); // <-- terminal
//   Callback ERROR (no rompe la tx si falla)
var okErr = await _callback.SendAsync(tx, "ERROR", ct);
if (!okErr)
{
    await _dlq.SendToDlqAsync(_queueName, tx, new Exception("Webhook failed"), ct);
}
                
                }
            }
            catch (OperationCanceledException)
            {
                break; // apagado ordenado
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en loop de WorkerService({Queue})", _queueName);
                await Task.Delay(500, ct);
            }
            finally
            {
                if (acquired)
                {
                    try { _limiter.Release(_queueName); }
                    catch (Exception ex) { _logger.LogError(ex, "Release falló en finally para {Queue}", _queueName); }
                }
            }
        }

        _logger.LogInformation("WorkerService detenido para cola {Queue}", _queueName);
    }

    private static int CalcBackoffMs(int attempt, int baseMs)
    {
        var exp = baseMs * (int)Math.Pow(2, Math.Max(0, attempt - 1));
        var jitter = Random.Shared.Next(0, baseMs);
        return Math.Min(exp + jitter, 30_000); // cap 30s
    }
}
