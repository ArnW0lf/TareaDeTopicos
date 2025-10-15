// Dtos/Request/MateriaRequest.cs
namespace TAREATOPICOS.ServicioA.Dtos.request;

public class MateriaRequestDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int Creditos { get; set; }
        public int NivelId { get; set; }
    }
