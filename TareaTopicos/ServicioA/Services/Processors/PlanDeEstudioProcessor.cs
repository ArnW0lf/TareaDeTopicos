using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class PlanDeEstudioProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<PlanDeEstudioProcessor> _logger;

        public PlanDeEstudioProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<PlanDeEstudioProcessor> logger)
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
                _logger.LogInformation(" Tx {TxId} ya fue procesada (idempotente)", tx.Id);
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
                            var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<PlanDeEstudio>(tx.Payload, opts);
                            if (dto is null || string.IsNullOrWhiteSpace(dto.Codigo))
                            {
                                Skip(tx, "Payload inválido para POST/PlanDeEstudio");
                                return;
                            }

                            var existe = await _db.PlanesEstudio
                                .AsNoTracking()
                                .AnyAsync(p => p.Codigo == dto.Codigo, ct);

                            if (existe)
                            {
                                Skip(tx, $"Ya existe un plan con Código '{dto.Codigo}'");
                                return;
                            }

                            dto.Id = 0;
                            dto.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado;

                            _db.PlanesEstudio.Add(dto);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "PUT":
                        {
                            var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<PlanDeEstudio>(tx.Payload, opts);
                            if (dto is null || string.IsNullOrWhiteSpace(dto.Codigo))
                            {
                                Skip(tx, "Payload inválido para PUT/PlanDeEstudio");
                                return;
                            }

                            var existente = await _db.PlanesEstudio.FirstOrDefaultAsync(p => p.Codigo == dto.Codigo, ct);
                            if (existente is null)
                            {
                                Skip(tx, $"No existe plan con Código '{dto.Codigo}'");
                                return;
                            }

                            existente.Nombre = dto.Nombre;
                            existente.Fecha = dto.Fecha;
                            existente.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? existente.Estado : dto.Estado;
                            existente.CarreraId = dto.CarreraId;

                            await _db.SaveChangesAsync(ct);
                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "DELETE":
                        {
                            if (!TryGetCodigo(tx.Payload ?? string.Empty, out var codigo) || string.IsNullOrWhiteSpace(codigo))
                            {
                                Skip(tx, "DELETE/PlanDeEstudio requiere Código válido");
                                return;
                            }

                            var entity = await _db.PlanesEstudio.FirstOrDefaultAsync(p => p.Codigo == codigo, ct);
                            if (entity is null)
                            {
                                Skip(tx, $"No existe plan con Código '{codigo}'");
                                return;
                            }

                            try
                            {
                                _db.PlanesEstudio.Remove(entity);
                                await _db.SaveChangesAsync(ct);

                                await _guard.MarkProcessedAsync(tx.Id, ct);
                                tx.Estado = "COMPLETADO";
                                return;
                            }
                            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                                        .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogError(ex, " Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                                Skip(tx, $"No se puede eliminar Plan '{codigo}': tiene dependencias (FK).");
                                return;
                            }
                        }

                    default:
                        {
                            Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para PlanDeEstudio");
                            return;
                        }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en PlanDeEstudioProcessor: {ex.Message}", ex);
            }
        }

        private static bool TryGetCodigo(string payload, out string codigo)
        {
            codigo = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Codigo", out var cProp))
                {
                    if (cProp.ValueKind == JsonValueKind.String)
                    {
                        codigo = cProp.GetString() ?? string.Empty;
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