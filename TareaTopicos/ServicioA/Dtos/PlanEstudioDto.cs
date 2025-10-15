namespace TAREATOPICOS.ServicioA.Dtos;

public class PlanDeEstudioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Codigo { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public int CarreraId { get; set; }
    }


