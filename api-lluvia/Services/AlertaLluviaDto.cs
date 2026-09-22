namespace api_lluvia.Services
{
    public class AlertaLluviaDto
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public DateTime FechaReporte { get; set; } = DateTime.UtcNow;
    }
}
