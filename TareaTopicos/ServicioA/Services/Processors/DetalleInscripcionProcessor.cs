// Services/Processors/DetalleInscripcionProcessor.cs
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    /// <summary>
    /// Processor para Entidad = "DetalleInscripcion"
    /// Soporta TipoOperacion: POST, PUT, DELETE
    /// Trabaja con PeriodoId (no "Gestión") y NO lanza 404: si no existe, solo registra y marca procesado.
    /// </summary>
    public sealed class DetalleInscripcionProcessor : IProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<DetalleInscripcionProcessor> _log;

        public DetalleInscripcionProcessor(
            ServicioAContext db,
            IIdempotencyGuard guard,
            ILogger<DetalleInscripcionProcessor> log)
        {
            _db = db;
            _guard = guard;
            _log = log;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            // Idempotencia por Guid de la transacción
            if (await _guard.IsProcessedAsync(tx.Id, ct))
            {
                _log.LogInformation("Tx {TxId} ya estaba procesada (idempotente).", tx.Id);
                return;
            }

            try
            {
                switch (tx.TipoOperacion?.ToUpperInvariant())
                {
                    case "POST":
                        await HandlePostAsync(tx, ct);
                        break;

                    case "PUT":
                        await HandlePutAsync(tx, ct);
                        break;

                    case "DELETE":
                        await HandleDeleteAsync(tx, ct);
                        break;

                    default:
                        _log.LogWarning("TipoOperacion no soportado para DetalleInscripcion: {Op}", tx.TipoOperacion);
                        await MarkSkipAsync(tx, "TipoOperacion no soportado", ct);
                        return;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // No queremos reventar el worker; logueamos y marcamos procesado para no ciclar.
                _log.LogError(ex, "Error procesando Tx {TxId}: {Msg}", tx.Id, ex.Message);
                await MarkSkipAsync(tx, $"ERROR: {ex.Message}", ct);
                return;
            }

            await _guard.MarkProcessedAsync(tx.Id, ct);
        }

        // =======================
        //        POST
        // =======================
        // Payload esperado:
        // {
        //   "Registro": "12345",
        //   "PeriodoId": 7,
        //   "MateriaCodigo": "INF-101",
        //   "Grupo": "A",
        //   "AutoCrearInscripcion": true
        // }
        private async Task HandlePostAsync(Transaccion tx, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(tx.Payload))
            {
                await MarkSkipAsync(tx, "Payload vacío (POST DetalleInscripcion)", ct);
                return;
            }

            using var doc = JsonDocument.Parse(tx.Payload);
            var root = doc.RootElement;

            var registro      = N(GetStr(root, "Registro"));
            var periodoId     = GetInt(root, "PeriodoId");
            var materiaCodigo = N(GetStr(root, "MateriaCodigo"));
            var grupo         = N(GetStr(root, "Grupo"));
            var autoCrearInsc = GetBool(root, "AutoCrearInscripcion", false);

            if (string.IsNullOrEmpty(registro) || periodoId <= 0 ||
                string.IsNullOrEmpty(materiaCodigo) || string.IsNullOrEmpty(grupo))
            {
                await MarkSkipAsync(tx, "Faltan campos requeridos en POST (Registro, PeriodoId, MateriaCodigo, Grupo)", ct);
                return;
            }

            // 1) Resolver o crear Inscripción (Estudiante + PeriodoId)
            var insc = await ResolveInscripcionAsync(registro, periodoId, autoCrearInsc, ct);
            if (insc is null)
            {
                await MarkSkipAsync(tx, "No existe Inscripcion (y AutoCrearInscripcion=false)", ct);
                return;
            }

            // 2) Resolver GrupoMateria por MateriaCodigo + Grupo + PeriodoId
            var gm = await ResolveGrupoMateriaAsync(materiaCodigo, grupo, periodoId, ct);
            if (gm is null)
            {
                await MarkSkipAsync(tx, "GrupoMateria no encontrado (MateriaCodigo/Grupo/PeriodoId)", ct);
                return;
            }

            // 3) Idempotencia de detalle (índice único InscripcionId + GrupoMateriaId)
            var existe = await _db.DetallesInscripciones
                .AsNoTracking()
                .AnyAsync(d => d.InscripcionId == insc.Id && d.GrupoMateriaId == gm.Id, ct);
            if (existe)
            {
                await MarkSkipAsync(tx, "Detalle ya existe (idempotente)", ct);
                return;
            }

            // 4) Crear detalle (Codigo requerido por tu modelo)
            var det = new DetalleInscripcion
            {
                Codigo = $"{insc.Id}-{gm.Id}",   // puedes cambiar por un formato de negocio
                Estado = "INSCRITO",
                InscripcionId = insc.Id,
                GrupoMateriaId = gm.Id
            };

            _db.DetallesInscripciones.Add(det);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("POST ok: Detalle creado (Tx {TxId})", tx.Id);
        }

        // =======================
        //        PUT
        // =======================
        // Payload:
        // {
        //   "ClaveActual": { "Registro": "...", "PeriodoId": 7, "MateriaCodigo": "INF-101", "Grupo": "A" },
        //   "Update": { "NuevoGrupo": "B", "NuevoEstado": "RETIRADO" }
        // }
        private async Task HandlePutAsync(Transaccion tx, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(tx.Payload))
            {
                await MarkSkipAsync(tx, "Payload vacío (PUT DetalleInscripcion)", ct);
                return;
            }

            using var doc = JsonDocument.Parse(tx.Payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ClaveActual", out var clave)
                || !root.TryGetProperty("Update", out var update))
            {
                await MarkSkipAsync(tx, "PUT requiere ClaveActual y Update", ct);
                return;
            }

            var registro      = N(GetStr(clave, "Registro"));
            var periodoId     = GetInt(clave, "PeriodoId");
            var materiaCodigo = N(GetStr(clave, "MateriaCodigo"));
            var grupoActual   = N(GetStr(clave, "Grupo"));

            var nuevoGrupo    = GetStrOrNull(update, "NuevoGrupo") is string ng ? N(ng) : null;
            var nuevoEstado   = GetStrOrNull(update, "NuevoEstado") is string ne ? N(ne) : null;

            if (string.IsNullOrEmpty(registro) || periodoId <= 0 ||
                string.IsNullOrEmpty(materiaCodigo) || string.IsNullOrEmpty(grupoActual))
            {
                await MarkSkipAsync(tx, "PUT/ClaveActual incompleta", ct);
                return;
            }

            if (nuevoGrupo is null && nuevoEstado is null)
            {
                await MarkSkipAsync(tx, "PUT/Update requiere NuevoGrupo o NuevoEstado", ct);
                return;
            }

            // Resolver Inscripcion y detalle actual
            var insc = await ResolveInscripcionAsync(registro, periodoId, autoCreate: false, ct);
            if (insc is null)
            {
                await MarkSkipAsync(tx, "Inscripcion no existe", ct);
                return;
            }

            var gmActual = await ResolveGrupoMateriaAsync(materiaCodigo, grupoActual, periodoId, ct);
            if (gmActual is null)
            {
                await MarkSkipAsync(tx, "GrupoMateria actual no existe", ct);
                return;
            }

            var det = await _db.DetallesInscripciones
                .FirstOrDefaultAsync(d => d.InscripcionId == insc.Id && d.GrupoMateriaId == gmActual.Id, ct);

            if (det is null)
            {
                await MarkSkipAsync(tx, "Detalle a actualizar no existe", ct);
                return;
            }

            var huboCambio = false;

            // 1) Cambio de grupo
            if (!string.IsNullOrWhiteSpace(nuevoGrupo))
            {
                var gmNuevo = await ResolveGrupoMateriaAsync(materiaCodigo, nuevoGrupo!, periodoId, ct);
                if (gmNuevo is null)
                {
                    await MarkSkipAsync(tx, "GrupoMateria destino no existe", ct);
                    return;
                }

                // No duplicar otro detalle
                var dup = await _db.DetallesInscripciones
                    .AsNoTracking()
                    .AnyAsync(d => d.InscripcionId == insc.Id && d.GrupoMateriaId == gmNuevo.Id, ct);
                if (dup)
                {
                    await MarkSkipAsync(tx, "Cambio de grupo provocaría duplicado", ct);
                    return;
                }

                det.GrupoMateriaId = gmNuevo.Id;
                // Recalcular/actualizar el Código si lo basas en Ids
                det.Codigo = $"{insc.Id}-{gmNuevo.Id}";
                huboCambio = true;
            }

            // 2) Cambio de estado
            if (!string.IsNullOrWhiteSpace(nuevoEstado) &&
                !string.Equals(det.Estado, nuevoEstado, StringComparison.OrdinalIgnoreCase))
            {
                det.Estado = nuevoEstado!;
                huboCambio = true;
            }

            if (!huboCambio)
            {
                await MarkSkipAsync(tx, "PUT sin cambios efectivos", ct);
                return;
            }

            await _db.SaveChangesAsync(ct);
            _log.LogInformation("PUT ok: Detalle actualizado (Tx {TxId})", tx.Id);
        }

        // =======================
        //       DELETE
        // =======================
        // Payload:
        // { "Registro":"...", "PeriodoId":7, "MateriaCodigo":"INF-101", "Grupo":"A" }
        private async Task HandleDeleteAsync(Transaccion tx, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(tx.Payload))
            {
                await MarkSkipAsync(tx, "Payload vacío (DELETE DetalleInscripcion)", ct);
                return;
            }

            using var doc = JsonDocument.Parse(tx.Payload);
            var root = doc.RootElement;

            var registro      = N(GetStr(root, "Registro"));
            var periodoId     = GetInt(root, "PeriodoId");
            var materiaCodigo = N(GetStr(root, "MateriaCodigo"));
            var grupo         = N(GetStr(root, "Grupo"));

            if (string.IsNullOrEmpty(registro) || periodoId <= 0 ||
                string.IsNullOrEmpty(materiaCodigo) || string.IsNullOrEmpty(grupo))
            {
                await MarkSkipAsync(tx, "DELETE requiere Registro, PeriodoId, MateriaCodigo y Grupo", ct);
                return;
            }

            var insc = await ResolveInscripcionAsync(registro, periodoId, autoCreate: false, ct);
            if (insc is null)
            {
                await MarkSkipAsync(tx, "Inscripcion no existe", ct);
                return;
            }

            var gm = await ResolveGrupoMateriaAsync(materiaCodigo, grupo, periodoId, ct);
            if (gm is null)
            {
                await MarkSkipAsync(tx, "GrupoMateria no existe", ct);
                return;
            }

            var det = await _db.DetallesInscripciones
                .FirstOrDefaultAsync(d => d.InscripcionId == insc.Id && d.GrupoMateriaId == gm.Id, ct);

            if (det is null)
            {
                await MarkSkipAsync(tx, "Nada que eliminar (idempotente)", ct);
                return;
            }

            _db.DetallesInscripciones.Remove(det);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("DELETE ok: Detalle eliminado (Tx {TxId})", tx.Id);
        }

        // =======================
        //       HELPERS
        // =======================
        private async Task<Inscripcion?> ResolveInscripcionAsync(string registro, int periodoId, bool autoCreate, CancellationToken ct)
        {
            // Busca Inscripcion por Estudiante.Registro + PeriodoId
            var insc = await _db.Inscripciones
                .Include(i => i.Estudiante)
                .FirstOrDefaultAsync(i => i.PeriodoId == periodoId && i.Estudiante.Registro == registro, ct);

            if (insc is not null) return insc;
            if (!autoCreate) return null;

            // Auto-crear estudiante mínimo si no existe
            var est = await _db.Estudiantes.FirstOrDefaultAsync(e => e.Registro == registro, ct);
            if (est is null)
            {
                est = new Estudiante
                {
                    Registro = registro,
                    Ci = "",
                    PasswordHash = "",
                    Nombre = registro,
                    Email = $"{registro}@auto.local", // placeholder para cumplir NOT NULL si aplica
                    Estado = "ACTIVO",
                    CarreraId = _db.Carreras.Select(c => c.Id).FirstOrDefault() // si no hay, queda 0 (ajusta si FK NOT NULL)
                };
                _db.Estudiantes.Add(est);
                await _db.SaveChangesAsync(ct);
            }

            insc = new Inscripcion
            {
                EstudianteId = est.Id,
                PeriodoId = periodoId,
                Estado = "ACTIVA",
                Fecha = DateTime.UtcNow
            };
            _db.Inscripciones.Add(insc);
            await _db.SaveChangesAsync(ct);
            return insc;
        }

        private async Task<GrupoMateria?> ResolveGrupoMateriaAsync(string materiaCodigo, string grupo, int periodoId, CancellationToken ct)
        {
            return await _db.GruposMaterias
                .Include(g => g.Materia)
                .FirstOrDefaultAsync(g =>
                    g.Materia.Codigo == materiaCodigo &&
                    g.Grupo == grupo &&
                    g.PeriodoId == periodoId, ct);
        }

        private static string N(string s) => s.Trim().ToUpperInvariant();

        private static string GetStr(JsonElement obj, string prop) =>
            obj.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString() ?? string.Empty
                : string.Empty;

        private static string? GetStrOrNull(JsonElement obj, string prop) =>
            obj.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;

        private static int GetInt(JsonElement obj, string prop) =>
            obj.TryGetProperty(prop, out var p) && p.TryGetInt32(out var v) ? v : 0;

        private static bool GetBool(JsonElement obj, string prop, bool defaultValue = false)
        {
            if (!obj.TryGetProperty(prop, out var p)) return defaultValue;
            return p.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => defaultValue
            };
        }

        private async Task MarkSkipAsync(Transaccion tx, string motivo, CancellationToken ct)
        {
            _log.LogWarning("Tx {TxId} SKIP: {Motivo}", tx.Id, motivo);
            tx.Estado = "SKIP";
            await _guard.MarkProcessedAsync(tx.Id, ct);
        }
    }
}
