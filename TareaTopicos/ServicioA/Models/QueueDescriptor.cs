namespace TAREATOPICOS.ServicioA.Models;

/// <summary>
/// Definición/config de una cola en tiempo de ejecución.
/// (Nombre, concurrencia deseada, límites y políticas de reintento)
/// </summary>
public sealed class QueueDescriptor
{
    public string Name { get; set; } = "default";

    // Concurrencia deseada (n° de workers) — si no usas aquí,
    // lo puedes manejar solo desde appsettings u AdminController.
    public int Workers { get; set; } = 1;

    // Prioridades admitidas (0..Priorities-1). Usamos 3 por defecto: 0 alta, 1 normal, 2 baja
    public int Priorities { get; set; } = 3;

    // Límite de tareas "en vuelo" (semaforizado por RateLimiter)
    public int MaxInFlight { get; set; } = 50;

    // Límite de encoladas pendientes (0 = sin tope). Útil para backpressure.
    public int MaxQueued { get; set; } = 0;

    // Política de reintento
    public int MaxRetries { get; set; } = 5;
    public int BaseBackoffMs { get; set; } = 300;

    // Estado operativo
    public bool Paused { get; set; } = false;
}
