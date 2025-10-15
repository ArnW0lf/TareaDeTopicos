namespace TAREATOPICOS.ServicioA.Options;

/// <summary>
/// Config general de Redis (conexión) y parámetros de la cola en Redis.
/// Mapea secciones: "Redis" y "RedisQueue" de appsettings.json
/// </summary>
public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
}

public sealed class RedisQueueOptions
{
    public string KeyPrefix { get; set; } = "q:";
    public int VisibilityTimeoutSeconds { get; set; } = 60;
}
