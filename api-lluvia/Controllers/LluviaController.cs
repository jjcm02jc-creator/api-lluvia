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
    }
}