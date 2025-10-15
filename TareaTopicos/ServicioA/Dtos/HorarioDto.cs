namespace TAREATOPICOS.ServicioA.Dtos;

public class HorarioDto
    {
        public int Id { get; set; }
        public string Dia { get; set; } = null!;
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
    }
