//TareaTopicos/ServicioA/Dtos/DetalleInscripcionDto.cs
namespace TAREATOPICOS.ServicioA.Dtos;

public class DetalleInscripcionDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Estado { get; set; } = "INSCRITO";
        public int? NotaFinal { get; set; }
        public int InscripcionId { get; set; }
        public int GrupoMateriaId { get; set; }
    }