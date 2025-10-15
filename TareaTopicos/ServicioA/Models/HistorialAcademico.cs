namespace TAREATOPICOS.ServicioA.Models;

public class HistorialAcademico
    {
        public int Id { get; set; }
        public int Intento { get; set; } = 1;
        public decimal? UltimaNota { get; set; }
        public bool Aprobado { get; set; } = false;
        public DateTime? FechaAprobacion { get; set; }

        public int DetalleInscripcionId { get; set; }
        public DetalleInscripcion DetalleInscripcion { get; set; } = null!;
    }
