//TareaTopicos/ServicioA/Dtos/response/EstudianteResponseDto.cs
using TAREATOPICOS.ServicioA.Dtos;
namespace TAREATOPICOS.ServicioA.Dtos.response;

public class EstudianteResponseDto
    {
        public string Registro { get; set; } = null!;
        public string Ci { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public CarreraDto Carrera { get; set; } = null!;

    }
