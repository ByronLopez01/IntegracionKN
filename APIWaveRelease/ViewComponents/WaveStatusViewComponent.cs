using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIWaveRelease.ViewComponents
{
    public class WaveStatusViewComponent : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public WaveStatusViewComponent(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            bool waveExiste;
            string nombreWave;

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var urlApi = _configuration["ApiUrl"];
                var fullUrl = $"{urlApi}/WaveRelease/ObtenerNombreWaveCache";
                var response = await httpClient.GetAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;

                    waveExiste = root.GetProperty("existe").GetBoolean();
                    nombreWave = root.GetProperty("nombre").GetString();
                }
                else
                {
                    waveExiste = false;
                    nombreWave = "Error al obtener datos";
                }
            }
            catch
            {
                waveExiste = false;
                nombreWave = "Error de conexión";
            }

            // Pasamos los datos a una tupla para la vista del componente
            return View((waveExiste, nombreWave));
        }
    }
}
