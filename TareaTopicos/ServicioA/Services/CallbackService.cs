using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;                 // 👈 asegúrate de tener esto
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Contracts;

namespace TAREATOPICOS.ServicioA.Services
{
    public class CallbackService
    {
        private readonly HttpClient _http;
        private readonly ILogger<CallbackService> _logger;

        public CallbackService(HttpClient http, ILogger<CallbackService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<bool> SendAsync(Transaccion tx, string status, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(tx.CallbackUrl)) return true;

            var evt = new CallbackEvent
            {
                TransactionId = tx.Id,
                Status = status,
                Entity = tx.Entidad,
                Operation = tx.TipoOperacion,
                Payload = tx.Payload ?? string.Empty,
                Attempt = tx.Attempt
            };

            var body = JsonSerializer.Serialize(evt);
            using var req = new HttpRequestMessage(HttpMethod.Post, tx.CallbackUrl);
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");

            if (!string.IsNullOrWhiteSpace(tx.CallbackSecret))
            {
                using var h = new HMACSHA256(Encoding.UTF8.GetBytes(tx.CallbackSecret));
                var sig = Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
                req.Headers.Add("X-Signature", $"sha256={sig}");
            }

            if (!string.IsNullOrWhiteSpace(tx.IdempotencyKey))
                req.Headers.TryAddWithoutValidation("Idempotency-Key", tx.IdempotencyKey);

            // El timeout ya lo configuraste en Program.cs al registrar el HttpClient con Polly.

            try
            {
                var resp = await _http.SendAsync(req, ct);
                _logger.LogInformation("Callback → {Url} status {Code}", tx.CallbackUrl, (int)resp.StatusCode);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)                               // 👈 aquí estaba el fallo
            {
                _logger.LogError(ex, "Callback → {Url} lanzó excepción", tx.CallbackUrl);
                return false;
            }
        }
    }
}
