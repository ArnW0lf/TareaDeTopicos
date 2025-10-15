namespace   TAREATOPICOS.ServicioA.Dtos;

public class AulaDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public int Capacidad { get; set; }
        public string Ubicacion { get; set; } = null!;
    }