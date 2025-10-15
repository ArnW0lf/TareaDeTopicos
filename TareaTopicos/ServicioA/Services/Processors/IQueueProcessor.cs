using System.Threading;
using System.Threading.Tasks;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public interface IQueueProcessor
    {
        Task ProcessAsync(TAREATOPICOS.ServicioA.Models.Transaccion tx, CancellationToken ct);
    }
}
