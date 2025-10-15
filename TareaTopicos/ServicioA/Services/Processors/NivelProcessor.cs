using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class NivelProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<NivelProcessor> _logger;

        public NivelProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<NivelProcessor> logger)
        {
            _db = db;
            _guard = guard;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            _logger.LogInformation(" Procesando Tx {TxId} tipo {Tipo}", tx.Id, tx.TipoOperacion);

            // 1) Idempotencia
            if (await _guard.IsProcessedAsync(tx.Id, ct))
            {
                _logger.LogInformation("🟡 Tx {TxId} ya fue procesada (idempotente)", tx.Id);
                tx.Estado = "COMPLETADO";
                return;
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                switch (tx.TipoOperacion)
                {
                    case "POST":
                        {
                            var n = tx.Payload is null ? null : JsonSerializer.Deserialize<Nivel>(tx.Payload, opts);
                            if (n is null)
                            {
                                Skip(tx, "Payload vacío para POST/Nivel");
                                return;
                            }

                            if (n.Numero == 0)
                            {
                                Skip(tx, "POST/Nivel requiere Numero (>0)");
                                return;
                            }

                            var existe = await _db.Niveles
                                                  .AsNoTracking()
                                                  .AnyAsync(x => x.Numero == n.Numero, ct);
                            if (existe)
                            {
                                Skip(tx, $"Nivel con Numero {n.Numero} ya existe");
                                return;
                            }

                            n.Id = 0;
                            _db.Niveles.Add(n);
                            _logger.LogInformation("💾 Guardando cambios para Tx {TxId}", tx.Id);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "PUT":
                        {
                            var n = tx.Payload is null ? null : JsonSerializer.Deserialize<Nivel>(tx.Payload, opts);
                            if (n is null)
                            {
                                Skip(tx, "Payload vacío para PUT/Nivel");
                                return;
                            }

                            if (n.Numero == 0)
                            {
                                Skip(tx, "PUT/Nivel requiere Numero (>0)");
                                return;
                            }

                            var existente = await _db.Niveles.FirstOrDefaultAsync(x => x.Numero == n.Numero, ct);
                            if (existente is null)
                            {
                                Skip(tx, $"Nivel {n.Numero} no existe");
                                return;
                            }

                            existente.Nombre = n.Nombre;
                            _logger.LogInformation("💾 Guardando cambios para Tx {TxId}", tx.Id);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "DELETE":
                        {
                            if (!TryGetNumero(tx.Payload ?? string.Empty, out var numero) || numero <= 0)
                            {
                                Skip(tx, "DELETE/Nivel requiere Numero (>0)");
                                return;
                            }

                            var entity = await _db.Niveles.FirstOrDefaultAsync(x => x.Numero == numero, ct);
                            if (entity is null)
                            {
                                Skip(tx, $"Nivel {numero} no existe");
                                return;
                            }

                            try
                            {
                                _db.Niveles.Remove(entity);
                                _logger.LogInformation("💾 Guardando cambios para Tx {TxId}", tx.Id);
                                await _db.SaveChangesAsync(ct);

                                await _guard.MarkProcessedAsync(tx.Id, ct);
                                tx.Estado = "COMPLETADO";
                                return;
                            }
                            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                                        .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogError(ex, "💥 Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                                Skip(tx, $"No se puede eliminar Nivel {numero}: tiene dependencias (FK).");
                                return;
                            }
                        }

                    default:
                        {
                            Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para Nivel");
                            return;
                        }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en NivelProcessor: {ex.Message}", ex);
            }
        }

        private static bool TryGetNumero(string payload, out int numero)
        {
            numero = 0;

            if (int.TryParse(payload.Trim('"'), out numero)) return true;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Numero", out var numProp))
                {
                    if (numProp.ValueKind == JsonValueKind.Number && numProp.TryGetInt32(out numero)) return true;
                    if (numProp.ValueKind == JsonValueKind.String && int.TryParse(numProp.GetString(), out numero)) return true;
                }
            }
            catch { }

            return false;
        }

        private void Skip(Transaccion tx, string motivo)
        {
            tx.Estado = "SKIP";
            _logger.LogWarning("⚠️ Tx {TxId} marcada como SKIP: {Motivo}", tx.Id, motivo);
        }

        private static bool IsTransient(DbUpdateException ex)
        {
            var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
            return msg.Contains("timeout") ||
                   msg.Contains("deadlock") ||
                   msg.Contains("could not open connection") ||
                   msg.Contains("the connection is broken") ||
                   msg.Contains("temporarily") ||
                   msg.Contains("try again");
        }
    }
}
