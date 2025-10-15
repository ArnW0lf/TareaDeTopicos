namespace TAREATOPICOS.ServicioA.Models;

public class PlanDeEstudio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Codigo { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "ACTIVO";

    public int CarreraId { get; set; }
    public Carrera Carrera { get; set; } = null!;

    public ICollection<PlanMateria> PlanesMaterias { get; set; } = new List<PlanMateria>();
}

