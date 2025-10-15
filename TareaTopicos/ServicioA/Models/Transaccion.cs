namespace TAREATOPICOS.ServicioA.Models;

public class Transaccion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    
    public string TipoOperacion { get; set; } = string.Empty;

 
    public string Entidad { get; set; } = string.Empty;

    
    public string? Payload { get; set; }
  public string Estado { get; set; } = "EN_COLA";
 public int Attempt { get; set; } = 0;

 
    public int Priority { get; set; } = 1;

 
    public int MaxRetries { get; set; } = 3;

   

   public string? CallbackUrl { get; set; }          // <— nuevo
    public string? CallbackSecret { get; set; }       // <— nuevo (para HMAC)
    public string? IdempotencyKey { get; set; }       // <— nuevo

    
    public DateTimeOffset NotBefore { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
} 
