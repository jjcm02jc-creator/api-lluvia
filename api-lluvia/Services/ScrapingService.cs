using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text.Json;

namespace api_lluvia.Services
{


    public static class ScrapingService
    {
       

        public static Dictionary<string, CacheItem> cache = new();

        public class CacheItem
        {
            public DateTime UltimaActualizacion { get; set; }
            public DateTime UltimaConsulta { get; set; }
            public (string v10, string v30, string v60, string vDia) Datos { get; set; }
        }


        public static async Task<(string v10, string v30, string v60, string vDia)> ObtenerAcumuladosAsync(string estacionid)
        {
            // 🔍 1. REVISAR CACHE
            if (cache.ContainsKey(estacionid))
            {
                var item = cache[estacionid];

                // 🔄 actualizar última consulta
                item.UltimaConsulta = DateTime.Now;

                // ⏱ si no han pasado 10 min → devolver cache
                if ((DateTime.Now - item.UltimaActualizacion).TotalMinutes < 10)
                {
                    return item.Datos;
                }
            }

            // 🌐 2. HACER SCRAPING (igual que ya tienes)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                SslProtocols = SslProtocols.Tls12
            };

            using var client = new HttpClient(handler);

            var url = "https://app.sab.gov.co/sab/ServletAcumuladosTiempos";

            var campos = new Dictionary<string, string>
    {
        { "idsensor", estacionid }
    };

            try
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                client.DefaultRequestHeaders.Add("Referer", "https://app.sab.gov.co/sab/SAB.jsp");

                var response = await client.PostAsync(url, new FormUrlEncodedContent(campos));
                var rawResponse = await response.Content.ReadAsStringAsync();

                if (rawResponse.StartsWith("null"))
                {
                    rawResponse = rawResponse.Replace("null", "").Trim();
                }

                using JsonDocument doc = JsonDocument.Parse(rawResponse);
                var acumulado = doc.RootElement.GetProperty("Acumulado");

                int cantidad = acumulado.GetArrayLength();

                string v10 = cantidad > 0 ? ExtraerValor(acumulado, 0) : "0";
                string v30 = cantidad > 1 ? ExtraerValor(acumulado, 1) : "0";
                string v60 = cantidad > 2 ? ExtraerValor(acumulado, 2) : "0";
                string vDia = cantidad > 3 ? ExtraerValor(acumulado, 3) : "0";

                var resultado = (v10, v30, v60, vDia);

                // 💾 3. GUARDAR / ACTUALIZAR CACHE
                cache[estacionid] = new CacheItem
                {
                    Datos = resultado,
                    UltimaActualizacion = DateTime.Now,
                    UltimaConsulta = DateTime.Now
                };

                return resultado;
            }
            catch
            {
                return ("0", "0", "0", "0");
            }
        }

        private static string ExtraerValor(JsonElement lista, int indice)
        {
            try
            {
                var valor = lista[indice].GetProperty("VALORACUMULADO").ToString();
                return string.IsNullOrEmpty(valor) ? "0" : valor;
            }
            catch
            {
                return "0";
            }
        }






    }

}