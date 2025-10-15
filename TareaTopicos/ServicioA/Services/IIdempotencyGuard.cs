using System;
using System.Threading;
using System.Threading.Tasks;

namespace TAREATOPICOS.ServicioA.Services
{
    public interface IIdempotencyGuard
    {
        Task<bool> IsProcessedAsync(Guid messageId, CancellationToken ct);
        Task MarkProcessedAsync(Guid messageId, CancellationToken ct);
    }
}
