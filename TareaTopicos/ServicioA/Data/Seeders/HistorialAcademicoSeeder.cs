using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class HistorialAcademicoSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HistorialAcademico>().HasData(
                new HistorialAcademico { Id = 1, Intento = 1, UltimaNota = 85, Aprobado = true, FechaAprobacion = new DateTime(2025,6,30,0,0,0,DateTimeKind.Utc), DetalleInscripcionId = 1 },
                new HistorialAcademico { Id = 2, Intento = 1, UltimaNota = 70, Aprobado = false, FechaAprobacion = new DateTime(2025,6,30,0,0,0,DateTimeKind.Utc), DetalleInscripcionId = 2 }
            );
        }
    }
}
