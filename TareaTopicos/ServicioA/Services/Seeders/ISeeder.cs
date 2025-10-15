using TAREATOPICOS.ServicioA.Data;

namespace TAREATOPICOS.ServicioA.Services.Seeders
{
    public interface ISeeder
    {
        Task SeedAsync(ServicioAContext db, CancellationToken ct = default);
    }
}
