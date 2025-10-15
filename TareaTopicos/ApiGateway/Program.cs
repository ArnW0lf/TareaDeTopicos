using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var client = new HttpClient();

app.MapGet("/materias", async () =>
    await client.GetStringAsync("http://servicioa:5000/materias")
);

app.MapGet("/carreras", async () =>
    await client.GetStringAsync("http://serviciob:5000/carreras")
);

app.MapGet("/malla", async () =>
{
    var materias = await client.GetStringAsync("http://servicioa:5000/materias");
    var carreras = await client.GetStringAsync("http://serviciob:5000/carreras");
    return $"{materias} {carreras}";
});
app.Urls.Add("http://0.0.0.0:5000");

app.Run();
