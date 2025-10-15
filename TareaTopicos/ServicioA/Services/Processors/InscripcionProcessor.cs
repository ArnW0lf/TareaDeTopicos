 
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors;

public sealed class InscripcionProcessor : IProcessor
{
    private readonly ServicioAContext _db;
    private readonly ILogger<InscripcionProcessor> _log;

    public InscripcionProcessor(ServicioAContext db, ILogger<InscripcionProcessor> log)
    {
        _db = db;
        _log = log;
    }

    // === Payload DTO ===
    private sealed record PayloadInscripcion(
        string Registro,
        int PeriodoId,
        List<MateriaGrupo> Materias,
        int InscripcionId
    );

    private sealed record MateriaGrupo(string MateriaCodigo, string Grupo);

    // === Procesamiento principal ===
    public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
    {
        _log.LogInformation("📥 Procesando Tx {TxId} ({Entidad})", tx.Id, tx.Entidad);

        if (string.IsNullOrWhiteSpace(tx.Payload))
        {
            Skip(tx, "Payload vacío o nulo");
            return;
        }

        PayloadInscripcion? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PayloadInscripcion>(tx.Payload);
        }
        catch (Exception ex)
        {
            Skip(tx, $"Payload inválido: {ex.Message}");
            return;
        }

        if (payload == null)
        {
            Skip(tx, "Payload nulo o sin formato válido");
            return;
        }

        await using var dbTx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // 🔎 Buscar inscripción base (ya creada como PENDIENTE)
            var inscripcion = await _db.Inscripciones
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.Id == payload.InscripcionId, ct);

            if (inscripcion == null)
            {
                Skip(tx, $"Inscripción base {payload.InscripcionId} no encontrada");
                await dbTx.RollbackAsync(ct);
                return;
            }

            var estudiante = await _db.Estudiantes
                .FirstOrDefaultAsync(e => e.Registro == payload.Registro, ct);

            if (estudiante == null)
            {
                _log.LogWarning("⚠️ Estudiante {Registro} no encontrado", payload.Registro);
                inscripcion.Estado = "RECHAZADA";
                await _db.SaveChangesAsync(ct);
                await dbTx.CommitAsync(ct);
                return;
            }

            int confirmadas = 0;
            int total = payload.Materias.Count;

            foreach (var m in payload.Materias)
            {
                var grupo = await _db.GruposMaterias
                    .Include(g => g.Materia)
                    .FirstOrDefaultAsync(g =>
                        g.Materia.Codigo == m.MateriaCodigo &&
                        g.Grupo == m.Grupo &&
                        g.PeriodoId == payload.PeriodoId, ct);

                if (grupo == null)
                {
                    _log.LogWarning("⚠️ Grupo no encontrado para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
                    continue;
                }

                if (grupo.Cupo <= 0)
                {
                    _log.LogWarning("❌ Sin cupos para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
                    continue;
                }

                // Crear detalle si aún no existe
                bool yaInscrito = inscripcion.Detalles.Any(d => d.GrupoMateriaId == grupo.Id);
                if (!yaInscrito)
                {
                    var detalle = new DetalleInscripcion
                    {
                        Codigo = $"{m.MateriaCodigo}-{m.Grupo}-{inscripcion.Id}",
                        Estado = "INSCRITO",
                        InscripcionId = inscripcion.Id,
                        GrupoMateriaId = grupo.Id
                    };
                    _db.DetallesInscripciones.Add(detalle);
                    confirmadas++;

                    grupo.Cupo -= 1;
                    _db.GruposMaterias.Update(grupo);

                    _log.LogInformation("📉 Cupo actualizado {MateriaCodigo}-{Grupo}: nuevo cupo={NuevoCupo}",
                        m.MateriaCodigo, m.Grupo, grupo.Cupo);
                }
            }

            // 🟢 Actualizar estado final
            inscripcion.Estado = confirmadas switch
            {
                0 => "RECHAZADA",
                var c when c < total => "PARCIAL",
                _ => "CONFIRMADA"
            };

            inscripcion.Fecha = DateTime.UtcNow; // Actualizamos la fecha también
            await _db.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);

            // 🔁 Actualizar estado de la transacción
            tx.Estado = inscripcion.Estado switch
            {
                "CONFIRMADA" => "OK",
                "PARCIAL" => "OK_PARTIAL",
                "RECHAZADA" => "REJECTED",
                _ => "COMPLETADO"
            };

            _log.LogInformation("✅ Tx {TxId} → Inscripción {Id} {Estado} ({Confirmadas}/{Total})",
                tx.Id, inscripcion.Id, inscripcion.Estado, confirmadas, total);
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync(ct);
            tx.Estado = "ERROR";
            _log.LogError(ex, "💥 Error procesando Tx {TxId}", tx.Id);
        }
    }

    private void Skip(Transaccion tx, string motivo)
    {
        tx.Estado = "SKIP";
        _log.LogWarning("⚠️ Tx {TxId} omitida: {Motivo}", tx.Id, motivo);
    }
}
