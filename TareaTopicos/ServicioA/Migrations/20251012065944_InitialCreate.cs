using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServicioA.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Ubicacion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aulas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carreras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Docentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Registro = table.Column<string>(type: "text", nullable: false),
                    Ci = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Horarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dia = table.Column<string>(type: "text", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Niveles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Niveles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Gestion = table.Column<string>(type: "text", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosAcademicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoOperacion = table.Column<string>(type: "text", nullable: false),
                    Entidad = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    CallbackUrl = table.Column<string>(type: "text", nullable: true),
                    CallbackSecret = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: true),
                    NotBefore = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estudiantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Registro = table.Column<string>(type: "text", nullable: false),
                    Ci = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CarreraId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estudiantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estudiantes_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanesEstudio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CarreraId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesEstudio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesEstudio_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Creditos = table.Column<int>(type: "integer", nullable: false),
                    NivelId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materias_Niveles_NivelId",
                        column: x => x.NivelId,
                        principalTable: "Niveles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    EstudianteId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Estudiantes_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Estudiantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscripciones_PeriodosAcademicos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "PeriodosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GruposMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Grupo = table.Column<string>(type: "text", nullable: false),
                    Cupo = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false),
                    HorarioId = table.Column<int>(type: "integer", nullable: true),
                    AulaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GruposMaterias_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GruposMaterias_PeriodosAcademicos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "PeriodosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    Semestre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanMaterias_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanMaterias_PlanesEstudio_PlanId",
                        column: x => x.PlanId,
                        principalTable: "PlanesEstudio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prerequisitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    NotaMin = table.Column<decimal>(type: "numeric", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    MateriaPrerequisitoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prerequisitos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prerequisitos_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prerequisitos_Materias_MateriaPrerequisitoId",
                        column: x => x.MateriaPrerequisitoId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesInscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    NotaFinal = table.Column<int>(type: "integer", nullable: true),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    GrupoMateriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesInscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesInscripciones_GruposMaterias_GrupoMateriaId",
                        column: x => x.GrupoMateriaId,
                        principalTable: "GruposMaterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesInscripciones_Inscripciones_InscripcionId",
                        column: x => x.InscripcionId,
                        principalTable: "Inscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialesAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Intento = table.Column<int>(type: "integer", nullable: false),
                    UltimaNota = table.Column<decimal>(type: "numeric", nullable: true),
                    Aprobado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DetalleInscripcionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesAcademicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialesAcademicos_DetallesInscripciones_DetalleInscripc~",
                        column: x => x.DetalleInscripcionId,
                        principalTable: "DetallesInscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Aulas",
                columns: new[] { "Id", "Capacidad", "Codigo", "Ubicacion" },
                values: new object[,]
                {
                    { 1, 40, "A101", "Bloque A" },
                    { 2, 35, "B201", "Bloque B" },
                    { 3, 50, "C301", "Bloque C" }
                });

            migrationBuilder.InsertData(
                table: "Carreras",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Ingeniería de Sistemas" },
                    { 2, "Ingeniería Industrial" },
                    { 3, "Administración de Empresas" }
                });

            migrationBuilder.InsertData(
                table: "Docentes",
                columns: new[] { "Id", "Ci", "Estado", "Nombre", "Registro", "Telefono" },
                values: new object[,]
                {
                    { 1, "123456", "ACTIVO", "ABARCA SOTA NANCY", "DOC001", "70000001" },
                    { 2, "123457", "ACTIVO", "ACOSTA CABEZAS BARTOLO JAVIER", "DOC002", "70000002" },
                    { 3, "123458", "ACTIVO", "AGUILAR MARTINEZ DOMINGO", "DOC003", "70000003" },
                    { 4, "123459", "ACTIVO", "ALIAGA HOWARD SHARON KENNY", "DOC004", "70000004" },
                    { 5, "123460", "ACTIVO", "ALPIRE RIVERO GERMAN", "DOC005", "70000005" },
                    { 6, "123461", "ACTIVO", "ARANIBAR QUIROZ M. MONICA", "DOC006", "70000006" },
                    { 7, "123462", "ACTIVO", "ARGOTE CLAROS IRMA ISABEL", "DOC007", "70000007" },
                    { 8, "123463", "ACTIVO", "ATILA LIJERON JHONNY DAVID", "DOC008", "70000008" },
                    { 9, "123464", "ACTIVO", "AVENDAÑO GONZALES EUDAL", "DOC009", "70000009" },
                    { 10, "123465", "ACTIVO", "BALCAZAR VEIZAGA EVANS", "DOC010", "70000010" },
                    { 11, "123466", "ACTIVO", "BARROSO VIRUEZ GINO", "DOC011", "70000011" },
                    { 12, "123467", "ACTIVO", "CABALLERO RUA MAURICIO CHRISTI", "DOC012", "70000012" },
                    { 13, "123468", "ACTIVO", "CABELLO MERIDA JUAN RUBEN", "DOC013", "70000013" },
                    { 14, "123469", "ACTIVO", "CACERES CHACON BRAULIO", "DOC014", "70000014" },
                    { 15, "123470", "ACTIVO", "CALDERON FLORES MODESTO FRANK", "DOC015", "70000015" },
                    { 16, "123471", "ACTIVO", "CALIZAYA AJHUACHO MAGNO EDWIN", "DOC016", "70000016" },
                    { 17, "123472", "ACTIVO", "CALLE TERRAZAS EDWIN", "DOC017", "70000017" },
                    { 18, "123473", "ACTIVO", "CAMPOS BARRERA MARIO", "DOC018", "70000018" },
                    { 19, "123474", "ACTIVO", "CANO CESPEDES JORGE", "DOC019", "70000019" },
                    { 20, "123475", "ACTIVO", "CARVAJAL CORDERO MARCIO", "DOC020", "70000020" },
                    { 21, "123476", "ACTIVO", "CARRENO PEREIRA ANDRES", "DOC021", "70000021" },
                    { 22, "123477", "ACTIVO", "CASTRO MARISCAL JHONNY", "DOC022", "70000022" },
                    { 23, "123478", "ACTIVO", "CAYOJA LUCANA VICTOR MILTON", "DOC023", "70000023" },
                    { 24, "123479", "ACTIVO", "CHAHIN AVICHACRA JUAN MANUEL", "DOC024", "70000024" },
                    { 25, "123480", "ACTIVO", "CHAU WONG JORGE", "DOC025", "70000025" },
                    { 26, "123481", "ACTIVO", "CLAURE MEDRANO DE OROPEZA ELIZ", "DOC026", "70000026" },
                    { 27, "123482", "ACTIVO", "CONTRERAS VILLEGAS JUAN CARLOS", "DOC027", "70000027" },
                    { 28, "123483", "ACTIVO", "CORTEZ UZEDA JULIO MARTIN", "DOC028", "70000028" },
                    { 29, "123484", "ACTIVO", "DAVALOS SANCHEZ DE MANCILLA Pl", "DOC029", "70000029" },
                    { 30, "123485", "ACTIVO", "DURAN CESPEDES BERTHY RONALD", "DOC030", "70000030" },
                    { 31, "123486", "ACTIVO", "EVELYN VANESA SORIA AVILA", "DOC031", "70000031" },
                    { 32, "123487", "ACTIVO", "FLORES CUELLAD DAVID LUIS", "DOC032", "70000032" },
                    { 33, "123488", "ACTIVO", "FLORES FLORES MARCOS OSCAR", "DOC033", "70000033" },
                    { 34, "123489", "ACTIVO", "FLORES GUZMAN VALENTIN VICTOR", "DOC034", "70000034" },
                    { 35, "123490", "ACTIVO", "GARZON CUELLAR ANGELICA", "DOC035", "70000035" },
                    { 36, "123491", "ACTIVO", "GIANELLA PEREDO EDUARDO", "DOC036", "70000036" },
                    { 37, "123492", "ACTIVO", "GIANELLA PEREDO LUIS ANTONIO", "DOC037", "70000037" },
                    { 38, "123493", "ACTIVO", "GONZALES SANDOVAL JORGE ANTONI", "DOC038", "70000038" },
                    { 39, "123494", "ACTIVO", "GRAGEDA ESCUDERO MARIO WILSON", "DOC039", "70000039" },
                    { 40, "123495", "ACTIVO", "GRIMALDO BRAVO PAUL", "DOC040", "70000040" },
                    { 41, "123496", "ACTIVO", "GUARACHI SOLANO JONATHAN FELIX", "DOC041", "70000041" },
                    { 42, "123497", "ACTIVO", "GUTHRIE PACHECO MIGUEL ANGEL", "DOC042", "70000042" },
                    { 43, "123498", "ACTIVO", "GUTIERREZ BRUNO KATIME ESTHER", "DOC043", "70000043" },
                    { 44, "123499", "ACTIVO", "HINOJOSA SAAVEDRA JOSE SAID", "DOC044", "70000044" },
                    { 45, "123500", "ACTIVO", "JUSTINIANO FLORES CARMEN LILIA", "DOC045", "70000045" },
                    { 46, "123501", "ACTIVO", "JUSTINIANO ROCA RONALD", "DOC046", "70000046" },
                    { 47, "123502", "ACTIVO", "JUSTINIANO VACA JUAN TOMAS", "DOC047", "70000047" },
                    { 48, "123503", "ACTIVO", "LAMAS RODRIGUEZ MARCOS", "DOC048", "70000048" },
                    { 49, "123504", "ACTIVO", "LAZO ARTEAGA CARLOS ROBERTO", "DOC049", "70000049" },
                    { 50, "123505", "ACTIVO", "LAZO QUISPE SEBASTIAN", "DOC050", "70000050" },
                    { 51, "123506", "ACTIVO", "LOBO LIMPIAS VICTOR HUGO", "DOC051", "70000051" },
                    { 52, "123507", "ACTIVO", "LOPEZ NEGRETTY MARY DUNNIA", "DOC052", "70000052" },
                    { 53, "123508", "ACTIVO", "LOPEZ WINNIPEG MARIO MILTON", "DOC053", "70000053" },
                    { 54, "123509", "ACTIVO", "MARTINEZ CANEDO ROLANDO ANTONI", "DOC054", "70000054" },
                    { 55, "123510", "ACTIVO", "MARTINEZ CARDONA SARAH MIRNA", "DOC055", "70000055" },
                    { 56, "123511", "ACTIVO", "MIRANDA CARRASCO CARLOS", "DOC056", "70000056" },
                    { 57, "123512", "ACTIVO", "MOLLO MAMANI ALBERTO", "DOC057", "70000057" },
                    { 58, "123513", "ACTIVO", "MONRROY DIPP VICTOR FERNANDO", "DOC058", "70000058" },
                    { 59, "123514", "ACTIVO", "MORALES MENDEZ MAGALY", "DOC059", "70000059" },
                    { 60, "123515", "ACTIVO", "MORENO SUAREZ ENRIQUE", "DOC060", "70000060" },
                    { 61, "123516", "ACTIVO", "OQUENDO HEREDIA FREDDY MIGUEL", "DOC061", "70000061" },
                    { 62, "123517", "ACTIVO", "ORNOSCO GOMEZ RUBEN", "DOC062", "70000062" },
                    { 63, "123518", "ACTIVO", "ORTIZ ARTEAGA VICTOR HUGO", "DOC063", "70000063" },
                    { 64, "123519", "ACTIVO", "OROPEZA CLAURE GUSTAVO ADOLFO", "DOC064", "70000064" },
                    { 65, "123520", "ACTIVO", "PEINADO PEREIRA JUAN CARLOS", "DOC065", "70000065" },
                    { 66, "123521", "ACTIVO", "PEINADO PEREIRA MIGUEL JESUS", "DOC066", "70000066" },
                    { 67, "123522", "ACTIVO", "PEREZ DELGADILLO SHIRLEY EULAL", "DOC067", "70000067" },
                    { 68, "123523", "ACTIVO", "PEREZ FERREIRA UBALDO", "DOC068", "70000068" },
                    { 69, "123524", "ACTIVO", "PINTO VARGAS EDUARDO", "DOC069", "70000069" },
                    { 70, "123525", "ACTIVO", "POR DESIGNAR", "DOC070", "70000070" },
                    { 71, "123526", "ACTIVO", "RISSIOTTI VELASQUEZ EDGAR ZACA", "DOC071", "70000071" },
                    { 72, "123527", "ACTIVO", "ROCHA ARGOTE FERNANDO", "DOC072", "70000072" },
                    { 73, "123528", "ACTIVO", "ROMAN ROCA RUFINO WILBERTO", "DOC073", "70000073" },
                    { 74, "123529", "ACTIVO", "ROSALES FUENTES JORGE MARCELO", "DOC074", "70000074" },
                    { 75, "123530", "ACTIVO", "SALVATIERRA MERCADO ROLANDO", "DOC075", "70000075" },
                    { 76, "123531", "ACTIVO", "SANCHEZ RIOJA EDWIN ALEJANDRO", "DOC076", "70000076" },
                    { 77, "123532", "ACTIVO", "SANCHEZ VELASCO ENRIQUE", "DOC077", "70000077" },
                    { 78, "123533", "ACTIVO", "SELAYA GARVIZU IVAN VLADISHLAV", "DOC078", "70000078" },
                    { 79, "123534", "ACTIVO", "SEVERICHE TOLEDO SAUL", "DOC079", "70000079" },
                    { 80, "123535", "ACTIVO", "SILES MUÑOZ FELIX", "DOC080", "70000080" },
                    { 81, "123536", "ACTIVO", "SUAREZ CESPEDES MELBY", "DOC081", "70000081" },
                    { 82, "123537", "ACTIVO", "TAPIA FLORES LUIS PERCY", "DOC082", "70000082" },
                    { 83, "123538", "ACTIVO", "TEJERINA GUERRA JULIO", "DOC083", "70000083" },
                    { 84, "123539", "ACTIVO", "TERRAZAS SOTO RICARDO", "DOC084", "70000084" },
                    { 85, "123540", "ACTIVO", "TORREZ CAMACHO LUZ DIANA", "DOC085", "70000085" },
                    { 86, "123541", "ACTIVO", "VACA PINTO CESPEDES ROBERTO CA", "DOC086", "70000086" },
                    { 87, "123542", "ACTIVO", "VALDELOMAR ORELLANA TOMAS", "DOC087", "70000087" },
                    { 88, "123543", "ACTIVO", "VALLET VALLET CORRADO", "DOC088", "70000088" },
                    { 89, "123544", "ACTIVO", "VARGAS CASTILLO CIRO EDGAR", "DOC089", "70000089" },
                    { 90, "123545", "ACTIVO", "VARGAS PEÑA LEONARDO", "DOC090", "70000090" },
                    { 91, "123546", "ACTIVO", "VARGAS YAPURA EDWIN", "DOC091", "70000091" },
                    { 92, "123547", "ACTIVO", "VEIZAGA GONZALES JOSUE OBED", "DOC092", "70000092" },
                    { 93, "123548", "ACTIVO", "VELASCO GUAMAN ANGEL", "DOC093", "70000093" },
                    { 94, "123549", "ACTIVO", "VILLAGOMEZ MELGAR JOSE JUNIOR", "DOC094", "70000094" },
                    { 95, "123550", "ACTIVO", "ZEBALLOS PAREDES DANIEL LUIS", "DOC095", "70000095" },
                    { 96, "123551", "ACTIVO", "ZUNA VILLAGOMEZ JULIO", "DOC096", "70000096" },
                    { 97, "123552", "ACTIVO", "ZUNA VILLAGOMEZ RICARDO", "DOC097", "70000097" },
                    { 98, "123553", "ACTIVO", "ZUÑIGA RUIZ WILMA", "DOC098", "70000098" }
                });

            migrationBuilder.InsertData(
                table: "Horarios",
                columns: new[] { "Id", "Dia", "HoraFin", "HoraInicio" },
                values: new object[,]
                {
                    { 1, "Lunes", new TimeOnly(10, 0, 0), new TimeOnly(8, 0, 0) },
                    { 2, "Martes", new TimeOnly(12, 0, 0), new TimeOnly(10, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "Niveles",
                columns: new[] { "Id", "Nombre", "Numero" },
                values: new object[,]
                {
                    { 1, "1er Semestre", 1 },
                    { 2, "2do Semestre", 2 },
                    { 3, "3er Semestre", 3 },
                    { 4, "4to Semestre", 4 },
                    { 5, "5to Semestre", 5 }
                });

            migrationBuilder.InsertData(
                table: "PeriodosAcademicos",
                columns: new[] { "Id", "FechaFin", "FechaInicio", "Gestion" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 6, 30, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2025-1" },
                    { 2, new DateTime(2025, 12, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2025-2" }
                });

            migrationBuilder.InsertData(
                table: "Estudiantes",
                columns: new[] { "Id", "CarreraId", "Ci", "Direccion", "Email", "Estado", "Nombre", "PasswordHash", "Registro", "Telefono" },
                values: new object[,]
                {
                    { 1, 1, "7894561", null, "carlos@uni.edu", "ACTIVO", "Carlos Sánchez", "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq", "20251234", null },
                    { 2, 2, "222222", null, "ana@uni.edu", "ACTIVO", "Ana Rodríguez", "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq", "EST002", null }
                });

            migrationBuilder.InsertData(
                table: "Materias",
                columns: new[] { "Id", "Codigo", "Creditos", "NivelId", "Nombre" },
                values: new object[,]
                {
                    { 1, "MAT101", 15, 1, "CALCULO I" },
                    { 2, "INF119", 20, 1, "ESTRUCTURAS DISCRETAS" },
                    { 3, "INF110", 20, 1, "INTRODUCCION A LA INFORMATICA" },
                    { 4, "FIS100", 15, 1, "FISICA I" },
                    { 5, "LIN100", 15, 1, "INGLES I" },
                    { 6, "MAT102", 20, 2, "CALCULO II" },
                    { 7, "MAT103", 20, 2, "ALGEBRA LINEAL" },
                    { 8, "INF120", 20, 2, "PROGRAMACION I" },
                    { 9, "FIS102", 20, 2, "FISICA II" },
                    { 10, "LIN101", 20, 2, "INGLES II" },
                    { 11, "MAT207", 20, 3, "ECUACIONES DIFERENCIALES" },
                    { 12, "INF210", 20, 3, "PROGRAMACION II" },
                    { 13, "INF211", 20, 3, "ARQUITECTURA DE COMPUTADORAS" },
                    { 14, "FIS200", 20, 3, "FISICA III" },
                    { 15, "ADM100", 20, 3, "ADMINISTRACION" },
                    { 16, "MAT202", 20, 4, "PROBABILIDAD Y ESTADISTICA I" },
                    { 17, "MAT205", 20, 4, "METODOS NUMERICOS" },
                    { 18, "INF220", 20, 4, "ESTRUCTURA DE DATOS I" },
                    { 19, "INF221", 20, 4, "PROGRAMACION ENSAMBLADOR" },
                    { 20, "ADM200", 20, 4, "CONTABILIDAD" },
                    { 21, "MAT302", 20, 5, "PROBABILIDAD Y ESTADISTICA II" },
                    { 22, "INF318", 20, 5, "PROGRAMACION LOGICA Y FUNCIONAL" },
                    { 23, "INF310", 20, 5, "ESTRUCTURA DE DATOS II" },
                    { 24, "INF312", 20, 5, "BASE DE DATOS I" },
                    { 25, "INF319", 20, 5, "LENGUAJES FORMALES" }
                });

            migrationBuilder.InsertData(
                table: "PlanesEstudio",
                columns: new[] { "Id", "CarreraId", "Codigo", "Estado", "Fecha", "Nombre" },
                values: new object[,]
                {
                    { 1, 1, "SIS25", "ACTIVO", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan 2025" },
                    { 2, 2, "IND24", "ACTIVO", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan 2024" }
                });

            migrationBuilder.InsertData(
                table: "GruposMaterias",
                columns: new[] { "Id", "AulaId", "Cupo", "DocenteId", "Estado", "Grupo", "HorarioId", "MateriaId", "PeriodoId" },
                values: new object[,]
                {
                    { 1, 1, 5, 1, "ACTIVO", "A", 1, 1, 1 },
                    { 2, 2, 5, 2, "ACTIVO", "B", 2, 2, 1 }
                });

            migrationBuilder.InsertData(
                table: "Inscripciones",
                columns: new[] { "Id", "Estado", "EstudianteId", "Fecha", "PeriodoId" },
                values: new object[,]
                {
                    { 1, "PENDIENTE", 1, new DateTime(2025, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, "PENDIENTE", 2, new DateTime(2025, 2, 6, 0, 0, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.InsertData(
                table: "PlanMaterias",
                columns: new[] { "Id", "MateriaId", "PlanId", "Semestre" },
                values: new object[,]
                {
                    { 1, 1, 1, 1 },
                    { 2, 2, 1, 1 },
                    { 3, 3, 1, 2 }
                });

            migrationBuilder.InsertData(
                table: "Prerequisitos",
                columns: new[] { "Id", "MateriaId", "MateriaPrerequisitoId", "NotaMin", "Tipo" },
                values: new object[,]
                {
                    { 1, 6, 1, 51m, "OBLIGATORIO" },
                    { 2, 7, 2, 51m, "OBLIGATORIO" },
                    { 3, 8, 3, 51m, "OBLIGATORIO" },
                    { 4, 9, 4, 51m, "OBLIGATORIO" },
                    { 5, 10, 5, 51m, "OBLIGATORIO" },
                    { 6, 11, 6, 51m, "OBLIGATORIO" },
                    { 7, 12, 7, 51m, "OBLIGATORIO" },
                    { 8, 12, 8, 51m, "OBLIGATORIO" },
                    { 9, 13, 8, 51m, "OBLIGATORIO" },
                    { 10, 13, 9, 51m, "OBLIGATORIO" },
                    { 11, 14, 9, 51m, "OBLIGATORIO" },
                    { 12, 16, 6, 51m, "OBLIGATORIO" },
                    { 13, 17, 11, 51m, "OBLIGATORIO" },
                    { 14, 18, 12, 51m, "OBLIGATORIO" },
                    { 15, 19, 13, 51m, "OBLIGATORIO" },
                    { 16, 20, 15, 51m, "OBLIGATORIO" },
                    { 17, 21, 16, 51m, "OBLIGATORIO" },
                    { 18, 22, 18, 51m, "OBLIGATORIO" },
                    { 19, 23, 18, 51m, "OBLIGATORIO" },
                    { 20, 24, 18, 51m, "OBLIGATORIO" },
                    { 21, 25, 18, 51m, "OBLIGATORIO" }
                });

            migrationBuilder.InsertData(
                table: "DetallesInscripciones",
                columns: new[] { "Id", "Codigo", "Estado", "GrupoMateriaId", "InscripcionId", "NotaFinal" },
                values: new object[,]
                {
                    { 1, "DET001", "INSCRITO", 1, 1, null },
                    { 2, "DET002", "INSCRITO", 2, 2, null }
                });

            migrationBuilder.InsertData(
                table: "HistorialesAcademicos",
                columns: new[] { "Id", "Aprobado", "DetalleInscripcionId", "FechaAprobacion", "Intento", "UltimaNota" },
                values: new object[,]
                {
                    { 1, true, 1, new DateTime(2025, 6, 30, 0, 0, 0, 0, DateTimeKind.Utc), 1, 85m },
                    { 2, false, 2, new DateTime(2025, 6, 30, 0, 0, 0, 0, DateTimeKind.Utc), 1, 70m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aulas_Codigo",
                table: "Aulas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesInscripciones_GrupoMateriaId",
                table: "DetallesInscripciones",
                column: "GrupoMateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesInscripciones_InscripcionId_GrupoMateriaId",
                table: "DetallesInscripciones",
                columns: new[] { "InscripcionId", "GrupoMateriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_CarreraId",
                table: "Estudiantes",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_Registro",
                table: "Estudiantes",
                column: "Registro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_AulaId",
                table: "GruposMaterias",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_DocenteId",
                table: "GruposMaterias",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_HorarioId",
                table: "GruposMaterias",
                column: "HorarioId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_MateriaId",
                table: "GruposMaterias",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposMaterias_PeriodoId",
                table: "GruposMaterias",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesAcademicos_DetalleInscripcionId",
                table: "HistorialesAcademicos",
                column: "DetalleInscripcionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_EstudianteId",
                table: "Inscripciones",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_PeriodoId",
                table: "Inscripciones",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_Materias_Codigo",
                table: "Materias",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materias_NivelId",
                table: "Materias",
                column: "NivelId");

            migrationBuilder.CreateIndex(
                name: "IX_Niveles_Numero",
                table: "Niveles",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosAcademicos_Gestion",
                table: "PeriodosAcademicos",
                column: "Gestion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanesEstudio_CarreraId",
                table: "PlanesEstudio",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesEstudio_Codigo",
                table: "PlanesEstudio",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanMaterias_MateriaId",
                table: "PlanMaterias",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanMaterias_PlanId_MateriaId",
                table: "PlanMaterias",
                columns: new[] { "PlanId", "MateriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prerequisitos_MateriaId_MateriaPrerequisitoId",
                table: "Prerequisitos",
                columns: new[] { "MateriaId", "MateriaPrerequisitoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prerequisitos_MateriaPrerequisitoId",
                table: "Prerequisitos",
                column: "MateriaPrerequisitoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialesAcademicos");

            migrationBuilder.DropTable(
                name: "PlanMaterias");

            migrationBuilder.DropTable(
                name: "Prerequisitos");

            migrationBuilder.DropTable(
                name: "ProcessedMessages");

            migrationBuilder.DropTable(
                name: "Transacciones");

            migrationBuilder.DropTable(
                name: "DetallesInscripciones");

            migrationBuilder.DropTable(
                name: "PlanesEstudio");

            migrationBuilder.DropTable(
                name: "GruposMaterias");

            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropTable(
                name: "Aulas");

            migrationBuilder.DropTable(
                name: "Docentes");

            migrationBuilder.DropTable(
                name: "Horarios");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "Estudiantes");

            migrationBuilder.DropTable(
                name: "PeriodosAcademicos");

            migrationBuilder.DropTable(
                name: "Niveles");

            migrationBuilder.DropTable(
                name: "Carreras");
        }
    }
}
