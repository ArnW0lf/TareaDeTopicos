namespace TAREATOPICOS.ServicioA.Models;

public class Carrera
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;

    // Navigation
    public ICollection<PlanDeEstudio> PlanesDeEstudio { get; set; } = new List<PlanDeEstudio>();
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
}
