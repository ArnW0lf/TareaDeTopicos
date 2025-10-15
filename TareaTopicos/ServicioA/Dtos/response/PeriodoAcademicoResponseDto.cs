//TareaTopicos/ServicioA/Dtos/response/PeriodoAcademicoResponseDto.cs
namespace TAREATOPICOS.ServicioA.Dtos.response;

public class PeriodoAcademicoResponseDto
    {
        public string Gestion { get; set; } = null!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
