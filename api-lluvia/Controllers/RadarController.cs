using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RadarController : ControllerBase
{
    [HttpGet("lluvia")]
    public IActionResult ObtenerDatosRadar()
    {
        if (RadarStore.UltimoAnalisis == null)
            return StatusCode(503, "El radar aún se está procesando.");

        return Ok(RadarStore.UltimoAnalisis);
    }
}