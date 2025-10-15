namespace TAREATOPICOS.ServicioA.Dtos;

public class PrerequisitoDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = null!;
        public decimal NotaMin { get; set; }
        public int MateriaId { get; set; }
        public int MateriaPrerequisitoId { get; set; }
    }
