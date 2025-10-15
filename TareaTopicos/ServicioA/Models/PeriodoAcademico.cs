namespace TAREATOPICOS.ServicioA.Models;

public class PeriodoAcademico
{
    public int Id { get; set; }
    public string Gestion { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public ICollection<GrupoMateria> Grupos { get; set; } = new List<GrupoMateria>();
    public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
}

