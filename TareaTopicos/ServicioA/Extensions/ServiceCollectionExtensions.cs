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
        /// <summary>
        /// Registro central de Redis, colas, workers, opciones y health.
        /// </summary>
        public static IServiceCollection AddServicioAQueues(this IServiceCollection services, IConfiguration cfg)
        {
            // ============================
            // 1) Opciones de configuración (bind desde appsettings.json)
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
            var redisConn = cfg.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

            // ============================
            // 3) Infraestructura de colas
            // ============================
            services.AddSingleton<IBackgroundTaskQueue, RedisTaskQueue>();
            services.AddSingleton<ITransaccionStore, RedisTransaccionStore>();

            // ⚡ QueueManager: debe ser Scoped porque depende de IOptionsSnapshot
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

            // Se registran como IQueueProcessor (inyección polimórfica)
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
