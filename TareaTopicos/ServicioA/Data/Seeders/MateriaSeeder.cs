using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class MateriaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Materia>().HasData(
                new Materia { Id = 1, Codigo = "MAT101", Nombre = "CALCULO I", Creditos = 15, NivelId = 1 },
                new Materia { Id = 2, Codigo = "INF119", Nombre = "ESTRUCTURAS DISCRETAS", Creditos = 20, NivelId = 1 },
                new Materia { Id = 3, Codigo = "INF110", Nombre = "INTRODUCCION A LA INFORMATICA", Creditos = 20, NivelId = 1 },
                new Materia { Id = 4, Codigo = "FIS100", Nombre = "FISICA I", Creditos = 15, NivelId = 1 },
                new Materia { Id = 5, Codigo = "LIN100", Nombre = "INGLES I", Creditos = 15, NivelId = 1 },


                new Materia { Id = 6, Codigo = "MAT102", Nombre = "CALCULO II", Creditos = 20, NivelId = 2 },
                new Materia { Id = 7, Codigo = "MAT103", Nombre = "ALGEBRA LINEAL", Creditos = 20, NivelId = 2 },
                new Materia { Id = 8, Codigo = "INF120", Nombre = "PROGRAMACION I", Creditos = 20, NivelId = 2 },
                new Materia { Id = 9, Codigo = "FIS102", Nombre = "FISICA II", Creditos = 20, NivelId = 2 },
                new Materia { Id = 10, Codigo = "LIN101", Nombre = "INGLES II", Creditos = 20, NivelId = 2 },


                new Materia { Id = 11, Codigo = "MAT207", Nombre = "ECUACIONES DIFERENCIALES", Creditos = 20, NivelId = 3 },
                new Materia { Id = 12, Codigo = "INF210", Nombre = "PROGRAMACION II", Creditos = 20, NivelId = 3 },
                new Materia { Id = 13, Codigo = "INF211", Nombre = "ARQUITECTURA DE COMPUTADORAS", Creditos = 20, NivelId = 3 },
                new Materia { Id = 14, Codigo = "FIS200", Nombre = "FISICA III", Creditos = 20, NivelId = 3 },
                new Materia { Id = 15, Codigo = "ADM100", Nombre = "ADMINISTRACION", Creditos = 20, NivelId = 3 },


                new Materia { Id = 16, Codigo = "MAT202", Nombre = "PROBABILIDAD Y ESTADISTICA I", Creditos = 20, NivelId = 4 },
                new Materia { Id = 17, Codigo = "MAT205", Nombre = "METODOS NUMERICOS", Creditos = 20, NivelId = 4 },
                new Materia { Id = 18, Codigo = "INF220", Nombre = "ESTRUCTURA DE DATOS I", Creditos = 20, NivelId = 4 },
                new Materia { Id = 19, Codigo = "INF221", Nombre = "PROGRAMACION ENSAMBLADOR", Creditos = 20, NivelId = 4 },
                new Materia { Id = 20, Codigo = "ADM200", Nombre = "CONTABILIDAD", Creditos = 20, NivelId = 4 },


                new Materia { Id = 21, Codigo = "MAT302", Nombre = "PROBABILIDAD Y ESTADISTICA II", Creditos = 20, NivelId = 5 },
                new Materia { Id = 22, Codigo = "INF318", Nombre = "PROGRAMACION LOGICA Y FUNCIONAL", Creditos = 20, NivelId = 5 },
                new Materia { Id = 23, Codigo = "INF310", Nombre = "ESTRUCTURA DE DATOS II", Creditos = 20, NivelId = 5 },
                new Materia { Id = 24, Codigo = "INF312", Nombre = "BASE DE DATOS I", Creditos = 20, NivelId = 5 },
                new Materia { Id = 25, Codigo = "INF319", Nombre = "LENGUAJES FORMALES", Creditos = 20, NivelId = 5 }


            );
        }
    }
}
