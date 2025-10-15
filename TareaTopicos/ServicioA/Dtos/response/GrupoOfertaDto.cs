namespace TAREATOPICOS.ServicioA.Dtos.response;

public class HorarioOfertaDto
{
    public string Dia { get; set; } = string.Empty; // o int si así lo manejas
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public class GrupoOfertaDto
{
    public int GrupoMateriaId { get; set; }
    public string MateriaCodigo { get; set; } = string.Empty;
    public string MateriaNombre { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public int Cupo { get; set; }
    public string Docente { get; set; } = string.Empty;
    public string Aula { get; set; } = string.Empty;
    public List<HorarioOfertaDto> Horarios { get; set; } = new();
}
