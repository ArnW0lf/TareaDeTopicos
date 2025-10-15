namespace TAREATOPICOS.ServicioA.Models
{
    public enum ProcessOutcome
    {
        Success,          // OK → ack
        RetryableFailure, // fallo transitorio → requeue/backoff
        NotFoundSkip,     // no existe → ack sin reintento (SKIP)
        InvalidSkip       // inválido / regla de negocio → ack sin reintento (SKIP)
    }

    public record ProcessResult(ProcessOutcome Outcome, string? Message = null)
    {
        public static ProcessResult Ok() => new(ProcessOutcome.Success);
        public static ProcessResult Retry(string msg) => new(ProcessOutcome.RetryableFailure, msg);
        public static ProcessResult NotFound(string msg) => new(ProcessOutcome.NotFoundSkip, msg);
        public static ProcessResult Invalid(string msg) => new(ProcessOutcome.InvalidSkip, msg);
    }
}
