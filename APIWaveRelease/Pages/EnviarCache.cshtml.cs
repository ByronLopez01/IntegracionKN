using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace APIFamilyMaster.Pages
{
    public class EnviarCacheModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        //[TempData]
        //public string Mensaje { get; set; }

        public EnviarCacheModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        //public void OnGet() { }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            string mensaje;
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                // Credenciales desde appsettings.json
                var usuario = _configuration["BasicAuth:Username"];
                var contrasena = _configuration["BasicAuth:Password"];
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{usuario}:{contrasena}"))
                );

                var response = await httpClient.PostAsync(
                    "http://apiwaverelease:8080/api/WaveRelease/EnviarCache",
                    null
                );

                mensaje = response.IsSuccessStatusCode
                    ? await response.Content.ReadAsStringAsync()
                    : $"Error HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
            }
            catch (HttpRequestException ex)
            {
                mensaje = $"Error de conexión: {ex.Message}";
            }
            catch (Exception ex)
            {
                mensaje = $"Error inesperado: {ex.Message}";
            }

            return new JsonResult(new { mensaje });
        }
    }
}