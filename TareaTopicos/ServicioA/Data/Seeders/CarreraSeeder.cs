using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class CarreraSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Carrera>().HasData(
                new Carrera { Id = 1, Nombre = "Ingeniería de Sistemas" },
                new Carrera { Id = 2, Nombre = "Ingeniería Industrial" },
                new Carrera { Id = 3, Nombre = "Administración de Empresas" }
            );
        }
    }
}
