using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class PlanDeEstudioSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanDeEstudio>().HasData(
                new PlanDeEstudio { Id = 1, Nombre = "Plan 2025", Codigo = "SIS25", Estado = "ACTIVO", CarreraId = 1, Fecha = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
                new PlanDeEstudio { Id = 2, Nombre = "Plan 2024", Codigo = "IND24", Estado = "ACTIVO", CarreraId = 2, Fecha = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) }
            );
        }
    }
}
