namespace TAREATOPICOS.ServicioA.Models;

public class Nivel
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = null!;

    public ICollection<Materia> Materias { get; set; } = new List<Materia>();
}
