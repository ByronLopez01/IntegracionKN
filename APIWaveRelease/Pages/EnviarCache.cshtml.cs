//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using System.Net.Http.Headers;
//using System.Text;
//using Microsoft.Extensions.Configuration;
//using Microsoft.AspNetCore.Authorization;

//namespace APIFamilyMaster.Pages
//{
//    [AllowAnonymous]
//    public class EnviarCacheModel : PageModel
//    {
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly IConfiguration _configuration;

//        //[TempData]
//        //public string Mensaje { get; set; }

//        public EnviarCacheModel(
//            IHttpClientFactory httpClientFactory,
//            IConfiguration configuration)
//        {
//            _httpClientFactory = httpClientFactory;
//            _configuration = configuration;
//        }

//        public void OnGet() { }

//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> OnPostAsync()
//        {
//            string mensaje;
//            try
//            {
//                var httpClient = _httpClientFactory.CreateClient();

//                var usuario = _configuration["BasicAuth:Username"];
//                var contrasena = _configuration["BasicAuth:Password"];
//                var urlApi = _configuration["ApiUrl"];
//                var fullUrl = $"{urlApi}/WaveRelease/EnviarCache";

//                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);

//                // Agregar header de autenticación
//                var authValue = new AuthenticationHeaderValue(
//                    "Basic",
//                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{usuario}:{contrasena}"))
//                );
//                request.Headers.Authorization = authValue;

//                // Depuración: Imprimir la solicitud
//                Console.WriteLine($"Enviando solicitud a: {fullUrl}");
//                Console.WriteLine($"Authorization Header: {request.Headers.Authorization}");

//                var response = await httpClient.SendAsync(request);

//                mensaje = response.IsSuccessStatusCode
//                    ? await response.Content.ReadAsStringAsync()
//                    : $"Error HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
//            }
//            catch (HttpRequestException ex)
//            {
//                mensaje = $"Error de conexión: {ex.Message}";
//            }
//            catch (Exception ex)
//            {
//                mensaje = $"Error inesperado: {ex.Message}";
//            }

//            return new JsonResult(new { mensaje });
//        }
//    }
//}


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace APIFamilyMaster.Pages
{
    [AllowAnonymous]
    public class EnviarCacheModel : PageModel
    {
        public void OnGet()
        {
            // Este método se ejecuta cuando cargas la página por primera vez.
            // No necesitas cambiar nada aquí.
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostAsync()
        {
            // Este método se ejecuta cuando envías el formulario.
            // Solo devuelve un mensaje en JSON y deja que el JavaScript maneje la solicitud real.
            return new JsonResult(new { mensaje = "Solicitud enviada desde el frontend." });
        }
    }
}