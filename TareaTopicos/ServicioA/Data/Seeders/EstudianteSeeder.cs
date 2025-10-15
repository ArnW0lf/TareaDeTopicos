using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class EstudianteSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Estudiante>().HasData(
                new Estudiante { Id = 1, Registro = "20251234", Ci = "7894561",PasswordHash = "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq", Nombre = "Carlos Sánchez", Email = "carlos@uni.edu", Estado = "ACTIVO", CarreraId = 1 },
                new Estudiante { Id = 2, Registro = "EST002", Ci = "222222",PasswordHash = "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq", Nombre = "Ana Rodríguez", Email = "ana@uni.edu", Estado = "ACTIVO", CarreraId = 2 }
            );
        }
    }
}
