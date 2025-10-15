using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class GrupoMateriaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GrupoMateria>().HasData(
                new GrupoMateria { Id = 1, Grupo = "A", Cupo = 5, Estado = "ACTIVO", MateriaId = 1, DocenteId = 1, PeriodoId = 1, HorarioId = 1, AulaId = 1 },
                new GrupoMateria { Id = 2, Grupo = "B", Cupo = 5, Estado = "ACTIVO", MateriaId = 2, DocenteId = 2, PeriodoId = 1, HorarioId = 2, AulaId = 2 }
            );
        }
    }
}
