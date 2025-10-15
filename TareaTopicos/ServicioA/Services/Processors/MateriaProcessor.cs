using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class MateriaProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<MateriaProcessor> _logger;

        public MateriaProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<MateriaProcessor> logger)
        {
            _db = db;
            _guard = guard;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            _logger.LogInformation("🔄 Procesando Tx {TxId} tipo {Tipo}", tx.Id, tx.TipoOperacion);

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
                        var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<MateriaRequestDto>(tx.Payload, opts);
                        if (dto is null)
                        {
                            Skip(tx, "Payload vacío para POST/Materia");
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(dto.Codigo))
                        {
                            Skip(tx, "POST/Materia requiere un Código");
                            return;
                        }

                        // Permitir NivelNumero en el payload si NivelId no vino/vale 0
                        if (dto.NivelId <= 0)
                        {
                            var nivelId = await ResolveNivelIdFromNumeroAsync(tx.Payload!, ct);
                            if (nivelId <= 0)
                            {
                                Skip(tx, "POST/Materia requiere NivelId válido o NivelNumero existente");
                                return;
                            }
                            dto.NivelId = nivelId;
                        }

                        var nivelExiste = await _db.Niveles.AsNoTracking().AnyAsync(n => n.Id == dto.NivelId, ct);
                        if (!nivelExiste)
                        {
                            Skip(tx, $"NivelId {dto.NivelId} no existe");
                            return;
                        }

                        var yaExiste = await _db.Materias.AsNoTracking().AnyAsync(m => m.Codigo == dto.Codigo, ct);
                        if (yaExiste)
                        {
                            Skip(tx, $"Ya existe una materia con Código {dto.Codigo}");
                            return;
                        }

                        var materia = new Materia
                        {
                            Codigo = dto.Codigo,
                            Nombre = dto.Nombre,
                            Creditos = dto.Creditos,
                            NivelId = dto.NivelId
                        };

                        _db.Materias.Add(materia);
                        _logger.LogInformation("💾 Guardando cambios para Tx {TxId}", tx.Id);
                        await _db.SaveChangesAsync(ct);

                        await _guard.MarkProcessedAsync(tx.Id, ct);
                        tx.Estado = "COMPLETADO";
                        return;
                    }

                    case "PUT":
                    {
                        var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<MateriaRequestDto>(tx.Payload, opts);
                        if (dto is null || string.IsNullOrWhiteSpace(dto.Codigo))
                        {
                            Skip(tx, "Payload vacío o sin Código para PUT/Materia");
                            return;
                        }

                        var existente = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == dto.Codigo, ct);
                        if (existente is null)
                        {
                            Skip(tx, $"Materia con Código {dto.Codigo} no existe");
                            return;
                        }

                        // Permitir NivelNumero si NivelId no vino/vale 0
                        if (dto.NivelId <= 0)
                        {
                            var nivelId = await ResolveNivelIdFromNumeroAsync(tx.Payload!, ct);
                            if (nivelId <= 0)
                            {
                                Skip(tx, "PUT/Materia requiere NivelId válido o NivelNumero existente");
                                return;
                            }
                            dto.NivelId = nivelId;
                        }

                        var nivelExiste = await _db.Niveles.AsNoTracking().AnyAsync(n => n.Id == dto.NivelId, ct);
                        if (!nivelExiste)
                        {
                            Skip(tx, $"NivelId {dto.NivelId} no existe");
                            return;
                        }

                        existente.Nombre = dto.Nombre;
                        existente.Creditos = dto.Creditos;
                        existente.NivelId = dto.NivelId;

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
                            Skip(tx, "DELETE/Materia requiere un Código válido");
                            return;
                        }

                        var entity = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
                        if (entity is null)
                        {
                            Skip(tx, $"Materia con Código {codigo} no existe");
                            return;
                        }

                        try
                        {
                            _db.Materias.Remove(entity);
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
                            Skip(tx, $"No se puede eliminar Materia {codigo}: tiene dependencias (FK).");
                            return;
                        }
                    }

                    default:
                    {
                        Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para Materia");
                        return;
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en MateriaProcessor: {ex.Message}", ex);
            }
        }

        // ==== Helpers ====

        // Si el payload trae "NivelNumero": <int>, lo resuelve a Nivel.Id. Si no, devuelve 0.
        private async Task<int> ResolveNivelIdFromNumeroAsync(string payload, CancellationToken ct)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return 0;

                if (root.TryGetProperty("NivelNumero", out var nivelNumProp) &&
                    nivelNumProp.ValueKind == JsonValueKind.Number &&
                    nivelNumProp.TryGetInt32(out var nivelNumero))
                {
                    var nivel = await _db.Niveles.AsNoTracking()
                        .FirstOrDefaultAsync(n => n.Numero == nivelNumero, ct);
                    return nivel?.Id ?? 0;
                }
            }
            catch { /* ignorar y devolver 0 */ }

            return 0;
        }

        private static bool TryGetCodigo(string payload, out string codigo)
        {
            codigo = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Codigo", out var codProp) &&
                    codProp.ValueKind == JsonValueKind.String)
                {
                    codigo = codProp.GetString() ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(codigo);
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
