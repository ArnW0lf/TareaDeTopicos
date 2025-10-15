using System.Text.Json;
using StackExchange.Redis;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Guarda el estado de cada transacción en Redis (clave por tx).
/// Clave: tx:{guid}
/// Campos: json completo de la transacción (para simplicidad inicial).
/// </summary>
public class RedisTransaccionStore : ITransaccionStore
{
    private readonly IConnectionMultiplexer _mux;
    private readonly string _txPrefix = "tx:";

    public RedisTransaccionStore(IConnectionMultiplexer mux)
    {
        _mux = mux;
    }

    private string Key(Guid id) => $"{_txPrefix}{id}";

    public async Task AddAsync(Transaccion tx, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var json = JsonSerializer.Serialize(tx);
        await db.StringSetAsync(Key(tx.Id), json);
    }

    public async Task<Transaccion?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var raw = await db.StringGetAsync(Key(id));
        if (!raw.HasValue) return null;

        try { return JsonSerializer.Deserialize<Transaccion>(raw!); }
        catch { return null; }
    }

    public async Task UpdateEstadoAsync(Guid id, string estado, string? error = null, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var tx = await GetAsync(id, ct);
        if (tx is null) return;

        tx.Estado = estado;
        if (!string.IsNullOrWhiteSpace(error))
        {
            // Podrías agregar una propiedad Error si la agregas al modelo.
            // Por ahora, dejamos comentario para que lo extiendas luego.
        }
        var json = JsonSerializer.Serialize(tx);
        await db.StringSetAsync(Key(id), json);
    }
        public Task MarkFinalizadoAsync(Guid id, DateTimeOffset at, CancellationToken ct = default)
        => Task.CompletedTask;

}
