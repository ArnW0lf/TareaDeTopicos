using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Persistencia de estado de transacciones (EN_COLA, PROCESANDO, COMPLETADO, ERROR).
/// </summary>
public interface ITransaccionStore
{
    Task AddAsync(Transaccion tx, CancellationToken ct = default);
    Task<Transaccion?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateEstadoAsync(Guid id, string estado, string? error = null, CancellationToken ct = default);
    Task MarkFinalizadoAsync(Guid id, DateTimeOffset at, CancellationToken ct = default); 

}
