using APIWaveRelease.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace APIWaveRelease.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class EnviarCacheModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;


        public string AdminPassword { get; private set; }

        public EnviarCacheModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult OnGetWaveStatusPartial()
        {
            return ViewComponent("WaveStatus");
        }

        public void OnGet()
        {
            // Lógica para obtener la contraseña del administrador
            var usuariosPermitidos = _configuration.GetSection("UsuariosPermitidos").Get<List<UsuarioConfig>>();
            var adminUser = usuariosPermitidos?.FirstOrDefault(u => u.Usuario == "kn");
            
            // Contraseña Admin Wave
            AdminPassword = _configuration["AdminSettings:Password"] ?? "DefaultAdminPassword";
        }

        // Page Handler para validar credenciales de login
        public IActionResult OnPostValidarUsuario([FromBody] UsuarioModel credenciales)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuariosPermitidos = _configuration.GetSection("UsuariosPermitidos")
                                                  .Get<List<UsuarioConfig>>();

            var usuarioValido = usuariosPermitidos?.FirstOrDefault(u =>
                u.Usuario == credenciales.Usuario &&
                u.Contrasena == credenciales.Contrasena);

            return usuarioValido != null ? new OkResult() : new UnauthorizedResult();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            string mensaje;
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var usuario = _configuration["BasicAuth:Username"];
                var contrasena = _configuration["BasicAuth:Password"];
                var urlApi = _configuration["ApiUrl"];
                var fullUrl = $"{urlApi}/WaveRelease/EnviarCache";

                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);

                // Agregar header de autenticación
                var authValue = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{usuario}:{contrasena}"))
                );
                request.Headers.Authorization = authValue;

                // Depuración: Imprimir la solicitud
                Console.WriteLine($"Enviando solicitud a: {fullUrl}");
                Console.WriteLine($"Authorization Header: {request.Headers.Authorization}");

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                //response back

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, responseContent);
                }

                return Content(responseContent, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error de conexión: {ex.Message}", esError = true });
            }
        }
    }
}