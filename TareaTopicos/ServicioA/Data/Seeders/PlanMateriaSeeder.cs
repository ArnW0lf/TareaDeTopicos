using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class PlanMateriaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanMateria>().HasData(
                new { Id = 1, PlanId = 1, MateriaId = 1, Semestre = 1 },
                new { Id = 2, PlanId = 1, MateriaId = 2, Semestre = 1 },
                new { Id = 3, PlanId = 1, MateriaId = 3, Semestre = 2 }
            );
        }
    }
}
