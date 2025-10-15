// File: Services/Processors/DefaultProcessor.cs
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    /// <summary>
    /// Router simple: resuelve qué IProcessor maneja una Transaccion según Entidad.
    /// Cada IProcessor interno puede diferenciar por TipoOperacion.
    /// </summary>
    public sealed class DefaultProcessor : IQueueProcessor
    {
        private readonly ILogger<DefaultProcessor> _logger;
        private readonly ConcurrentDictionary<string, IProcessor> _map;

        public DefaultProcessor(ILogger<DefaultProcessor> logger,
                                IEnumerable<IProcessor> processors)
        {
            _logger = logger;

            // Construimos el mapa Entidad -> IProcessor a partir de los processors registrados.
            // Si tuvieras múltiples entidades, aquí irías agregando más entradas.
            _map = new ConcurrentDictionary<string, IProcessor>(StringComparer.OrdinalIgnoreCase);
            
            // Itera sobre todos los procesadores inyectados y los mapea por su nombre de entidad.
            foreach (var processor in processors)
            {
                var typeName = processor.GetType().Name;
                if (typeName.Contains("NivelProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["Nivel"] = processor;
                else if (typeName.Contains("MateriaProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["Materia"] = processor;
                else if (typeName.Contains("AulaProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["Aula"] = processor;
                else if (typeName.Contains("DocenteProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["Docente"] = processor;
                else if (typeName.Contains("EstudianteProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["Estudiante"] = processor;    
                else if (typeName.Contains("PeriodoAcademicoProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["PeriodoAcademico"] = processor; 
                else if (typeName.Contains("PlanDeEstudioProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["PlanDeEstudio"] = processor;  
                else if (typeName.Contains("GrupoMateriaProcessor", StringComparison.OrdinalIgnoreCase))
                    _map["GrupoMateria"] = processor;           
                else if (typeName.Contains("DetalleInscripcionProcessor", StringComparison.OrdinalIgnoreCase))
                  _map["DetalleInscripcion"] = processor;
                else if (typeName.Contains("InscripcionProcessor", StringComparison.OrdinalIgnoreCase))
                  _map["Inscripcion"] = processor;

                // Agrega más 'else if' para futuras entidades aquí.
            }
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            // Valida entrada
            if (string.IsNullOrWhiteSpace(tx.Entidad))
                throw new InvalidOperationException("Transaccion sin Entidad");

            // Busca handler por entidad
            if (!_map.TryGetValue(tx.Entidad, out var handler))
            {
                _logger.LogError("DefaultProcessor: no existe processor para Entidad={Entidad} Tipo={Tipo}",
                                 tx.Entidad, tx.TipoOperacion);
                throw new InvalidOperationException($"Processor no definido para {tx.Entidad}/{tx.TipoOperacion}");
            }

            // Delegar en el processor específico (él discrimina por TipoOperacion).
            await handler.ProcessAsync(tx, ct);
        }
    }
}
