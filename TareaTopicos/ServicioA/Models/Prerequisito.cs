namespace TAREATOPICOS.ServicioA.Models;

public class Prerequisito
{
    public int Id { get; set; }
    public string Tipo { get; set; } = null!;
    public decimal NotaMin { get; set; }

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    public int MateriaPrerequisitoId { get; set; }
    public Materia MateriaPrerequisito { get; set; } = null!;
}

