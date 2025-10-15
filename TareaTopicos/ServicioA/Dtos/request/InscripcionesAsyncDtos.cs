namespace TAREATOPICOS.ServicioA.Dtos.request;

// Crear "carátula" de inscripción
public class CrearInscripcionAsyncDto
{
    public int EstudianteId { get; set; }
    public int PeriodoId { get; set; }
}

// Agregar un grupo (materia + horario) a la inscripción
public class AgregarDetalleAsyncDto
{
    public int InscripcionId { get; set; }
    public int GrupoMateriaId { get; set; }
}

// Quitar un grupo por DetalleId (cuando lo conoces)
public class QuitarDetallePorIdAsyncDto
{
    public int InscripcionId { get; set; }
    public int DetalleId { get; set; }
}

// Quitar un grupo por GrupoMateriaId (alternativa si no conoces DetalleId)
public class QuitarDetallePorGrupoAsyncDto
{
    public int InscripcionId { get; set; }
    public int GrupoMateriaId { get; set; }
}

// Finalizar inscripción
public class FinalizarInscripcionAsyncDto
{
    public int InscripcionId { get; set; }
}
