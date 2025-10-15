namespace TAREATOPICOS.ServicioA.Models;

public class PlanMateria
{
    public int Id { get; set; }

    public int PlanId { get; set; }
    public PlanDeEstudio Plan { get; set; } = default!;

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = default!;

    public int Semestre { get; set; }
}
