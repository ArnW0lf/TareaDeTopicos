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
using StackExchange.Redis; // CAMBIO RENDER: Añadido para la conexión con Redis

var builder = WebApplication.CreateBuilder(args);

// === CONFIGURACIÓN CENTRALIZADA ===
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// === CORS ===
// CAMBIO RENDER: Hacemos la política de CORS dinámica.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // En desarrollo, permite el acceso desde tu Vite local
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else // En producción (Render)
        {
            // Lee la URL del frontend desde una variable de entorno
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
            if (!string.IsNullOrEmpty(frontendUrl))
            {
                policy.WithOrigins(frontendUrl)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        }
    });
});


// === CONEXIÓN A LA BASE DE DATOS (POSTGRESQL) ===
// CAMBIO RENDER: Conexión dinámica a PostgreSQL
string dbConnectionString;
if (builder.Environment.IsProduction())
{
    // En Render, usa la variable de entorno DATABASE_URL
    dbConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
}
else
{
    // En local, usa la cadena de appsettings.json
    dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
builder.Services.AddDbContext<ServicioAContext>(options => options.UseNpgsql(dbConnectionString));


// === CONEXIÓN A REDIS ===
// CAMBIO RENDER: Conexión dinámica a Redis
string redisConnectionString;
if (builder.Environment.IsProduction())
{
    // En Render, usa la variable de entorno REDIS_URL
    redisConnectionString = Environment.GetEnvironmentVariable("REDIS_URL");
}
else
{
    // En local, busca la configuración de Redis (ajusta "Redis:ConnectionString" si es diferente en tu appsettings)
    redisConnectionString = builder.Configuration["Redis:ConnectionString"];
}
// Registra la conexión de Redis como un Singleton para que toda la app la reutilice
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));


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

// ... (El resto de tu código no necesita cambios)
// ... (Aquí va todo el registro de tus Processors, Services, Swagger, JWT, etc.)

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

builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IQueueProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.DefaultProcessor>();

builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.IIdempotencyGuard,
                           TAREATOPICOS.ServicioA.Services.IdempotencyGuard>();

builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.DetalleInscripcionProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.DetalleInscripcionProcessor>();

builder.Services.AddScoped<QueueManager>();

builder.Services.AddScoped<ITransaccionStore, RedisTransaccionStore>(); // tu store real

builder.Services.AddServicioAQueues(builder.Configuration);
builder.Services.AddSingleton<QueueStateService>();

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
 
builder.Services.AddSingleton<WorkerHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WorkerHost>());

var app = builder.Build();
 
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<TAREATOPICOS.ServicioA.Data.ServicioAContext>();
    await db.Database.MigrateAsync();

    var cfg = sp.GetRequiredService<IConfiguration>();
    var doSeed = cfg.GetValue<bool>("SEED");
    if (doSeed)
    {
        var seeders = sp.GetServices<ISeeder>();
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

app.UseCors("AllowFrontend");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run("http://0.0.0.0:5000");