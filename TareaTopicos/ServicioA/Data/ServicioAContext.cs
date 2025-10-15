using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data
{
    public class ServicioAContext : DbContext
    {
        public ServicioAContext(DbContextOptions<ServicioAContext> options) : base(options) { }
        

        // DbSets
        public DbSet<Carrera> Carreras { get; set; }
        public DbSet<PlanDeEstudio> PlanesEstudio { get; set; }
        public DbSet<Nivel> Niveles { get; set; }
        public DbSet<Materia> Materias { get; set; }
        public DbSet<PlanMateria> PlanMaterias { get; set; }

        public DbSet<Prerequisito> Prerequisitos { get; set; }
        public DbSet<Docente> Docentes { get; set; }
        public DbSet<Aula> Aulas { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<PeriodoAcademico> PeriodosAcademicos { get; set; }
        public DbSet<GrupoMateria> GruposMaterias { get; set; }
        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Inscripcion> Inscripciones { get; set; }
        public DbSet<DetalleInscripcion> DetallesInscripciones { get; set; }
        public DbSet<HistorialAcademico> HistorialesAcademicos { get; set; }

        public DbSet<Transaccion> Transacciones { get; set; } = null!;

        public DbSet<ProcessedMessage> ProcessedMessages { get; set; } // para idempotencia

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            // Índices únicos y constraints para tablas puente
            modelBuilder.Entity<PlanMateria>()
                .HasIndex(pm => new { pm.PlanId, pm.MateriaId })
                .IsUnique();

            modelBuilder.Entity<Prerequisito>()
                .HasIndex(p => new { p.MateriaId, p.MateriaPrerequisitoId })
                .IsUnique();

            // Relaciones de PlanMateria
            modelBuilder.Entity<PlanMateria>()
                .HasOne(pm => pm.Plan)
                .WithMany(p => p.PlanesMaterias)
                .HasForeignKey(pm => pm.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanMateria>()
                .HasOne(pm => pm.Materia)
                .WithMany(m => m.PlanesMaterias)
                .HasForeignKey(pm => pm.MateriaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relaciones de Prerequisito (self-reference controlado)
            modelBuilder.Entity<Prerequisito>()
                .HasOne(p => p.Materia)
                .WithMany(m => m.MateriaRequeridaPor)
                .HasForeignKey(p => p.MateriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prerequisito>()
                .HasOne(p => p.MateriaPrerequisito)
                .WithMany(m => m.RequiereMaterias)
                .HasForeignKey(p => p.MateriaPrerequisitoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cupo mínimo en GrupoMateria
            modelBuilder.Entity<GrupoMateria>()
                .Property(g => g.Cupo)
                .HasDefaultValue(0)
                .IsRequired();

            // Restricción de inscripciones únicas
            modelBuilder.Entity<DetalleInscripcion>()
                .HasIndex(d => new { d.InscripcionId, d.GrupoMateriaId })
                .IsUnique();

            // Configuración de relaciones de Inscripción ↔ Estudiante
            modelBuilder.Entity<Inscripcion>()
                .HasOne(i => i.Estudiante)
                .WithMany(e => e.Inscripciones)
                .HasForeignKey(i => i.EstudianteId)
                .OnDelete(DeleteBehavior.Restrict);

            // para controlar la idempotencia
            modelBuilder.Entity<ProcessedMessage>()
                .HasKey(p => p.MessageId);

            // En ServicioAContext.OnModelCreating // para no insertar doble 
            modelBuilder.Entity<Nivel>()
                .HasIndex(n => n.Numero)
                .IsUnique();

            modelBuilder.Entity<Aula>()
              .HasIndex(a => a.Codigo)
              .IsUnique();

            modelBuilder.Entity<Materia>()
              .HasIndex(m => m.Codigo)
              .IsUnique();
            modelBuilder.Entity<Estudiante>()
              .HasIndex(m => m.Registro)
              .IsUnique();  
            modelBuilder.Entity<PeriodoAcademico>()
              .HasIndex(m => m.Gestion)
              .IsUnique();
            modelBuilder.Entity<PlanDeEstudio>()
              .HasIndex(m => m.Codigo)
              .IsUnique();






            // Seeders opcionales
            TAREATOPICOS.ServicioA.Data.Seeders.NivelSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.MateriaSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.PlanDeEstudioSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.CarreraSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.PlanMateriaSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.PrerequisitoSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.DocenteSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.AulaSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.HorarioSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.PeriodoAcademicoSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.GrupoMateriaSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.InscripcionSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.DetalleInscripcionSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.HistorialAcademicoSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.EstudianteSeeder.Seed(modelBuilder);
        }
    }
}