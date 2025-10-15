using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class DocenteProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<DocenteProcessor> _logger;

        public DocenteProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<DocenteProcessor> logger)
        {
            _db = db;
            _guard = guard;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            _logger.LogInformation("🔄 Procesando Tx {TxId} tipo {Tipo}", tx.Id, tx.TipoOperacion);

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
                            var docente = tx.Payload is null ? null : JsonSerializer.Deserialize<Docente>(tx.Payload, opts);
                            if (docente is null || string.IsNullOrWhiteSpace(docente.Registro))
                            {
                                Skip(tx, "Payload inválido para POST/Docente");
                                return;
                            }

                            var existe = await _db.Docentes
                                .AsNoTracking()
                                .AnyAsync(d => d.Registro == docente.Registro, ct);

                            if (existe)
                            {
                                Skip(tx, $"Docente con Registro {docente.Registro} ya existe");
                                return;
                            }

                            docente.Id = 0;
                            docente.Estado = string.IsNullOrWhiteSpace(docente.Estado) ? "ACTIVO" : docente.Estado;

                            _db.Docentes.Add(docente);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "PUT":
                        {
                            var docente = tx.Payload is null ? null : JsonSerializer.Deserialize<Docente>(tx.Payload, opts);
                            if (docente is null || string.IsNullOrWhiteSpace(docente.Registro))
                            {
                                Skip(tx, "Payload inválido para PUT/Docente");
                                return;
                            }

                            var existente = await _db.Docentes.FirstOrDefaultAsync(d => d.Registro == docente.Registro, ct);
                            if (existente is null)
                            {
                                Skip(tx, $"Docente con Registro {docente.Registro} no existe");
                                return;
                            }

                            existente.Ci = docente.Ci;
                            existente.Nombre = docente.Nombre;
                            existente.Telefono = docente.Telefono;
                            existente.Estado = string.IsNullOrWhiteSpace(docente.Estado) ? existente.Estado : docente.Estado;

                            await _db.SaveChangesAsync(ct);
                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "DELETE":
                        {
                            if (!TryGetRegistro(tx.Payload ?? string.Empty, out var registro) || string.IsNullOrWhiteSpace(registro))
                            {
                                Skip(tx, "DELETE/Docente requiere Registro válido");
                                return;
                            }

                            var entity = await _db.Docentes.FirstOrDefaultAsync(d => d.Registro == registro, ct);
                            if (entity is null)
                            {
                                Skip(tx, $"Docente con Registro {registro} no existe");
                                return;
                            }

                            try
                            {
                                _db.Docentes.Remove(entity);
                                await _db.SaveChangesAsync(ct);

                                await _guard.MarkProcessedAsync(tx.Id, ct);
                                tx.Estado = "COMPLETADO";
                                return;
                            }
                            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                                        .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogError(ex, "💥 Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                                Skip(tx, $"No se puede eliminar Docente {registro}: tiene dependencias (FK).");
                                return;
                            }
                        }

                    default:
                        {
                            Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para Docente");
                            return;
                        }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en DocenteProcessor: {ex.Message}", ex);
            }
        }

        private static bool TryGetRegistro(string payload, out string registro)
        {
            registro = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Registro", out var regProp))
                {
                    if (regProp.ValueKind == JsonValueKind.String)
                    {
                        registro = regProp.GetString() ?? string.Empty;
                        return !string.IsNullOrWhiteSpace(registro);
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