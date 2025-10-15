using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace TesterNiveles;

public class Program
{
    // ===== Config =====
    const string BASE_URL = "http://localhost:5001/api/niveles"; // ajústalo si cambia el puerto
    const string QUEUE = "default";

    const int SEED_POSTS = 50;        // posts iniciales para crear entidades
    const int TOTAL_OPS  = 300;       // operaciones aleatorias

    const double PROB_POST   = 0.60;  // 60% POST
    const double PROB_PUT    = 0.25;  // 25% PUT
    const double PROB_DELETE = 0.15;  // 15% DELETE  (suma = 1.0)

    const bool RANDOM_PRIORITY = true;
    const bool USE_NOT_BEFORE  = false; // si true, difiere +30s
    const int  CONCURRENCY     = 30;    // nivel de paralelismo
    const int  POLL_MS         = 800;

    const int  START_NUMERO    = 5000;  // para no chocar con datos previos

    public record TxResp(string Id, string Estado);
    public record EstadoResp(string Id, string Estado);

    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static async Task Main(string[] args)
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var rnd  = new Random();

        var txIds = new ConcurrentBag<string>();                 // ids de transacciones para el polling
        var existentes = new ConcurrentDictionary<int, bool>();  // numeros creados (para PUT/DELETE)
        int nextNumero = START_NUMERO;

        Console.WriteLine($"Seed: {SEED_POSTS} | Ops aleatorias: {TOTAL_OPS} | Concurrency: {CONCURRENCY}\n");

        // 1) Siembra: POST iniciales
        await RunConcurrent(SEED_POSTS, async _ =>
        {
            int numero = Interlocked.Increment(ref nextNumero);
            var resp = await DoPost(http, numero, rnd);
            if (resp != null) txIds.Add(resp.Id);
            existentes[numero] = true;
        });

        // 2) Operaciones aleatorias (todas por Numero)
        await RunConcurrent(TOTAL_OPS, async _ =>
        {
            double p = rnd.NextDouble();
            if (p < PROB_POST)
            {
                int numero = Interlocked.Increment(ref nextNumero);
                var resp = await DoPost(http, numero, rnd);
                if (resp != null) txIds.Add(resp.Id);
                existentes[numero] = true;
            }
            else if (p < PROB_POST + PROB_PUT)
            {
                if (existentes.IsEmpty) return;
                int pick = PickRandom(existentes.Keys, rnd);
                var respPut = await DoPut(http, pick, rnd);
                if (respPut != null) txIds.Add(respPut.Id);
            }
            else
            {
                if (existentes.IsEmpty) return;
                int pick = PickRandom(existentes.Keys, rnd);
                var respDel = await DoDelete(http, pick, rnd);
                if (respDel != null)
                {
                    txIds.Add(respDel.Id);
                    existentes.TryRemove(pick, out bool _);

                }
            }
        });

        Console.WriteLine($"\n→ Total transacciones encoladas: {txIds.Count} (aprox {SEED_POSTS + TOTAL_OPS})");
        Console.WriteLine("Polling de estados...\n");

        // 3) Polling hasta COMPLETADO/ERROR
        var last = new ConcurrentDictionary<string, string>();
        int vivos;
        do
        {
            vivos = 0;
            await ForEachAsync(txIds, CONCURRENCY, async id =>
            {
                try
                {
                    var resp = await http.GetAsync($"{BASE_URL}/estado/{id}");
                    if (!resp.IsSuccessStatusCode) { Interlocked.Increment(ref vivos); return; }
                    var body = await resp.Content.ReadAsStringAsync();
                    var st = JsonSerializer.Deserialize<EstadoResp>(body, JsonOpts);
                    if (st is null) { Interlocked.Increment(ref vivos); return; }

                    var cur = st.Estado ?? "";
                    var prev = last.GetOrAdd(id, "");
                    if (prev != cur)
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | {id} → {cur}");
                        last[id] = cur;
                    }
                    if (cur != "COMPLETADO" && cur != "ERROR")
                        Interlocked.Increment(ref vivos);
                }
                catch { Interlocked.Increment(ref vivos); }
            });

