namespace TAREATOPICOS.ServicioA.Models;

public class GrupoMateria
{
    public int Id { get; set; }
    public string Grupo { get; set; } = null!;
    public int Cupo { get; set; }
    public string Estado { get; set; } = "ACTIVO";

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    public int DocenteId { get; set; }
    public Docente Docente { get; set; } = null!;

    public int PeriodoId { get; set; }
    public PeriodoAcademico Periodo { get; set; } = null!;

    public int? HorarioId { get; set; }
    public Horario? Horario { get; set; }

    public int? AulaId { get; set; }
    public Aula? Aula { get; set; }

    public ICollection<DetalleInscripcion> DetallesInscripcion { get; set; } = new List<DetalleInscripcion>();
}

