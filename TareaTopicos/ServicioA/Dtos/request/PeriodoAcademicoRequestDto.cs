namespace TAREATOPICOS.ServicioA.Dtos.request;

public class PeriodoAcademicoRequestDto
    {
        public int Id { get; set; }
        public string Gestion { get; set; } = null!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
