
namespace TAREATOPICOS.ServicioA.Models;

public class DetalleInscripcion
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Estado { get; set; } = "INSCRITO";
    public int? NotaFinal { get; set; }
    public int InscripcionId { get; set; }
    public Inscripcion Inscripcion { get; set; } = null!;

    public int GrupoMateriaId { get; set; }
    public GrupoMateria GrupoMateria { get; set; } = null!;

    public HistorialAcademico? HistorialAcademico { get; set; }
}
