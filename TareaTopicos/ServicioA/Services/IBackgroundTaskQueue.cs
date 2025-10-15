using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Contrato mínimo de la cola: encolar y sacar una transacción.
/// (Más adelante agregamos ACK/NACK/Delay si lo necesitas)
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>Encola la transacción en la cola indicada (default si null).</summary>
    Task EnqueueAsync(Transaccion transaccion, string? queueName = null, CancellationToken ct = default);

    /// <summary>
    /// Intenta sacar una transacción de la cola (bloqueo corto).
    /// Devuelve null si no hay nada.
    /// </summary>
    Task<Transaccion?> TryDequeueAsync(CancellationToken ct = default, string? queueName = null);
        Task<long> GetBacklogSizeAsync(string queueName);
}
