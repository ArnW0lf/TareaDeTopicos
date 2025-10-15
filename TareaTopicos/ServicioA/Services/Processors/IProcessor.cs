using System.Threading;
using System.Threading.Tasks;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public interface IProcessor
    {
        Task ProcessAsync(Transaccion tx, CancellationToken ct);
    }
}
