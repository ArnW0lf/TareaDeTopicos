namespace TAREATOPICOS.ServicioA.Models;

public class Inscripcion
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "PENDIENTE";

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public int PeriodoId { get; set; }
    public PeriodoAcademico Periodo { get; set; } = null!;

    public ICollection<DetalleInscripcion> Detalles { get; set; } = new List<DetalleInscripcion>();
}
