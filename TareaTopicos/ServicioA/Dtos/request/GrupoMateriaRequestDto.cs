namespace TAREATOPICOS.ServicioA.Dtos.request;

public class GrupoMateriaRequestDto
    {
        public int Id { get; set; }
        public string Grupo { get; set; } = null!;
        public int Cupo { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public int MateriaId { get; set; }
        public int DocenteId { get; set; }
        public int PeriodoId { get; set; }
        public int? HorarioId { get; set; }
        public int? AulaId { get; set; }
    }
