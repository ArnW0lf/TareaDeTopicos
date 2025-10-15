using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Extensions;
using TAREATOPICOS.ServicioA.Services.Seeders;
using TAREATOPICOS.ServicioA.Services.Processors;
using TAREATOPICOS.ServicioA.Services; 
using TAREATOPICOS.ServicioA.Services.Options;

using Polly;
using Polly.Extensions.Http;
using System.Net;






var builder = WebApplication.CreateBuilder(args);
// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173") // puerto de tu Vite
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<ServicioAContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// 1) Define la política (inline, sin método)
var retryPolicy =
    HttpPolicyExtensions
        .HandleTransientHttpError()                 // 5xx, 408 y errores de red
        .OrResult(r => (int)r.StatusCode == 429)    // rate limit
        .WaitAndRetryAsync(
            3,                                      // reintentos
            intento => TimeSpan.FromMilliseconds(200 * Math.Pow(2, intento)) // backoff exponencial
        );

// 2) Registra el HttpClient UNA sola vez, con timeout + Polly
builder.Services
    .AddHttpClient<CallbackService>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddPolicyHandler(retryPolicy);







builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Program.cs
// Program.cs (solo la parte de DI relevante a processors/queues)

// Processors concretos
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.NivelProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.NivelProcessor>();

// Registramos el nuevo procesador de Materias
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.MateriaProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.MateriaProcessor>();

// Registramos el nuevo procesador de Aulas
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.AulaProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.AulaProcessor>();

// Registramos el nuevo procesador de Docentes
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.DocenteProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.DocenteProcessor>();

// Registramos el nuevo procesador de Estudiantes
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.EstudianteProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.EstudianteProcessor>();

// Processor para Inscripcion (maneja POST/async)
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.InscripcionProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.InscripcionProcessor>();

builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.PeriodoAcademicoProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.PeriodoAcademicoProcessor>();    


builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.PlanDeEstudioProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.PlanDeEstudioProcessor>();  

/*
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.GrupoMateriaProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.GrupoMateriaProcessor>();                             
*/
// Router (IQueueProcessor) → DefaultProcessor
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IQueueProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.DefaultProcessor>();

// ... resto de tus servicios
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.IIdempotencyGuard,
                           TAREATOPICOS.ServicioA.Services.IdempotencyGuard>();

// builder.Services.AddHttpClient<CallbackService>();         // para webhooks

// builder.Services.AddHttpClient<CallbackService>(c =>
// {
//     c.Timeout = TimeSpan.FromSeconds(5);
// });
// Processor para DetalleInscripcion (POST/PUT/DELETE)
// Processor para DetalleInscripcion (POST/PUT/DELETE)
// Processor para DetalleInscripcion (POST/PUT/DELETE)
// Processor para DetalleInscripcion (POST/PUT/DELETE)
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.DetalleInscripcionProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.DetalleInscripcionProcessor>();

// Processor para DetalleInscripcion (POST/PUT/DELETE)
// Processor para DetalleInscripcion (POST/PUT/DELETE)
// Processor para DetalleInscripcion (POST/PUT/DELETE)
// Processor para DetalleInscripcion (POST/PUT/DELETE)



builder.Services.AddScoped<QueueManager>();

builder.Services.AddScoped<ITransaccionStore, RedisTransaccionStore>(); // tu store real

// Tu registro de colas/servicios propios
builder.Services.AddServicioAQueues(builder.Configuration);
builder.Services.AddSingleton<QueueStateService>();

// Health checks
builder.Services.AddHealthChecks();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-key";
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


 
// Tu registro de colas/servicios propios
builder.Services.AddServicioAQueues(builder.Configuration);
// WorkerHost: como Singleton e IHostedService
builder.Services.AddSingleton<WorkerHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WorkerHost>());


var app = builder.Build();

 
// 🚀 APLICAR MIGRACIONES AQUÍ (después de Build, antes de Run)
// 👇 MIGRAR + SEED (dentro de Docker también)
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<TAREATOPICOS.ServicioA.Data.ServicioAContext>();

    // Crea tablas si faltan (aplica todas las migraciones)
    await db.Database.MigrateAsync();

    // Lee variable SEED (true/false) para poblar
    var cfg = sp.GetRequiredService<IConfiguration>();
    var doSeed = cfg.GetValue<bool>("SEED");
    if (doSeed)
    {
        // Ejecuta TODOS los seeders registrados en DI
        var seeders = sp.GetServices<ISeeder>(); // tu interfaz
        foreach (var seeder in seeders)
            await seeder.SeedAsync(db);

        await db.SaveChangesAsync();
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();

// === CORS ===
app.UseCors("AllowFrontend");


app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

//app.Run();
app.Run("http://0.0.0.0:5000"); // Escucha en todas las interfaces de red

