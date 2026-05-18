using Microsoft.AspNetCore.Mvc;
using api_lluvia.Services;

namespace api_lluvia.Controllers
{
    
    
    
    [ApiController]
    [Route("api/lluvia")]
    public class LluviaController : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var data = await ScrapingService.ObtenerAcumuladosAsync(id);

            return Ok(new
            {
                v10 = data.v10,
                v30 = data.v30,
                v60 = data.v60,
                vDia = data.vDia
            });
        }

        [HttpGet("debug/cache")]
        public IActionResult VerCache()
        {
            var resultado = ScrapingService.cache.Select(x => new
            {
                estacionId = x.Key,
                ultimaActualizacion = x.Value.UltimaActualizacion
                .ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm:ss"),

                ultimaConsulta = x.Value.UltimaConsulta
                .ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm:ss"),
                v10 = x.Value.Datos.v10,
                v30 = x.Value.Datos.v30,
                v60 = x.Value.Datos.v60,
                vDia = x.Value.Datos.vDia
            });

            return Ok(new
            {
                total = ScrapingService.cache.Count,
                estaciones = resultado
            });
        }

        [HttpPost("validar-serial")]
        public IActionResult ValidarSerial([FromBody] SolicitudSerial solicitud)
        {
            // Lee la variable de entorno llamada "SERIAL_VALIDO" configurada en Render
            string serialCorrecto = Environment.GetEnvironmentVariable("SERIAL_VALIDO")
                                    ?? "SERIAL_POR_DEFECTO";

            if (solicitud.Serial == serialCorrecto)
            {
                return Ok(new { Valido = true });
            }

            return Ok(new { Valido = false });
        }
        public class SolicitudSerial
        {
            public string Serial { get; set; }
        }



    }
}