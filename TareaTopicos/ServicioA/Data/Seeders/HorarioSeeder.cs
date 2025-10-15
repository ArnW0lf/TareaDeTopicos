using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class HorarioSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Horario>().HasData(
                new Horario { Id = 1, Dia = "Lunes", HoraInicio = new TimeOnly(8,0), HoraFin = new TimeOnly(10,0) },
                new Horario { Id = 2, Dia = "Martes", HoraInicio = new TimeOnly(10,0), HoraFin = new TimeOnly(12,0) }
            );
        }
    }
}
