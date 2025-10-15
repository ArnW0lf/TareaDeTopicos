namespace TAREATOPICOS.ServicioA.Options;

/// <summary>
/// Config por cola (se lee de appsettings.json: "Queues": [ ... ]).
/// </summary>
public sealed class QueueItemOptions
{
    public string Name { get; set; } = "default";
    public int Workers { get; set; } = 1;
    public int MaxInFlight { get; set; } = 50;
    public int MaxRetries { get; set; } = 5;
    public int BaseBackoffMs { get; set; } = 300;
    public int Priorities { get; set; } = 3;
    public int MaxQueued { get; set; } = 0; // 0 = sin límite (puedes implementarlo luego)
}

/// <summary>
/// Contenedor opcional si quieres bind tipado de toda la lista.
/// (Si no, puedes leer la sección "Queues" directamente desde IConfiguration.)
/// </summary>
public sealed class QueuesOptions
{
    public List<QueueItemOptions> Queues { get; set; } = new();
}
