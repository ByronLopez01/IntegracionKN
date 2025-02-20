using APIFamilyMaster.data;
using APIFamilyMaster.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;


namespace APIFamilyMaster.controllers
{
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    [Route("api/[controller]")]
    [ApiController]
    public class FamilyMasterController : ControllerBase
    {
        private readonly FamilyMasterContext _context;
        private readonly FamilyMasterService _familyMasterService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public FamilyMasterController(FamilyMasterContext context, FamilyMasterService familyMasterService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _familyMasterService = familyMasterService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

        }
        private void SetAuthorizationHeader(HttpClient client)
        {
            var username = _configuration["BasicAuth:Username"];
            var password = _configuration["BasicAuth:Password"];
            var credentials = $"{username}:{password}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }

        [HttpGet("obtener-total-salidas")]
        public async Task<IActionResult> ObtenerTotalSalidas()
        {
            var totalSalidas = await _familyMasterService.ObtenerTotalSalidasAsync();
            return Ok(new { TotalSalidas = totalSalidas });
        }

        [HttpPost]
        public async Task<IActionResult> PostFamilyMaster([FromBody] List<FamilyMaster> familyMasters)
        {
            if (familyMasters == null || familyMasters.Count == 0)
            {
                return BadRequest("Datos invalidos.");
            }

            var familiasAgrupadas = familyMasters.GroupBy(fm => fm.Familia);
            foreach (var grupo in familiasAgrupadas)
            {
                var tandasDistintas = grupo.Select(fm => fm.NumTanda).Distinct().Count();
                if (tandasDistintas > 1)
                {
                    return BadRequest($"La familia '{grupo.Key}' tiene diferentes numeros de tandas. Esto no esta permitido!");
                }
            }

                try
            {
                var datosExistentes = _context.Familias.ToList();
                if (datosExistentes.Any())
                {
                    _context.Familias.RemoveRange(datosExistentes);
                }

                var familyMasterEntities = new List<FamilyMaster>();

                foreach (var dto in familyMasters)
                {
                    var familyMaster = new FamilyMaster
                    {
                        Familia = dto.Familia,
                        NumSalida = dto.NumSalida,
                        NumTanda = dto.NumTanda,
                        Tienda1 = dto.Tienda1,
                        Tienda2 = dto.Tienda2,
                        Tienda3 = dto.Tienda3,
                        Tienda4 = dto.Tienda4,
                        Tienda5 = dto.Tienda5,
                        Tienda6 = dto.Tienda6,
                        Tienda7 = dto.Tienda7,
                        Tienda8 = dto.Tienda8,
                        Tienda9 = dto.Tienda9,
                        Tienda10 = dto.Tienda10,
                        Tienda11 = dto.Tienda11,
                        Tienda12 = dto.Tienda12,

                    };

                    familyMasterEntities.Add(familyMaster);
                }

                _context.Familias.AddRange(familyMasterEntities);
                await _context.SaveChangesAsync();


                // ENVIO DE JSON A LUCA!!
                var jsonContent = JsonSerializer.Serialize(familyMasters);
                var httpClient = _httpClientFactory.CreateClient("apiLuca");
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                SetAuthorizationHeader(httpClient);

                var urlLucaBase = _configuration["ServiceUrls:luca"];
                var urlLuca = $"{urlLucaBase}/api/sort/FamilyMaster";

                Console.WriteLine($"Enviando JSON a Luca en la URL: {urlLuca}");

                // Mostrar el JSON que se enviará a Luca
                Console.WriteLine(jsonContent);

                
                try
                {

                    var response = await httpClient.PostAsync(urlLuca, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Error al enviar el JSON a Luca.");
                    }
                    else
                    {
                        Console.WriteLine("El JSON fue enviado correctamente a Luca.");
                    }
                    
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error en la solicitud HTTP: {ex.Message}");
                    return StatusCode(500, $"Error en la solicitud HTTP: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");
                    return StatusCode(500, $"Ocurrió un error inesperado: {ex.Message}");
                }
                


                return Ok("Datos guardados correctamente.");

            }
            catch
            {
                return BadRequest("Error al procesar familymaster");
            }
            
        }

        // GET: api/FamilyMaster
        [HttpGet]
        public async Task<IActionResult> GetFamilyMasters([FromQuery] string tienda, [FromQuery] string familia)
        {
            // Validar los parámetros de entrada
            if (string.IsNullOrEmpty(tienda) && string.IsNullOrEmpty(familia))
            {
                return BadRequest("Los parámetros de búsqueda 'tienda' y 'familia' no pueden ser nulos o vacíos.");
            }

            // Realizar la consulta en base a los parámetros proporcionados
            var query = _context.Familias.AsQueryable();

            if (!string.IsNullOrEmpty(tienda))
            {
                query = query.Where(f => f.Tienda1 == tienda ||
                                         f.Tienda2 == tienda ||
                                         f.Tienda3 == tienda ||
                                         f.Tienda4 == tienda ||
                                         f.Tienda5 == tienda ||
                                         f.Tienda6 == tienda ||
                                         f.Tienda7 == tienda ||
                                         f.Tienda8 == tienda ||
                                         f.Tienda9 == tienda ||
                                         f.Tienda10 == tienda ||
                                         f.Tienda11 == tienda ||
                                         f.Tienda12 == tienda);
            }

            if (!string.IsNullOrEmpty(familia))
            {
                query = query.Where(f => f.Familia == familia);
            }

            var familyMasters = await query
                .Select(f => new
                {
                    f.IdFamilyMaster,
                    f.Familia,
                    f.NumSalida,
                    f.NumTanda,
                    TiendaConsultada = tienda,
                    FamiliaConsultada = familia
                })
                .ToListAsync();

            // Verificar si se encontraron resultados
            if (familyMasters == null || !familyMasters.Any())
            {
                return NotFound("No se encontraron datos para la tienda y familia proporcionadas.");
            }

            return Ok(familyMasters);
        }


        // GET: api/FamilyMaster/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFamilyMasterById(int id)
        {
            var familyMaster = await _context.Familias.FindAsync(id);

            if (familyMaster == null)
            {
                return NotFound("No se encontró el dato con el id proporcionado.");
            }

            return Ok(familyMaster);
        }

        [HttpPost("activar-tandas")]
        public async Task<IActionResult> ActivarTandas([FromQuery] int salidasDisponibles)
        {
            if (salidasDisponibles <= 0)
            {
                return BadRequest("El número de salidas disponibles debe ser mayor a cero.");
            }

            // Llamamos al servicio para activar las tandas
            var tandasActivadas = await _familyMasterService.ActivarTandasAsync(salidasDisponibles);

            return Ok(new
            {
                Message = $"{tandasActivadas.Count} tanda(s) activada(s).",
                TandasActivadas = tandasActivadas
            });
        }

        [HttpPost("activarSiguienteTanda")]
        public async Task<IActionResult> ActivarSiguienteTanda([FromQuery] int numTandaActual)
        {
            try
            {
                // Llama al método del servicio
                var tandaActivada = await _familyMasterService.ActivarSiguienteTandaAsync(numTandaActual);

                if (!tandaActivada.NumTanda.HasValue)
                {
                    return Ok(new { message = "No se encontró una tanda siguiente que coincida con las salidas." });
                }

                return Ok(new
                {
                    message = $"Tanda {tandaActivada} activada correctamente.",
                    tandaActivada = tandaActivada.NumTanda
                });
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, new { message = "Error interno del servidor.", error = ex.Message });
            }
        }
    }
}
