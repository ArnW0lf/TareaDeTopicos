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
            // 2) Infraestructura de colas
            // ============================
            services.AddSingleton<IBackgroundTaskQueue, RedisTaskQueue>();
            services.AddSingleton<ITransaccionStore, RedisTransaccionStore>();
            services.AddScoped<QueueManager>();

            // ============================
            // 3) Servicios de soporte
            // ============================
            services.AddSingleton<DeadLetterService>();
            services.AddSingleton<VisibilityReclaimer>();
            services.AddSingleton<RateLimiter>();
            services.AddSingleton<ConfigWatcher>();
            services.AddSingleton<RedisScaleBackplane>();
            services.AddSingleton<QueueStateService>();

            // ============================
            // 4) Processors de negocio
            // ============================
            services.AddScoped<DefaultProcessor>();
            services.AddScoped<NivelProcessor>();

            // REGISTRO ÚNICO de IQueueProcessor
            services.AddScoped<IQueueProcessor, NivelProcessor>();
            services.AddScoped<IQueueProcessor, DefaultProcessor>();

            // ============================
            // 5) WorkerHost (HostedService)
            // ============================
            services.AddHostedService<WorkerHost>();

            return services;
        }
    }
}