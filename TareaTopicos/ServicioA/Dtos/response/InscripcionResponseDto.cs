//TareaTopicos/ServicioA/Dtos/response/InscripcionResponseDto.cs
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.response;


namespace TAREATOPICOS.ServicioA.Dtos.response;
public class InscripcionResponseDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public EstudianteResponseDto Estudiante { get; set; } = null!;
    public PeriodoAcademicoResponseDto Periodo { get; set; } = null!;
}