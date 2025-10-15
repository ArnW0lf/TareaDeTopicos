namespace TAREATOPICOS.ServicioA.Dtos;

public class HistorialAcademicoDto
    {
        public int Id { get; set; }
        public int Intento { get; set; } = 1;
        public decimal? UltimaNota { get; set; }
        public bool Aprobado { get; set; } = false;
        public DateTime? FechaAprobacion { get; set; }
        public int DetalleInscripcionId { get; set; }
    }
