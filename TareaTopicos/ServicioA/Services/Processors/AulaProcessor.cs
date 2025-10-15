using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class AulaProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<AulaProcessor> _logger;

        public AulaProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<AulaProcessor> logger)
        {
            _db = db;
            _guard = guard;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            _logger.LogInformation("🔄 Procesando Tx {TxId} tipo {Tipo}", tx.Id, tx.TipoOperacion);

            // Idempotencia
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
                            var aula = tx.Payload is null ? null : JsonSerializer.Deserialize<Aula>(tx.Payload, opts);
                            if (aula is null)
                            {
                                Skip(tx, "Payload vacío para POST/Aula");
                                return;
                            }

                            if (string.IsNullOrWhiteSpace(aula.Codigo))
                            {
                                Skip(tx, "POST/Aula requiere Codigo");
                                return;
                            }

                            var existe = await _db.Aulas.AsNoTracking().AnyAsync(x => x.Codigo == aula.Codigo, ct);
                            if (existe)
                            {
                                Skip(tx, $"Aula con Codigo {aula.Codigo} ya existe");
                                return;
                            }

                            aula.Id = 0;
                            _db.Aulas.Add(aula);
                            _logger.LogInformation("💾 Guardando cambios para Tx {TxId}", tx.Id);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "PUT":
                        {
                            var aula = tx.Payload is null ? null : JsonSerializer.Deserialize<Aula>(tx.Payload, opts);
                            if (aula is null || string.IsNullOrWhiteSpace(aula.Codigo))
                            {
                                Skip(tx, "Payload inválido para PUT/Aula");
                                return;
                            }

                            var existente = await _db.Aulas.FirstOrDefaultAsync(x => x.Codigo == aula.Codigo, ct);
                            if (existente is null)
                            {
                                Skip(tx, $"Aula con Codigo {aula.Codigo} no existe");
                                return;
                            }

                            existente.Capacidad = aula.Capacidad;
                            existente.Ubicacion = aula.Ubicacion;
                            _logger.LogInformation("💾 Guardando cambios para Tx {TxId}", tx.Id);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "DELETE":
                        {
                            if (!TryGetCodigo(tx.Payload ?? string.Empty, out var codigo) || string.IsNullOrWhiteSpace(codigo))
                            {
                                Skip(tx, "DELETE/Aula requiere Codigo válido");
                                return;
                            }

                            var entity = await _db.Aulas.FirstOrDefaultAsync(x => x.Codigo == codigo, ct);
                            if (entity is null)
                            {
                                Skip(tx, $"Aula con Codigo {codigo} no existe");
                                return;
                            }

                            try
                            {
                                _db.Aulas.Remove(entity);
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
                                Skip(tx, $"No se puede eliminar Aula {codigo}: tiene dependencias (FK).");
                                return;
                            }
                        }

                    default:
                        {
                            Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para Aula");
                            return;
                        }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en AulaProcessor: {ex.Message}", ex);
            }
        }

        private static bool TryGetCodigo(string payload, out string codigo)
        {
            codigo = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Codigo", out var codProp))
                {
                    if (codProp.ValueKind == JsonValueKind.String)
                    {
                        codigo = codProp.GetString() ?? string.Empty;
                        return !string.IsNullOrWhiteSpace(codigo);
                    }
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
