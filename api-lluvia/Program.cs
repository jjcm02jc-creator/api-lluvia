using api_lluvia.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHostedService<RadarWorker>();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();



const string API_KEY = "qweasd";

var timer = new Timer(_ =>
{
    var ahora = DateTime.Now;

    var clavesAEliminar = ScrapingService.cache
        .Where(x => (ahora - x.Value.UltimaConsulta).TotalMinutes > 10)
        .Select(x => x.Key)
        .ToList();

    foreach (var key in clavesAEliminar)
    {
        ScrapingService.cache.Remove(key);
    }

}, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));



app.Run();