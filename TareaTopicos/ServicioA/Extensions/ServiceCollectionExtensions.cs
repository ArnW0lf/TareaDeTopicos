// File: Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Collections.Generic;
using TAREATOPICOS.ServicioA.Options;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Services.Processors;

namespace TAREATOPICOS.ServicioA.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServicioAQueues(this IServiceCollection services, IConfiguration cfg)
        {
            // ============================
            // 1) Opciones de configuración
            // ============================
            services.Configure<RedisOptions>(cfg.GetSection("Redis"));
            services.Configure<RedisQueueOptions>(cfg.GetSection("RedisQueue"));
            services.Configure<QueuesOptions>(opts =>
            {
                opts.Queues = cfg.GetSection("Queues").Get<List<QueueItemOptions>>() ?? new();
            });

            // ============================
            // 2) Redis Connection
            // ============================
            // SE HA ELIMINADO LA CONEXIÓN DUPLICADA DE AQUÍ.
            // La única conexión a Redis se define en Program.cs y se inyecta
            // automáticamente en los servicios que la necesiten.

            // ============================
            // 3) Infraestructura de colas
            // ============================
            services.AddSingleton<IBackgroundTaskQueue, RedisTaskQueue>();
            services.AddSingleton<ITransaccionStore, RedisTransaccionStore>();
            services.AddScoped<QueueManager>();

            // ============================
            // 4) Servicios de soporte
            // ============================
            services.AddSingleton<DeadLetterService>();
            services.AddSingleton<VisibilityReclaimer>();
            services.AddSingleton<RateLimiter>();
            services.AddSingleton<ConfigWatcher>();
            services.AddSingleton<RedisScaleBackplane>();
            services.AddSingleton<QueueStateService>();

            // ============================
            // 5) Processors de negocio
            // ============================
            services.AddScoped<DefaultProcessor>();
            services.AddScoped<NivelProcessor>();
            services.AddScoped<IQueueProcessor, NivelProcessor>();
            services.AddScoped<IQueueProcessor, DefaultProcessor>();

            // ============================
            // 6) WorkerHost (HostedService)
            // ============================
            services.AddHostedService<WorkerHost>();

            return services;
        }
    }
}