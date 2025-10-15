// File: Services/RedisScaleBackplane.cs
using System.Text.Json;
using StackExchange.Redis;

namespace TAREATOPICOS.ServicioA.Services
{
    // Tipo de evento que circula por el backplane (multi-instancia)
    public enum BackplaneEventType
    {
        ConfigChanged = 1
    }

    // Mensaje que viaja en Pub/Sub
    public sealed record BackplaneMessage(BackplaneEventType Type, string Payload);

    /// <summary>
    /// Pub/Sub en Redis para coordinar varias instancias (cambios en caliente, etc.)
    /// </summary>
    public class RedisScaleBackplane
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly string _channel;

        public RedisScaleBackplane(IConnectionMultiplexer mux)
        {
            _mux = mux;
            _channel = "servicioa:backplane";
        }

        /// <summary>
        /// Suscribirse a mensajes del backplane. Llama al handler con BackplaneMessage.
        /// </summary>
        public async Task SubscribeAsync(Func<BackplaneMessage, Task> handler, CancellationToken ct = default)
        {
            var sub = _mux.GetSubscriber();
            await sub.SubscribeAsync(RedisChannel.Literal(_channel), async (ch, msg) =>
            {
                try
                {
                    // msg viene como RedisValue -> string JSON
                    var bm = JsonSerializer.Deserialize<BackplaneMessage>(msg.ToString());
                    if (bm is not null)
                        await handler(bm);
                }
                catch
                {
                    // swallow: no queremos romper el bus por un mensaje malo
                }
            });
        }

        /// <summary>
        /// Broadcast genérico. (Firma esperada por tu ConfigWatcher)
        /// </summary>
        public Task BroadcastConfigChanged(object payload, CancellationToken ct = default)
        {
            // serializamos payload a string JSON y lo metemos en BackplaneMessage
            var json = JsonSerializer.Serialize(payload);
            var bm = new BackplaneMessage(BackplaneEventType.ConfigChanged, json);
            return PublishAsync(bm, ct);
        }

        /// <summary>
        /// Broadcast tipado (útil si quieres llamar explícitamente con tu tipo de opciones).
        /// </summary>
        public Task BroadcastConfigChangedAsync<T>(T payload, CancellationToken ct = default)
            => BroadcastConfigChanged(payload!, ct);

        private async Task PublishAsync(BackplaneMessage message, CancellationToken ct)
        {
            var sub = _mux.GetSubscriber();
            var raw = JsonSerializer.Serialize(message);
            await sub.PublishAsync(RedisChannel.Literal(_channel), raw);
        }
    }
}
