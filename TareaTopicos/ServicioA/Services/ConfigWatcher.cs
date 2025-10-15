// File: Services/ConfigWatcher.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Options;

namespace TAREATOPICOS.ServicioA.Services
{
    /// <summary>
    /// Observa cambios en appsettings (section Queues) y los difunde por backplane.
    /// No es HostedService: se engancha con IOptionsMonitor.OnChange.
    /// </summary>
    public class ConfigWatcher
    {
        private readonly ILogger<ConfigWatcher> _logger;
        private readonly RedisScaleBackplane _backplane;
        private readonly IOptionsMonitor<QueuesOptions> _monitor;

        public ConfigWatcher(
            ILogger<ConfigWatcher> logger,
            RedisScaleBackplane backplane,
            IOptionsMonitor<QueuesOptions> monitor)
        {
            _logger = logger;
            _backplane = backplane;
            _monitor = monitor;

            // Suscripción a cambios locales (archivo/ENV con reloadOnChange)
            _monitor.OnChange(async opts =>
            {
                try
                {
                    _logger.LogInformation("ConfigWatcher: cambios detectados en Queues, difundiendo…");
                    await _backplane.BroadcastConfigChanged(opts);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ConfigWatcher: error difundiendo cambios de configuración");
                }
            });

            // Suscripción a eventos remotos del backplane (otras instancias)
            // No esperamos (fire & forget), solo log.
            _ = _backplane.SubscribeAsync(async bm =>
            {
                if (bm.Type == BackplaneEventType.ConfigChanged)
                {
                    try
                    {
                        var opts = JsonSerializer.Deserialize<QueuesOptions>(bm.Payload);
                        _logger.LogInformation("ConfigWatcher: config recibida por backplane (Queues).");
                        // Aquí podrías aplicar la config recibida a tus pools/hilos si expones un método en QueueManager.
                        // p.ej.: await _queueManager.ApplyConfigAsync(opts!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ConfigWatcher: error al interpretar ConfigChanged recibido");
                    }
                }
            });
        }
    }
}
