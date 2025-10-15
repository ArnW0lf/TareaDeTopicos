using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class NivelSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Nivel>().HasData(
                new Nivel { Id = 1, Numero = 1, Nombre = "1er Semestre" },
                new Nivel { Id = 2, Numero = 2, Nombre = "2do Semestre" },
                new Nivel { Id = 3, Numero = 3, Nombre = "3er Semestre" },
                new Nivel { Id = 4, Numero = 4, Nombre = "4to Semestre" },
                new Nivel { Id = 5, Numero = 5, Nombre = "5to Semestre" }
            );
        }
    }
}
