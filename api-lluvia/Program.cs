using api_lluvia.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

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