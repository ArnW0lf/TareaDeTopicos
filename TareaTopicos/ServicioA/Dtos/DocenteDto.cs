namespace   TAREATOPICOS.ServicioA.Dtos;

public class DocenteDto
    {
        public int Id { get; set; }
        public string Registro { get; set; } = null!;
        public string Ci { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Telefono { get; set; }
        public string Estado { get; set; } = "ACTIVO";
    }
