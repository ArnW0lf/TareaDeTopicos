namespace TAREATOPICOS.ServicioA.Models;

public class Horario
{
    public int Id { get; set; }
    public string Dia { get; set; } = null!;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    public ICollection<GrupoMateria> Grupos { get; set; } = new List<GrupoMateria>();
}

