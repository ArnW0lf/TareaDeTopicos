namespace TAREATOPICOS.ServicioA.Contracts;

public class CallbackEvent
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = default!;   // OK | ERROR
    public string Entity { get; set; } = default!;
    public string Operation { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public int Attempt { get; set; }
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}