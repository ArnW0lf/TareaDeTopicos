using TAREATOPICOS.ServicioA.Dtos;
namespace TAREATOPICOS.ServicioA.Dtos.response;

public class GrupoMateriaResponseDto
    {
        public int Id { get; set; }
        public string Grupo { get; set; } = null!;
        public int Cupo { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public MateriaResponseDto Materia { get; set; } = null!;
        public DocenteDto Docente { get; set; } =null!;
        public PeriodoAcademicoResponseDto Periodo { get; set; } = null!;
        public HorarioDto Horario { get; set; }=null!;
        public AulaDto Aula { get; set; } = null!;
    }
