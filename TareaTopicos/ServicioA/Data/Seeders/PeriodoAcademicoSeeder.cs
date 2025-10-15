using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class PeriodoAcademicoSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PeriodoAcademico>().HasData(
                new PeriodoAcademico { Id = 1, Gestion = "2025-1", FechaInicio = new DateTime(2025,2,1,0,0,0,DateTimeKind.Utc), FechaFin = new DateTime(2025,6,30,0,0,0,DateTimeKind.Utc) },
                new PeriodoAcademico { Id = 2, Gestion = "2025-2", FechaInicio = new DateTime(2025,8,1,0,0,0,DateTimeKind.Utc), FechaFin = new DateTime(2025,12,15,0,0,0,DateTimeKind.Utc) }
            );
        }
    }
}
