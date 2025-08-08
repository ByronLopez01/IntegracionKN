using APIFamilyMaster.data;
using APIFamilyMaster.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;


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
        private readonly ILogger<FamilyMasterController> _logger;

        public FamilyMasterController(
            FamilyMasterContext context,
            FamilyMasterService familyMasterService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<FamilyMasterController> logger)
        {
            _context = context;
            _familyMasterService = familyMasterService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
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
            _logger.LogInformation("Iniciando la obtención del total de salidas.");
            try
            {
                var totalSalidas = await _familyMasterService.ObtenerTotalSalidasAsync();
                _logger.LogInformation("Total de salidas obtenido: {TotalSalidas}", totalSalidas);
                return Ok(new { TotalSalidas = totalSalidas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el total de salidas.");
                return StatusCode(500, "Error interno del servidor al obtener el total de salidas.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostFamilyMaster([FromBody] List<FamilyMaster> familyMasters)
        {
            //_logger.LogInformation("Iniciando POST /FamilyMaster con {Count} registros.", familyMasters?.Count ?? 0);
            if (familyMasters == null || familyMasters.Count == 0)
            {
                _logger.LogError("La lista de FamilyMaster está vacía o es nula.");
                return BadRequest("Error. La lista de FamilyMaster está vacía o es nula.");
            }

            // Aplicar trim a todas las propiedades string
            foreach (var fm in familyMasters)
            {
                fm.Familia = fm.Familia?.Trim();
                fm.Tienda1 = fm.Tienda1?.Trim();
                fm.Tienda2 = fm.Tienda2?.Trim();
                fm.Tienda3 = fm.Tienda3?.Trim();
                fm.Tienda4 = fm.Tienda4?.Trim();
                fm.Tienda5 = fm.Tienda5?.Trim();
                fm.Tienda6 = fm.Tienda6?.Trim();
                fm.Tienda7 = fm.Tienda7?.Trim();
                fm.Tienda8 = fm.Tienda8?.Trim();
                fm.Tienda9 = fm.Tienda9?.Trim();
                fm.Tienda10 = fm.Tienda10?.Trim();
                fm.Tienda11 = fm.Tienda11?.Trim();
                fm.Tienda12 = fm.Tienda12?.Trim();

                // Verificar que después del trim no queden strings vacíos
                if (string.IsNullOrEmpty(fm.Familia))
                {
                    _logger.LogError($"Error. La propiedad 'Familia' quedaría vacía después de eliminar espacios.");
                    return BadRequest($"Error. La propiedad 'Familia' no puede ser una cadena vacía.");
                }

                // Verificar que las tiendas no sean strings vacíos (si no son nulas)
                if (fm.Tienda1 == "") { _logger.LogError($"Error. La tienda1 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda1 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda2 == "") { _logger.LogError($"Error. La tienda2 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda2 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda3 == "") { _logger.LogError($"Error. La tienda3 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda3 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda4 == "") { _logger.LogError($"Error. La tienda4 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda4 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda5 == "") { _logger.LogError($"Error. La tienda5 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda5 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda6 == "") { _logger.LogError($"Error. La tienda6 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda6 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda7 == "") { _logger.LogError($"Error. La tienda7 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda7 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda8 == "") { _logger.LogError($"Error. La tienda8 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda8 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda9 == "") { _logger.LogError($"Error. La tienda9 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda9 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda10 == "") { _logger.LogError($"Error. La tienda10 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda10 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda11 == "") { _logger.LogError($"Error. La tienda11 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda11 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
                if (fm.Tienda12 == "") { _logger.LogError($"Error. La tienda12 de la familia '{fm.Familia}' quedaría vacía."); return BadRequest($"Error. La tienda12 de la familia '{fm.Familia}' no puede ser una cadena vacía."); }
            }

            var familiasAgrupadas = familyMasters.GroupBy(fm => fm.Familia);
            foreach (var grupo in familiasAgrupadas)
            {
                var tandasDistintas = grupo.Select(fm => fm.NumTanda).Distinct().Count();
                if (tandasDistintas > 1)
                {
                    _logger.LogError($"Error. La familia '{grupo.Key}' tiene diferentes numeros de tandas. Esto no esta permitido!");
                    return BadRequest($"Error. La familia '{grupo.Key}' tiene diferentes numeros de tandas. Esto no esta permitido!");
                }
            }

            try
            {
                _logger.LogInformation("Eliminando datos existentes de FamilyMaster.");
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

                //_logger.LogInformation("Agregando {Count} nuevas entidades de FamilyMaster.", familyMasterEntities.Count);
                _context.Familias.AddRange(familyMasterEntities);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Datos de FamilyMaster guardados en la base de datos.");


                // ENVIO DE JSON A LUCA!!
                var jsonContent = JsonSerializer.Serialize(familyMasters);
                var httpClient = _httpClientFactory.CreateClient("apiLuca");
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                SetAuthorizationHeader(httpClient);

                var urlLucaBase = _configuration["ServiceUrls:luca"];
                var urlLuca = $"{urlLucaBase}/api/sort/FamilyMaster";

                _logger.LogInformation("Enviando JSON a Luca en la URL: {UrlLuca}", urlLuca);
                _logger.LogInformation("JSON a enviar: {JsonContent}", jsonContent);

                /*
                try
                {
                    var response = await httpClient.PostAsync(urlLuca, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Error al enviar el JSON a Luca. StatusCode: {StatusCode}", response.StatusCode);
                        return StatusCode((int)response.StatusCode, $"Error al enviar el JSON a Luca. StatusCode: {response.StatusCode}");
                    }
                    else
                    {
                        _logger.LogInformation("El JSON fue enviado correctamente a Luca.");
                    }
                    
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError("Error en la solicitud HTTP: {Message}", ex.Message);
                    return StatusCode(500, $"Error en la solicitud HTTP: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Ocurrió un error inesperado: {Message}", ex.Message);
                    return StatusCode(500, $"Ocurrió un error inesperado: {ex.Message}");
                }
                */

                _logger.LogInformation("Proceso de PostFamilyMaster completado exitosamente.");
                return Ok("Datos guardados correctamente.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la lista de FamilyMaster.");
                return BadRequest("Error al procesar FamilyMaster");
            }
            
        }

        // GET: api/FamilyMaster
        // GET: api/FamilyMaster
        [HttpGet]
        public async Task<IActionResult> GetFamilyMasters([FromQuery] string tienda, [FromQuery] string familia)
        {
            _logger.LogInformation("Iniciando GET /FamilyMaster con Tienda: '{Tienda}' y Familia: '{Familia}'", tienda, familia);
            // Validar los parámetros de entrada
            if (string.IsNullOrEmpty(tienda) && string.IsNullOrEmpty(familia))
            {
                _logger.LogError("Los parámetros de búsqueda 'tienda' y 'familia' no pueden ser nulos o vacíos.");
                return BadRequest("Los parámetros de búsqueda 'tienda' y 'familia' no pueden ser nulos o vacíos.");
            }

            try
            {
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
                    _logger.LogWarning("No se encontraron datos para la tienda '{Tienda}' y familia '{Familia}'.", tienda, familia);
                    return NotFound("No se encontraron datos para la tienda y familia proporcionadas.");
                }

                _logger.LogInformation("Se encontraron {Count} registros para la tienda '{Tienda}' y familia '{Familia}'.", familyMasters.Count, tienda, familia);
                return Ok(familyMasters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar en FamilyMaster con tienda '{Tienda}' y familia '{Familia}'.", tienda, familia);
                return StatusCode(500, "Error interno del servidor al realizar la búsqueda.");
            }
        }


        // GET: api/FamilyMaster/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFamilyMasterById(int id)
        {
            _logger.LogInformation("Iniciando GET /FamilyMaster/{Id}", id);
            try
            {
                var familyMaster = await _context.Familias.FindAsync(id);

                if (familyMaster == null)
                {
                    _logger.LogWarning("No se encontró FamilyMaster con Id: {Id}", id);
                    return NotFound("No se encontró el dato con el id proporcionado.");
                }

                _logger.LogInformation("FamilyMaster con Id: {Id} encontrado.", id);
                return Ok(familyMaster);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener FamilyMaster por Id: {Id}", id);
                return StatusCode(500, "Error interno del servidor al obtener el registro.");
            }
        }

        [HttpPost("activar-tandas")]
        public async Task<IActionResult> ActivarTandas([FromQuery] int salidasDisponibles)
        {
            _logger.LogInformation("Iniciando POST /activar-tandas con salidasDisponibles: {SalidasDisponibles}", salidasDisponibles);
            if (salidasDisponibles <= 0)
            {
                _logger.LogError("Error. El número de salidas disponibles debe ser mayor a cero. Valor recibido: {SalidasDisponibles}", salidasDisponibles);
                return BadRequest("Error. El número de salidas disponibles debe ser mayor a cero.");
            }

            try
            {
                // Llamamos al servicio para activar las tandas
                var tandasActivadas = await _familyMasterService.ActivarTandasAsync(salidasDisponibles);
                _logger.LogInformation("Inicio con {Count} tandas activadas. Tandas: {Tandas}", tandasActivadas.Count, string.Join(",", tandasActivadas));
                return Ok(new
                {
                    Message = $"{tandasActivadas.Count} tandas activadas.",
                    TandasActivadas = tandasActivadas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el proceso de activar tandas con salidas disponibles: {SalidasDisponibles}", salidasDisponibles);
                return StatusCode(500, "Error interno del servidor al activar las tandas.");
            }
        }

        [HttpPost("activarSiguienteTanda")]
        public async Task<IActionResult> ActivarSiguienteTanda([FromQuery] int numTandaActual)
        {
            _logger.LogInformation("Iniciando POST /activarSiguienteTanda con numTandaActual: {NumTandaActual}", numTandaActual);
            try
            {
                // Llama al método del servicio
                var tandaActivada = await _familyMasterService.ActivarSiguienteTandaAsync(numTandaActual);

                if (!tandaActivada.NumTanda.HasValue)
                {
                    _logger.LogWarning("No se encontró una tanda siguiente o ya fue activada para la tanda actual: {NumTandaActual}", numTandaActual);
                    return Ok(new { message = "No se encontró una tanda siguiente o ya fue activada: " + numTandaActual });
                }

                // Obtener la familia de la tanda anterior para un log más detallado
                var familiaAnterior = await _context.Familias
                    .Where(f => f.NumTanda == numTandaActual)
                    .Select(f => f.Familia)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("NORMAL. Tanda activada: {TandaActivada} (Familia: {FamiliaActivada}). Tanda anterior: {NumTandaAnterior} (Familia: {FamiliaAnterior}).",
                    tandaActivada.NumTanda, tandaActivada.Familia, numTandaActual, familiaAnterior);

                return Ok(new
                {
                    message = $"Tanda {tandaActivada.NumTanda} activada correctamente.",
                    tandaActivada = tandaActivada.NumTanda
                });
            }
            catch (Exception ex)
            {
                // Manejo de errores
                _logger.LogError(ex, "Error al activar la siguiente tanda para la tanda actual: {NumTandaActual}", numTandaActual);
                return StatusCode(500, new { message = "Error al activar siguiente tanda.", error = ex.Message });
            }
        }


        [HttpPost("activarSiguienteTandaFamily")]
        public async Task<IActionResult> ActivarSiguienteTandaFamilyConfirm([FromQuery] int numTandaActual)
        {
            _logger.LogInformation("Iniciando POST /activarSiguienteTandaFamily con numTandaActual: {NumTandaActual}", numTandaActual);
            try
            {
                // Llama al método del servicio, que ahora devuelve una tupla con el mensaje de estado.
                var (tandaActivadaNum, familiaActivada, message) = await _familyMasterService.ActivarSiguienteTandaAsyncFamilyConfirm(numTandaActual);

                // Si no se activó una nueva tanda, registra el mensaje específico del servicio y devuélvelo.
                if (!tandaActivadaNum.HasValue)
                {
                    _logger.LogWarning("No se pudo activar la siguiente tanda para la Tanda actual {NumTandaActual}. Motivo: {Message}", numTandaActual, message);
                    return Ok(new { message });
                }

                // Obtener la familia de la tanda anterior para un log más detallado
                var familiaAnterior = await _context.Familias
                    .Where(f => f.NumTanda == numTandaActual)
                    .Select(f => f.Familia)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("FAMILY. Tanda activada: {TandaActivada} (Familia: {FamiliaActivada}). Tanda anterior: {NumTandaAnterior} (Familia: {FamiliaAnterior}).",
                    tandaActivadaNum, familiaActivada, numTandaActual, familiaAnterior);

                return Ok(new
                {
                    message = $"Tanda {tandaActivadaNum} activada correctamente.",
                    tandaActivada = tandaActivadaNum
                });
            }
            catch (Exception ex)
            {
                // Manejo de errores
                _logger.LogError(ex, "Error al activar la siguiente tanda (FamilyConfirm) para la tanda actual: {NumTandaActual}", numTandaActual);
                return StatusCode(500, new { message = "Error interno del servidor.", error = ex.Message });
            }
        }


        // Nuevo endpoint GET para obtener todos los registros de FamilyMaster
        [HttpGet("all")]
        public async Task<IActionResult> GetAllFamilyMasters()
        {
            _logger.LogInformation("Iniciando GET /all para obtener todos los registros de FamilyMaster.");
            try
            {
                // Obtener todos los registros de la tabla FamilyMaster
                var familyMasters = await _context.Familias.ToListAsync();

                // Verificar si hay datos
                if (familyMasters == null || familyMasters.Count == 0)
                {
                    _logger.LogWarning("No se encontraron registros en la tabla FamilyMaster.");
                    return NotFound("Error. No se encontraron registros en la tabla FamilyMaster.");
                }

                _logger.LogInformation("Se obtuvieron {Count} registros de FamilyMaster.", familyMasters.Count);
                // Devolver los datos en formato JSON
                return Ok(familyMasters);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                _logger.LogError(ex, "Error al obtener todos los registros de FamilyMaster.");
                return StatusCode(500, new { message = "Error al obtener los registros de FamilyMaster.", error = ex.Message });
            }
        }

    }
}
