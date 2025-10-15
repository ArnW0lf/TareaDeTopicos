namespace TAREATOPICOS.ServicioA.Models;

/// <summary>
/// Descriptor pequeño para ordenar al host cambiar
/// la concurrencia de una cola en caliente.
/// </summary>
public sealed class WorkerDescriptor
{
    public string QueueName { get; set; } = "default";
    public int Workers { get; set; } = 1;
    public bool Paused { get; set; } = false;
}
