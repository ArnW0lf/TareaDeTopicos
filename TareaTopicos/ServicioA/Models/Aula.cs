namespace TAREATOPICOS.ServicioA.Models;

public class Aula
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public int Capacidad { get; set; }
    public string Ubicacion { get; set; } = null!;

    public ICollection<GrupoMateria> Grupos { get; set; } = new List<GrupoMateria>();
}





