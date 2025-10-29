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
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// === CONFIGURACIÓN CENTRALIZADA ===
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
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
// CAMBIO RENDER: Conexión robusta que convierte la URL de Render al formato correcto.
string dbConnectionString;
if (builder.Environment.IsProduction())
{
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrEmpty(dbUrl))
    {
        throw new InvalidOperationException("La variable de entorno DATABASE_URL no está configurada.");
    }

    var databaseUri = new Uri(dbUrl);
    var userInfo = databaseUri.UserInfo.Split(':');

    var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        // 💡 CAMBIO AQUÍ: Si el puerto no está en la URL, usa el default 5432.
        Port = databaseUri.Port > 0 ? databaseUri.Port : 5432,
        Username = userInfo[0],
        Password = userInfo[1],
        Database = databaseUri.LocalPath.TrimStart('/'),
        SslMode = Npgsql.SslMode.Prefer, // Render requiere SSL
        TrustServerCertificate = true   // Necesario para conexiones en la nube
    };
    dbConnectionString = connectionStringBuilder.ToString();
}
else
{
    // En local, usa la cadena de appsettings.json
    dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
builder.Services.AddDbContext<ServicioAContext>(options => options.UseNpgsql(dbConnectionString));


// === CONEXIÓN A REDIS ===
string redisConnectionString;
if (builder.Environment.IsProduction())
{
    redisConnectionString = Environment.GetEnvironmentVariable("REDIS_URL");
    if (string.IsNullOrEmpty(redisConnectionString))
    {
        throw new InvalidOperationException("La variable de entorno REDIS_URL no está configurada.");
    }
}
else
{
    redisConnectionString = builder.Configuration["Redis:ConnectionString"];
}

var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
redisConfiguration.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfiguration));


// ... (El resto de tu código no necesita cambios, se mantiene igual)

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
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.NivelProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.NivelProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.MateriaProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.MateriaProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.AulaProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.AulaProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.DocenteProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.DocenteProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.EstudianteProcessor>();
builder.Services.AddScoped<TAREATOPICOS.ServicioA.Services.Processors.IProcessor,
                           TAREATOPICOS.ServicioA.Services.Processors.EstudianteProcessor>();
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
builder.Services.AddScoped<ITransaccionStore, RedisTransaccionStore>();
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