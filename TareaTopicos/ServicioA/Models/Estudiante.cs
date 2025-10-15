    
namespace TAREATOPICOS.ServicioA.Models;

public class Estudiante
{
    public int Id { get; set; }
    public string Registro { get; set; } = string.Empty;
    public string Ci { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string Estado { get; set; } = "ACTIVO";

    public int CarreraId { get; set; }
    public Carrera Carrera { get; set; } = null!;

    public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
}
  