            if (vivos > 0) await Task.Delay(POLL_MS);
        } while (vivos > 0);

        var comp = last.Count(kv => kv.Value == "COMPLETADO");
        var errs = last.Count(kv => kv.Value == "ERROR");
        Console.WriteLine($"\nResumen → COMPLETADO: {comp} | ERROR: {errs}\n✅ Listo.");
    }

    // ---------- Ops (solo por Numero) ----------
    static async Task<TxResp?> DoPost(HttpClient http, int numero, Random rnd)
    {
        int pr = RANDOM_PRIORITY ? rnd.Next(0, 3) : 1;
        string nb = USE_NOT_BEFORE ? $"&notBeforeUtc={DateTimeOffset.UtcNow.AddSeconds(30):u}".Replace(" ", "%20") : "";
        var url = $"{BASE_URL}/async?queue={QUEUE}&priority={pr}{nb}";
        var payload = new { Numero = numero, Nombre = $"Semestre {numero}" };
        var res = await http.PostAsync(url, Json(payload));
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) Console.WriteLine($"POST {numero} → {(int)res.StatusCode} {res.ReasonPhrase} | {Short(body)}");
        return TryParseTx(body);
    }

    static async Task<TxResp?> DoPut(HttpClient http, int numero, Random rnd)
    {
        int pr = RANDOM_PRIORITY ? rnd.Next(0, 3) : 0;
        var url = $"{BASE_URL}/async/numero/{numero}?queue={QUEUE}&priority={pr}";
        var payload = new { Numero = numero, Nombre = $"Semestre {numero} (upd)" };
        var res = await http.PutAsync(url, Json(payload));
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) Console.WriteLine($"PUT {numero} → {(int)res.StatusCode} {res.ReasonPhrase} | {Short(body)}");
        return TryParseTx(body);
    }

    static async Task<TxResp?> DoDelete(HttpClient http, int numero, Random rnd)
    {
        int pr = RANDOM_PRIORITY ? rnd.Next(0, 3) : 2;
        var url = $"{BASE_URL}/async/numero/{numero}?queue={QUEUE}&priority={pr}";
        var res = await http.DeleteAsync(url);
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) Console.WriteLine($"DEL {numero} → {(int)res.StatusCode} {res.ReasonPhrase} | {Short(body)}");
        return TryParseTx(body);
    }

    // ---------- Helpers ----------
    static StringContent Json(object o) =>
        new StringContent(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    static TxResp? TryParseTx(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try { return JsonSerializer.Deserialize<TxResp>(body, JsonOpts); }
        catch { return null; }
    }

    static string Short(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length <= 160 ? s : s[..160] + "...");

    static int PickRandom(IEnumerable<int> source, Random rnd)
    {
        var list = source as IList<int> ?? source.ToList();
        if (list.Count == 0) return 0;
        return list[rnd.Next(list.Count)];
    }

    static async Task RunConcurrent(int total, Func<int, Task> body)
    {
        var sem = new SemaphoreSlim(CONCURRENCY);
        var tasks = new List<Task>(total);
        for (int i = 0; i < total; i++)
        {
            await sem.WaitAsync();
            int idx = i;
            tasks.Add(Task.Run(async () =>
            {
                try { await body(idx); }
                finally { sem.Release(); }
            }));
        }
        await Task.WhenAll(tasks);
    }

    static async Task ForEachAsync<T>(IEnumerable<T> src, int degree, Func<T, Task> body)
    {
        var sem = new SemaphoreSlim(degree);
        var tasks = new List<Task>();
        foreach (var item in src)
        {
            await sem.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try { await body(item); }
                finally { sem.Release(); }
            }));
        }
        await Task.WhenAll(tasks);
    }
}
