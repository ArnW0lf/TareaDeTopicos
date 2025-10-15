using System; 

namespace TAREATOPICOS.ServicioA.Models
{
    // Marca mensajes (Transaccion.Id) que YA se procesaron con éxito.
    public class ProcessedMessage
    {
        public Guid MessageId { get; set; }             // usa Transaccion.Id
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
