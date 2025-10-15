using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class EstudianteProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<EstudianteProcessor> _logger;

        public EstudianteProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<EstudianteProcessor> logger)
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
                            var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<Estudiante>(tx.Payload, opts);
                            if (dto is null || string.IsNullOrWhiteSpace(dto.Registro))
                            {
                                Skip(tx, "Payload inválido para POST/Estudiante");
                                return;
                            }

                            var existe = await _db.Estudiantes
                                .AsNoTracking()
                                .AnyAsync(e => e.Registro == dto.Registro, ct);

                            if (existe)
                            {
                                Skip(tx, $"Estudiante con Registro {dto.Registro} ya existe");
                                return;
                            }

                            dto.Id = 0;
                            dto.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado;
                            dto.PasswordHash = string.IsNullOrWhiteSpace(dto.PasswordHash)
                                ? BCrypt.Net.BCrypt.HashPassword("123456") // fallback defensivo
                                : dto.PasswordHash;

                            _db.Estudiantes.Add(dto);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "PUT":
                        {
                            var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<Estudiante>(tx.Payload, opts);
                            if (dto is null || string.IsNullOrWhiteSpace(dto.Registro))
                            {
                                Skip(tx, "Payload inválido para PUT/Estudiante");
                                return;
                            }

                            var existente = await _db.Estudiantes.FirstOrDefaultAsync(e => e.Registro == dto.Registro, ct);
                            if (existente is null)
                            {
                                Skip(tx, $"Estudiante con Registro {dto.Registro} no existe");
                                return;
                            }

                            existente.Ci = dto.Ci;
                            existente.Nombre = dto.Nombre;
                            existente.Email = dto.Email;
                            existente.Telefono = dto.Telefono;
                            existente.Direccion = dto.Direccion;
                            existente.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? existente.Estado : dto.Estado;
                            existente.CarreraId = dto.CarreraId;

                            if (!string.IsNullOrWhiteSpace(dto.PasswordHash))
                            {
                                existente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash);
                            }

                            await _db.SaveChangesAsync(ct);
                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "DELETE":
                        {
                            if (!TryGetRegistro(tx.Payload ?? string.Empty, out var registro) || string.IsNullOrWhiteSpace(registro))
                            {
                                Skip(tx, "DELETE/Estudiante requiere Registro válido");
                                return;
                            }

                            var entity = await _db.Estudiantes.FirstOrDefaultAsync(e => e.Registro == registro, ct);
                            if (entity is null)
                            {
                                Skip(tx, $"Estudiante con Registro {registro} no existe");
                                return;
                            }

                            try
                            {
                                _db.Estudiantes.Remove(entity);
                                await _db.SaveChangesAsync(ct);

                                await _guard.MarkProcessedAsync(tx.Id, ct);
                                tx.Estado = "COMPLETADO";
                                return;
                            }
                            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                                        .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogError(ex, "💥 Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                                Skip(tx, $"No se puede eliminar Estudiante {registro}: tiene dependencias (FK).");
                                return;
                            }
                        }

                    default:
                        {
                            Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para Estudiante");
                            return;
                        }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en EstudianteProcessor: {ex.Message}", ex);
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