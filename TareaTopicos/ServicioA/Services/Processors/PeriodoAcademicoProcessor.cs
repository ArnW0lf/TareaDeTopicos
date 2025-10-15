using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class PeriodoAcademicoProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<PeriodoAcademicoProcessor> _logger;

        public PeriodoAcademicoProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<PeriodoAcademicoProcessor> logger)
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
                            var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<PeriodoAcademico>(tx.Payload, opts);
                            if (dto is null || string.IsNullOrWhiteSpace(dto.Gestion))
                            {
                                Skip(tx, "Payload inválido para POST/PeriodoAcademico");
                                return;
                            }

                            if (dto.FechaFin < dto.FechaInicio)
                            {
                                Skip(tx, "POST/PeriodoAcademico: FechaFin < FechaInicio");
                                return;
                            }

                            var existe = await _db.PeriodosAcademicos
                                .AsNoTracking()
                                .AnyAsync(p => p.Gestion == dto.Gestion, ct);

                            if (existe)
                            {
                                Skip(tx, $"Periodo con Gestión '{dto.Gestion}' ya existe");
                                return;
                            }

                            dto.Id = 0;
                            _db.PeriodosAcademicos.Add(dto);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "PUT":
                        {
                            var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<PeriodoAcademico>(tx.Payload, opts);
                            if (dto is null || string.IsNullOrWhiteSpace(dto.Gestion))
                            {
                                Skip(tx, "Payload inválido para PUT/PeriodoAcademico");
                                return;
                            }

                            if (dto.FechaFin < dto.FechaInicio)
                            {
                                Skip(tx, "PUT/PeriodoAcademico: FechaFin < FechaInicio");
                                return;
                            }

                            var existente = await _db.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Gestion == dto.Gestion, ct);
                            if (existente is null)
                            {
                                Skip(tx, $"Periodo con Gestión '{dto.Gestion}' no existe");
                                return;
                            }

                            existente.FechaInicio = dto.FechaInicio;
                            existente.FechaFin = dto.FechaFin;

                            await _db.SaveChangesAsync(ct);
                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "DELETE":
                        {
                            if (!TryGetGestion(tx.Payload ?? string.Empty, out var gestion) || string.IsNullOrWhiteSpace(gestion))
                            {
                                Skip(tx, "DELETE/PeriodoAcademico requiere Gestión válida");
                                return;
                            }

                            var entity = await _db.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Gestion == gestion, ct);
                            if (entity is null)
                            {
                                Skip(tx, $"Periodo con Gestión '{gestion}' no existe");
                                return;
                            }

                            try
                            {
                                _db.PeriodosAcademicos.Remove(entity);
                                await _db.SaveChangesAsync(ct);

                                await _guard.MarkProcessedAsync(tx.Id, ct);
                                tx.Estado = "COMPLETADO";
                                return;
                            }
                            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                                        .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogError(ex, "💥 Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                                Skip(tx, $"No se puede eliminar Periodo '{gestion}': tiene dependencias (FK).");
                                return;
                            }
                        }

                    default:
                        {
                            Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para PeriodoAcademico");
                            return;
                        }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en PeriodoAcademicoProcessor: {ex.Message}", ex);
            }
        }

        private static bool TryGetGestion(string payload, out string gestion)
        {
            gestion = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Gestion", out var gProp))
                {
                    if (gProp.ValueKind == JsonValueKind.String)
                    {
                        gestion = gProp.GetString() ?? string.Empty;
                        return !string.IsNullOrWhiteSpace(gestion);
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