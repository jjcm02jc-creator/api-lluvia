using SkiaSharp;

public record PixelPos(int X, int Y);
public record GeoPos(double Latitud, double Longitud);

public class RadarResultDto
{
    public DateTime UltimaActualizacion { get; set; }
    public string ImagenBase64 { get; set; } = string.Empty;
    public List<GeoPos> CoordenadasLluvia { get; set; } = new();
}

public static class RadarStore
{
    public static RadarResultDto? UltimoAnalisis { get; set; }
}

public class RadarWorker : BackgroundService
{
    private readonly HttpClient _httpClient = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));

        await ProcesarRadarAsync();

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await ProcesarRadarAsync();
        }
    }

    private async Task ProcesarRadarAsync()
    {
        try
        {
            string url = "https://bart.ideam.gov.co/ospa/gifs/Radar/Bogota_Basemap.gif";
            byte[] imageBytes = await _httpClient.GetByteArrayAsync(url);

            using var ms = new MemoryStream(imageBytes);
            using var codec = SKCodec.Create(ms);
            if (codec == null) return;

            int ultimoFrameIndex = codec.FrameCount - 1;
            using var originalBitmap = new SKBitmap(codec.Info);
            var options = new SKCodecOptions(ultimoFrameIndex);
            codec.GetPixels(codec.Info, originalBitmap.GetPixels(), options);

            // Guardar imagen codificada en Base64 para que el cliente la muestre
            string base64Image;
            using (var data = originalBitmap.Encode(SKEncodedImageFormat.Png, 100))
            {
                base64Image = Convert.ToBase64String(data.ToArray());
            }

            int anchoOriginal = originalBitmap.Width;
            int altoOriginal = originalBitmap.Height;

            int anchoMini = 60;
            int altoMini = (int)((double)altoOriginal / anchoOriginal * anchoMini);

            using var miniBitmap = originalBitmap.Resize(new SKImageInfo(anchoMini, altoMini), SKFilterQuality.Medium);

            var clustersLluvia = new List<PixelPos>();
            int paso = 2;

            int minX_Mapa = (int)(anchoMini * 0.068);
            int maxX_Mapa = (int)(anchoMini * 0.848);
            int minY_Mapa = (int)(altoMini * 0.055);
            int maxY_Mapa = (int)(altoMini * 0.945);

            var colorAmarillo = (R: 255, G: 255, B: 0);
            var colorVerde = (R: 0, G: 215, B: 80);
            var colorAzul = (R: 0, G: 100, B: 235);

            int tolAmarillo = 55;
            int tolVerde = 45;
            int tolAzul = 55;

            for (int y = minY_Mapa; y < maxY_Mapa; y += paso)
            {
                bool esZonaMinimapaY = y < (altoMini * 0.18);
                bool esZonaLogoY = y > (altoMini * 0.85);

                for (int x = minX_Mapa; x < maxX_Mapa; x += paso)
                {
                    if (esZonaMinimapaY && x > (anchoMini * 0.68)) continue;
                    if (esZonaLogoY && x < (anchoMini * 0.18)) continue;

                    SKColor pixel = miniBitmap.GetPixel(x, y);

                    bool esAmarillo = Math.Abs(pixel.Red - colorAmarillo.R) <= tolAmarillo &&
                                     Math.Abs(pixel.Green - colorAmarillo.G) <= tolAmarillo &&
                                     Math.Abs(pixel.Blue - colorAmarillo.B) <= tolAmarillo;

                    bool esVerde = Math.Abs(pixel.Red - colorVerde.R) <= tolVerde &&
                                   Math.Abs(pixel.Green - colorVerde.G) <= tolVerde &&
                                   Math.Abs(pixel.Blue - colorVerde.B) <= tolVerde;

                    bool esAzul = Math.Abs(pixel.Red - colorAzul.R) <= tolAzul &&
                                 Math.Abs(pixel.Green - colorAzul.G) <= tolAzul &&
                                 Math.Abs(pixel.Blue - colorAzul.B) <= tolAzul;

                    if (esAmarillo || esVerde || esAzul)
                    {
                        int xReal = (int)((double)x / anchoMini * anchoOriginal);
                        int yReal = (int)((double)y / altoMini * altoOriginal);
                        clustersLluvia.Add(new PixelPos(xReal, yReal));
                    }
                }
            }

            var coordenadasGeo = clustersLluvia
                .Select(p => PixelACoordenadasGeoCalibrado(p.X, p.Y, anchoOriginal, altoOriginal))
                .ToList();

            RadarStore.UltimoAnalisis = new RadarResultDto
            {
                UltimaActualizacion = DateTime.UtcNow,
                ImagenBase64 = base64Image,
                CoordenadasLluvia = coordenadasGeo
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando radar: {ex.Message}");
        }
    }

    private GeoPos PixelACoordenadasGeoCalibrado(int pixelX, int pixelY, int anchoOriginal, int altoOriginal)
    {
        double latNorte = 5.01;
        double latSur = 4.33;
        double lngOeste = -74.48;
        double lngEste = -73.74;

        double xMin = anchoOriginal * 0.068;
        double xMax = anchoOriginal * 0.848;
        double yMin = altoOriginal * 0.055;
        double yMax = altoOriginal * 0.945;

        double pctX = Math.Clamp((pixelX - xMin) / (xMax - xMin), 0.0, 1.0);
        double pctY = Math.Clamp((pixelY - yMin) / (yMax - yMin), 0.0, 1.0);

        double longitud = lngOeste + (pctX * (lngEste - lngOeste));
        double latitud = latNorte - (pctY * (latNorte - latSur));

        return new GeoPos(latitud, longitud);
    }
}