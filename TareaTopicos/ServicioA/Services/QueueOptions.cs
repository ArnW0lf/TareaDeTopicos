namespace TAREATOPICOS.ServicioA.Services.Options;

public class QueueOptions
{
    public string Name { get; set; } = "default";
    public int Workers { get; set; } = 1;
    public int MaxInFlight { get; set; } = 50;
    public int MaxRetries { get; set; } = 5;
    public int BaseBackoffMs { get; set; } = 300;
    public int Priorities { get; set; } = 3;

    // 🚦 para backpressure
    public int MaxPendingTasks { get; set; } = 100000;
    public string RejectPolicy { get; set; } = "reject"; // reject | deadletter | block
}
