using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class AulaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Aula>().HasData(
                new Aula { Id = 1, Codigo = "A101", Capacidad = 40, Ubicacion = "Bloque A" },
                new Aula { Id = 2, Codigo = "B201", Capacidad = 35, Ubicacion = "Bloque B" },
                new Aula { Id = 3, Codigo = "C301", Capacidad = 50, Ubicacion = "Bloque C" }
            );
        }
    }
}