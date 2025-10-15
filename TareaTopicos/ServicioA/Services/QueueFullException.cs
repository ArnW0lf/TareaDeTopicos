namespace TAREATOPICOS.ServicioA.Services;

public class QueueFullException : Exception
{
    public string QueueName { get; }
    public long Backlog { get; }
    public long MaxAllowed { get; }

    public QueueFullException(string queueName, long backlog, long maxAllowed)
        : base($"Cola {queueName} llena ({backlog}/{maxAllowed})")
    {
        QueueName = queueName;
        Backlog = backlog;
        MaxAllowed = maxAllowed;
    }
}
