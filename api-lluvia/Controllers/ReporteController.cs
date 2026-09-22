using api_lluvia.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_lluvia.Controllers
{
    // 1. Almacenamiento temporal en memoria de las alertas reportadas por la comunidad
    public static class ReportesStore
    {
        public static List<AlertaLluviaDto> ReportesActivos { get; set; } = new();
    }

    // 2. Definición de la clase del controlador
    [ApiController]
    [Route("api/[controller]")]
    public class ReporteController : ControllerBase
    {
        // 📩 Endpoint HTTP POST para recibir alertas de lluvia
        [HttpPost("reportar-lluvia")]
        public IActionResult ReportarLluvia([FromBody] AlertaLluviaDto reporte)
        {
            reporte.FechaReporte = DateTime.UtcNow;
            ReportesStore.ReportesActivos.Add(reporte);

            // Limpieza automática de reportes de más de 30 minutos
            ReportesStore.ReportesActivos.RemoveAll(r => (DateTime.UtcNow - r.FechaReporte).TotalMinutes > 30);

            return Ok(new { mensaje = "Alerta registrada con éxito." });
        }

        // 📡 Endpoint HTTP GET para que los usuarios lean los reportes activos
        [HttpGet("alertas-comunidad")]
        public IActionResult ObtenerAlertasComunidad()
        {
            ReportesStore.ReportesActivos.RemoveAll(r => (DateTime.UtcNow - r.FechaReporte).TotalMinutes > 30);
            return Ok(ReportesStore.ReportesActivos);
        }
    }
}