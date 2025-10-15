// File: Services/DeadLetterService.cs
using StackExchange.Redis;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services
{
    public class DeadLetterService
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly string _prefix;

        public DeadLetterService(IConnectionMultiplexer mux, IConfiguration cfg)
        {
            _mux = mux;
            _prefix = cfg.GetValue<string>("RedisQueue:KeyPrefix") ?? "q:";
        }

        public async Task SendToDlqAsync(string queue, Transaccion tx, Exception? ex, CancellationToken ct)
        {
            var db = _mux.GetDatabase();

            var raw = JsonSerializer.Serialize(new
            {
                id = tx.Id,
                entidad = tx.Entidad,
                tipo = tx.TipoOperacion,
                payload = tx.Payload,
                attempt = tx.Attempt,
                error = ex?.Message,
                at = DateTimeOffset.UtcNow
            });

            await db.ListRightPushAsync($"{_prefix}{queue}:dlq", raw);
        }

        /// <summary>
        /// Saca hasta 'max' items de DLQ y los reinyecta a prioridad baja (p=2) de la cola principal.
        /// </summary>
        public async Task<int> ReplayAsync(string queue, int max, CancellationToken ct)
        {
            var db = _mux.GetDatabase();
            int moved = 0;

            for (; moved < max; moved++)
            {
                RedisValue raw = await db.ListLeftPopAsync($"{_prefix}{queue}:dlq");
                if (!raw.HasValue) break;

                // Si necesitás inspeccionar algo:
                // using var doc = JsonDocument.Parse(raw.ToString());

                // Reinyectamos el JSON crudo a la lista de prioridad baja (2),
                // o ajusta a tu política (0 alta / 1 normal / 2 baja)
                await db.ListRightPushAsync($"{_prefix}{queue}:p:2", raw.ToString());
            }

            return moved;
        }
    }
}
