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

        [TempData]
        public string Mensaje { get; set; }

        public EnviarCacheModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public void OnGet() { }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                // Credenciales desde appsettings.json
                var usuario = _configuration["BasicAuth:Username"];
                var contrasena = _configuration["BasicAuth:Password"];

                var authHeader = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{usuario}:{contrasena}"))
                );

                httpClient.DefaultRequestHeaders.Authorization = authHeader;

                var response = await httpClient.PostAsync(
                    "http://apiwaverelease:8080/api/WaveRelease/EnviarCache",
                    null
                );

                if (response.IsSuccessStatusCode)
                {
                    Mensaje = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Mensaje = $"Error HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (HttpRequestException ex)
            {
                Mensaje = $"Error de conexión: {ex.Message}";
            }
            catch (Exception ex)
            {
                Mensaje = $"Error inesperado: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}