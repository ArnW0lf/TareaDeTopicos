namespace TAREATOPICOS.ServicioA.Models;

public class Materia
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public int Creditos { get; set; }

    public int NivelId { get; set; }
    public Nivel Nivel { get; set; } = null!;

    public ICollection<PlanMateria> PlanesMaterias { get; set; } = new List<PlanMateria>();
    public ICollection<GrupoMateria> Grupos { get; set; } = new List<GrupoMateria>();
    public ICollection<Prerequisito> MateriaRequeridaPor { get; set; } = new List<Prerequisito>();
    public ICollection<Prerequisito> RequiereMaterias { get; set; } = new List<Prerequisito>();
}

