using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services
{
    public class IdempotencyGuard : IIdempotencyGuard
    {
        private readonly ServicioAContext _db;
        public IdempotencyGuard(ServicioAContext db) => _db = db;

        public async Task<bool> IsProcessedAsync(Guid messageId, CancellationToken ct)
        {
            return await _db.ProcessedMessages.AnyAsync(p => p.MessageId == messageId, ct);
        }

        public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct)
        {
            _db.ProcessedMessages.Add(new ProcessedMessage { MessageId = messageId });
            await _db.SaveChangesAsync(ct);
        }
    }
}
