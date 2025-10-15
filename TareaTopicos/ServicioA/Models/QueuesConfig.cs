namespace TAREATOPICOS.ServicioA.Models;

/// <summary>
/// Snapshot simple de configuración de colas en el servicio.
/// </summary>
public sealed class QueuesConfig
{
    public List<QueueDescriptor> Queues { get; set; } = new();
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
