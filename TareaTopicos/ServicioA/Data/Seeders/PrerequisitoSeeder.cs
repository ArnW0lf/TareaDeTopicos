using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class PrerequisitoSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prerequisito>().HasData(

                // Segundo semestre
                new Prerequisito { Id = 1, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 6, MateriaPrerequisitoId = 1 },   // MAT102 requiere MAT101
                new Prerequisito { Id = 2, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 7, MateriaPrerequisitoId = 2 },   // MAT103 requiere INF119
                new Prerequisito { Id = 3, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 8, MateriaPrerequisitoId = 3 },   // INF120 requiere INF110
                new Prerequisito { Id = 4, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 9, MateriaPrerequisitoId = 4 },   // FIS102 requiere FIS100
                new Prerequisito { Id = 5, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 10, MateriaPrerequisitoId = 5 },  // LIN101 requiere LIN100

                // Tercer semestre
                new Prerequisito { Id = 6, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 11, MateriaPrerequisitoId = 6 },  // MAT207 requiere MAT102
                new Prerequisito { Id = 7, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 12, MateriaPrerequisitoId = 7 },  // INF210 requiere MAT103
                new Prerequisito { Id = 8, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 12, MateriaPrerequisitoId = 8 },  // INF210 requiere INF120
                new Prerequisito { Id = 9, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 13, MateriaPrerequisitoId = 8 },  // INF211 requiere INF120
                new Prerequisito { Id = 10, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 13, MateriaPrerequisitoId = 9 }, // INF211 requiere FIS102
                new Prerequisito { Id = 11, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 14, MateriaPrerequisitoId = 9 }, // FIS200 requiere FIS102

                // Cuarto semestre
                new Prerequisito { Id = 12, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 16, MateriaPrerequisitoId = 6 }, // MAT202 requiere MAT102
                new Prerequisito { Id = 13, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 17, MateriaPrerequisitoId = 11 }, // MAT205 requiere MAT207
                new Prerequisito { Id = 14, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 18, MateriaPrerequisitoId = 12 }, // INF220 requiere INF210
                new Prerequisito { Id = 15, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 19, MateriaPrerequisitoId = 13 }, // INF221 requiere INF211
                new Prerequisito { Id = 16, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 20, MateriaPrerequisitoId = 15 }, // ADM200 requiere ADM100

                // Quinto semestre
                new Prerequisito { Id = 17, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 21, MateriaPrerequisitoId = 16 }, // MAT302 requiere MAT202
                new Prerequisito { Id = 18, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 22, MateriaPrerequisitoId = 18 }, // INF318 requiere INF220
                new Prerequisito { Id = 19, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 23, MateriaPrerequisitoId = 18 }, // INF310 requiere INF220
                new Prerequisito { Id = 20, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 24, MateriaPrerequisitoId = 18 }, // INF312 requiere INF220
                new Prerequisito { Id = 21, Tipo = "OBLIGATORIO", NotaMin = 51, MateriaId = 25, MateriaPrerequisitoId = 18 }  // INF319 requiere INF220

            );
        }
    }
}
