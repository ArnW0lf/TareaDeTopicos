using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class InscripcionSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Inscripcion>().HasData(
                new Inscripcion { Id = 1, Fecha = new DateTime(2025,2,5,0,0,0,DateTimeKind.Utc), Estado = "PENDIENTE", EstudianteId = 1, PeriodoId = 1 },
                new Inscripcion { Id = 2, Fecha = new DateTime(2025,2,6,0,0,0,DateTimeKind.Utc), Estado = "PENDIENTE", EstudianteId = 2, PeriodoId = 1 }
            );
        }
    }
}
