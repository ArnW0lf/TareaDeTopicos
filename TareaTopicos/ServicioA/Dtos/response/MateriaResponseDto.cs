// Dtos/Request/MateriaResponse.cs
using TAREATOPICOS.ServicioA.Dtos;
namespace TAREATOPICOS.ServicioA.Dtos.response;

public class MateriaResponseDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int Creditos { get; set; }
        public NivelDto Nivel { get; set; } = null!;
    }
