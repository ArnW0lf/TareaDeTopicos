//TareaTopicos/ServicioA/Dtos/request/EstudianteRequestDto.cs
namespace TAREATOPICOS.ServicioA.Dtos.request;

public class EstudianteRequestDto
    {
        public int Id { get; set; }
        public string Registro { get; set; } = null!;
        public string Ci { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public int CarreraId { get; set; }


        //  Solo se usa en Create/Update
        public string? Password { get; set; }
    }
