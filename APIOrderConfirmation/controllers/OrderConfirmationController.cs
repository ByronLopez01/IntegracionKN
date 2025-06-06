using System.Text;
using System.Net.Http.Headers;
using APIOrderConfirmation.data;
using APIOrderConfirmation.models;
using APIOrderConfirmation.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace APIOrderConfirmation.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public class OrderConfirmationController : ControllerBase
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _apiWaveReleaseClient;
        private readonly IOrderConfirmationService _orderConfirmationService;
        private readonly OrderConfirmationContext _context;
        private readonly IConfiguration _configuration;
        //Variable secuencial para generar el subnum
        //private static int _numSubNum = 1;
        private readonly ILogger<OrderConfirmationController> _logger;

        public OrderConfirmationController(
            IOrderConfirmationService orderConfirmationService,
            OrderConfirmationContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            HttpClient httpClient,
            ILogger<OrderConfirmationController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiWaveReleaseClient = httpClientFactory.CreateClient("apiWaveRelease");
            _orderConfirmationService = orderConfirmationService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        private static List<int> _activatedTandas = new();

        private async Task<HttpResponseMessage> activarSiguienteTandaAsync(int numTandaActual)
        {
            var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster/activarSiguienteTanda?numTandaActual={numTandaActual}";
            _logger.LogInformation("URL FamilyMaster: " + urlFamilyMaster);
            // Llamamos con un POST el endpoint de FamilyMaster para activar la siguiente tanda
            var familyMasterResponse = await _apiWaveReleaseClient.PostAsync(urlFamilyMaster, null);
            _logger.LogInformation($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
            return(familyMasterResponse);
        }


        private async Task<HttpResponseMessage> activarSiguienteTandaAsyncFamily(int numTandaActual)
        {
            var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster/activarSiguienteTandaFamily?numTandaActual={numTandaActual}";
            Console.WriteLine("URL FamilyMaster: " + urlFamilyMaster);
            // Llamamos con un POST el endpoint de FamilyMaster para activar la siguiente tanda
            var familyMasterResponse = await _apiWaveReleaseClient.PostAsync(urlFamilyMaster, null);
            Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
            return (familyMasterResponse);
        }


        private async Task<HttpResponseMessage> desactivarWaveAsync(String numOrden, String codProducto)
        {
            var DesactivarWave = "http://apiwaverelease:8080/api/WaveRelease/DesactivarWave";
            var waveURL = $"{DesactivarWave}/{numOrden}/{codProducto}";
            _logger.LogInformation("URL: " + waveURL);
            var desactivarWaveResponse = await _apiWaveReleaseClient.PostAsync(waveURL, null);
            return (desactivarWaveResponse);
        }

        // Método para activar la tanda solo si aún no se activó en la Wave actual.
        private async Task<HttpResponseMessage> activarTandaSinActivadaAsync(int numTandaActual)
        {
            if (!_activatedTandas.Contains(numTandaActual))
            {
                _activatedTandas.Add(numTandaActual);
                return await activarSiguienteTandaAsync(numTandaActual);
            }
            else
            {
                _logger.LogInformation("La tanda {NumTanda} ya fue activada en esta Wave.", numTandaActual);
                // Devuelve un OK simulado.
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }
        }

        // Método para desactivar todas las waves de una familia específica
        private async Task DesactivarWavesDeFamiliaAsync(string familia)
        {
            _logger.LogInformation("Desactivando waves de la familia: {Familia}", familia);

            // Obtener todas las waves activas para esta familia
            var wavesFamilia = await _context.WaveRelease
                .Where(w => w.familia == familia && w.estadoWave == true)
                .ToListAsync();

            if (wavesFamilia.Any())
            {
                _logger.LogInformation("Se encontraron {Count} waves activas para desactivar", wavesFamilia.Count);

                // Marcar todas como inactivas
                foreach (var wave in wavesFamilia)
                {
                    wave.estadoWave = false;
                }

                // Actualizar en la base de datos
                _context.WaveRelease.UpdateRange(wavesFamilia);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Waves de la familia {Familia} desactivadas correctamente", familia);
            }
            else
            {
                _logger.LogInformation("No se encontraron waves activas de la familia {Familia}", familia);
            }
        }

        private void LogJsonToFile(string jsonContent, string endpoint)
        {
            var logFilePath = "/app/logs/confirm_log.txt"; // Ruta del archivo de log en el contenedor Docker
            var logEntry = $"{DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy HH:mm:ss")}: {endpoint} - {jsonContent}{Environment.NewLine}";

            try
            {
                using (var writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error. Fallo al escribir en el archivo de log: {ex.Message}");
            }
        }
        private void SetAuthorizationHeader(HttpClient client)
        {
            var username = _configuration["BasicAuth:Username"];
            var password = _configuration["BasicAuth:Password"];
            var credentials = $"{username}:{password}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }

        // Endpoint para resetear la lista de tandas activadas
        [HttpPost("ResetTandas")]
        public IActionResult ResetTandas()
        {
            _logger.LogInformation("Tandas activadas actuales: {ActivatedTandas}", _activatedTandas);
            _activatedTandas.Clear();
            _logger.LogInformation("Reset de tandas activadas.");
            return Ok(_activatedTandas);
        }

        /*
        [HttpPost("")]
        public async Task<IActionResult> ProcessOrders()
        {
            var (success, detalles) = await _orderConfirmationService.ProcesoOrdersAsync();

            if (success)
            {
                return Ok(new { message = "Órdenes procesadas correctamente.", detalles });
            }
            else
            {
                return BadRequest(new { message = "Error al procesar las órdenes.", detalles });
            }
        }

        
        [HttpGet]
        public IActionResult GetSortComplete()
        {
            // dar formato 00000000 
            string subnumcompleto = _numSubNum.ToString("D9");

            var response = new
            {
                SORT_COMPLETE = new
                {
                    wcs_id = "WCS_ID",
                    wh_id = "CLPUD01", //fijo 
                    msg_id = "MSG_ID",
                    trandt = "YYYYMMDDHHMISS",
                    SORT_COMP_SEG = new
                    {
                        LOAD_HDR_SEG = new
                        {
                            LODNUM = "SRCLOD",
                            LOAD_DTL_SEG = new[]
                            {
                                new
                                {
                                    subnum = subnumcompleto,
                                    dtlnum = "DTLNUM",
                                    stoloc = "DSTLOC",
                                    qty = 10
                                }
                            }
                        }
                    }
                }
            };
            _numSubNum++;

            return Ok(response);
        }
        */

        // PROCESADO NUEVO!!!
        [HttpPost("Procesado")]
        public async Task<IActionResult> ProcesadoTest([FromBody] SortCompleteKN request)
        {
            _logger.LogInformation("---------- INICIO PROCESADO ----------");
            var jsonLog = JsonConvert.SerializeObject(request);
            LogJsonToFile(jsonLog, "Procesado");

            if (request?.SORT_COMPLETE?.SORT_COMP_SEG?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null ||
                request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                _logger.LogInformation("Error. Datos en formato incorrecto.");
                return BadRequest("Error. Datos en formato incorrecto.");
            }

            // Obtener la wave activa actual (fuera del bucle)
            var waveActivaActual = await _context.WaveRelease
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.estadoWave == true);

            if (waveActivaActual == null)
            {
                _logger.LogInformation("Error. No se encontró la wave activa en WaveRelease.");
                return NotFound("Error. No se encontró la wave activa en WaveRelease.");
            }

            // Lista para almacenar las familias procesadas
            var familiasProcesadas = new HashSet<string>();

            try
            {
                // Procesar cada orden individualmente
                foreach (var loadDtl in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                {
                    var dtlnum = loadDtl.dtlnum;

                    // Buscar la orden según su dtlnum según la wave activa
                    var orden = await _context.ordenesEnProceso
                        .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum && o.wave == waveActivaActual.Wave);


                    if (orden == null)
                    {
                        _logger.LogInformation("No se encontró la orden para dtlnum: {Dtlnum}", dtlnum);
                        continue; // Si no se encuentra la orden, continuar con la siguiente
                    }

                    // Lógica de órdenes completadas y no completadas
                    if (orden.cantidadProcesada == orden.cantidadLPN)
                    {
                        _logger.LogInformation("PROCESANDO CON SORTER!!!");
                        orden.estadoLuca = false; // Marcar como completada (KN)
                    }
                    else
                    {
                        _logger.LogInformation("PROCESANDO CON SORTER!!!");
                        orden.estadoLuca = false; // Marcar como completada (KN)
                        orden.estado = false; // Marcar como completada (Senad)
                        orden.fechaProceso = DateTime.UtcNow.AddHours(-2); // Actualizar fecha de proceso
                    }

                    /*
                    // Desactivar la wave de la orden
                    try
                    {
                        var numOrden = orden.numOrden;
                        var codProducto = orden.codProducto;
                        SetAuthorizationHeader(_apiWaveReleaseClient);
                        var response = await desactivarWaveAsync(numOrden, codProducto);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Orden {NumOrden} ya desactivada!. StatusCode: {StatusCode}", numOrden, response.StatusCode);
                        }
                        else
                        {
                            _logger.LogInformation("Wave de la orden {NumOrden} desactivada correctamente.", numOrden);
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _logger.LogError("Ocurrió un error HTTP al desactivar la wave de la orden {NumOrden}: {Message}", orden.numOrden, httpEx.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Ocurrió un error al desactivar la wave de la orden {NumOrden}: {Message}", orden.numOrden, ex.Message);
                    }
                    */

                    // Guardar la familia de la orden para su posterior verificación
                    familiasProcesadas.Add(orden.familia);
                }

                // Guardar los cambios en la base de datos después de procesar todas las órdenes
                await _context.SaveChangesAsync();

                // Verificar si todas las órdenes de las familias procesadas están completadas
                foreach (var familia in familiasProcesadas)
                {
                    // Obtener todas las órdenes de la familia en la wave actual
                    var ordenesFamilia = await _context.ordenesEnProceso
                        .Where(o => o.familia == familia && o.wave == waveActivaActual.Wave)
                        .ToListAsync();

                    // Verificar si todas las órdenes de la familia están completadas
                    bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);
                    _logger.LogInformation("PROCESADO - Todas las órdenes completadas de la familia {Familia}: {Completadas}", familia, todasOrdenesCompletadas); 

                    if (todasOrdenesCompletadas)
                    {
                        // Obtener la tanda actual de FamilyMaster
                        var FamilyMaster = await _context.FamilyMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Familia == familia);

                        if (FamilyMaster == null)
                        {
                            _logger.LogInformation("No se encontró la familia en FamilyMaster.");
                            continue;
                        }

                        var numTandaActual = FamilyMaster.NumTanda;

                        _logger.LogInformation("Wave actual: {Wave}", waveActivaActual.Wave);
                        _logger.LogInformation("Tanda actual: {NumTanda}", numTandaActual);



                        // Desactivar todas las waves de esta familia antes de activar la siguiente tanda
                        _logger.LogInformation("Procesado - Inicio func Desactivar waves de la familia: {Familia}", familia);
                        await DesactivarWavesDeFamiliaAsync(familia);


                        SetAuthorizationHeader(_apiWaveReleaseClient);

                        try
                        {
                            // Activar la siguiente tanda
                            var response = await activarTandaSinActivadaAsync(numTandaActual);
                            _logger.LogInformation("ACTIVACIÓN DE SIGUIENTE TANDA!!!!!");

                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("Error. Fallo al activar la siguiente tanda en FamilyMaster. StatusCode: {StatusCode}", response.StatusCode);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError("Error HTTP al activar la siguiente tanda en FamilyMaster: {Message}", ex.Message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                        }
                    }
                }

                // Agrupamos por dtlnum y tomamos la primera ocurrencia de cada grupo
                var uniqueDetails = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG
                    .GroupBy(d => d.dtlnum)
                    .Select(g => g.First())
                    .ToList();

                // Guardar registros de confirmación en la BD
                foreach (var detail in uniqueDetails)
                {
                    var nuevaConfirmada = new Confirmada
                    {
                        WcsId = request.SORT_COMPLETE.wcs_id,
                        WhId = request.SORT_COMPLETE.wh_id,
                        MsgId = request.SORT_COMPLETE.msg_id,
                        TranDt = DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy HH:mm:ss"),
                        LodNum = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                        SubNum = detail.subnum,
                        DtlNum = detail.dtlnum,
                        StoLoc = detail.stoloc,
                        Qty = detail.qty,
                        accion = "Procesado"
                    };

                    await _context.Confirmada.AddAsync(nuevaConfirmada);
                }

                // Guardar cambios en la BD
                await _context.SaveChangesAsync();

                // Filtrar los detalles con qty > 0 antes de enviar a KN
                var detallesFiltrados = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG
                    .Where(d => d.qty > 0)
                    .ToList();

                var requestFiltrado = new SortCompleteKN
                {
                    SORT_COMPLETE = new SortComplete
                    {
                        wcs_id = request.SORT_COMPLETE.wcs_id,
                        wh_id = request.SORT_COMPLETE.wh_id,
                        msg_id = request.SORT_COMPLETE.msg_id,
                        trandt = request.SORT_COMPLETE.trandt,
                        SORT_COMP_SEG = new SortCompSeg
                        {
                            LOAD_HDR_SEG = new LoadHdrSeg
                            {
                                LODNUM = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                                LOAD_DTL_SEG = detallesFiltrados
                            }
                        }
                    }
                };

                // Si no quedan detalles en la lista, no enviar a KN
                if (requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
                {
                    _logger.LogInformation("No hay detalles (qty > 0) para enviar a KN.");
                    return Ok("No hay detalles para enviar a KN.");
                }

                var jsonFiltrado = JsonConvert.SerializeObject(requestFiltrado);
                _logger.LogInformation("Procesado - JSON FILTRADO: {JsonFiltrado}", jsonFiltrado);

                // Enviar datos a KN
                try
                {
                    var urlKN = _configuration["ExternalService:UrlKN"];
                    _logger.LogInformation("URL KN:" + urlKN);

                    using (var client = new HttpClient())
                    {
                        // Basic Auth
                        var username = _configuration["BasicAuth:Username"];
                        var password = _configuration["BasicAuth:Password"];

                        var array = Encoding.ASCII.GetBytes($"{username}:{password}");
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(array));

                        // Serializar el JSON
                        var jsonContent = JsonConvert.SerializeObject(requestFiltrado);
                        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        // POST
                        var response = await client.PostAsync(urlKN, httpContent);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Datos enviados correctamente a KN.");
                            return Ok("Datos enviados correctamente a KN.");
                        }
                        else
                        {
                            _logger.LogError("Error. Fallo al enviar confirmación a KN.");
                            return StatusCode((int)response.StatusCode, "Error. Fallo al enviar confirmación a KN.");
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError($"Ocurrió un error HTTP al enviar los datos a KN: Message: {httpEx.Message}, InnerException: {httpEx.InnerException}");
                    return StatusCode(500, $"Ocurrió un error HTTP al enviar los datos a KN: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Ocurrió un error al enviar los datos a KN: Message: {ex.Message}, InnerException: {ex.InnerException}");
                    return StatusCode(500, $"Ocurrió un error al enviar los datos a KN: {ex.Message}");
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ocurrió un error al procesar las órdenes: Message: {ex.Message}, InnerException: {ex.InnerException}");
                return StatusCode(500, $"Ocurrió un error al procesar las órdenes: {ex.Message}");
            }
        }




        // SHORT PICK NUEVO!!!
        [HttpPost("Short")]
        public async Task<IActionResult> shortPickTest([FromBody] SortCompleteKN request)
        {
            _logger.LogError("---------- INICIO SHORT PICK ----------");

            var jsonLog = JsonConvert.SerializeObject(request);
            LogJsonToFile(jsonLog, "Short");

            if (request?.SORT_COMPLETE?.SORT_COMP_SEG?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null ||
                request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                _logger.LogError("Error. Datos en formato incorrecto.");
                return BadRequest("Error. Datos en formato incorrecto.");
            }

            try
            {
                // Obtener la wave activa actual (fuera del bucle)
                var waveActivaActual = await _context.WaveRelease
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.estadoWave == true);

                if (waveActivaActual == null)
                {
                    _logger.LogError("Error. No se encontró la wave activa en WaveRelease.");
                    return NotFound("Error. No se encontró la wave activa en WaveRelease.");
                }

                // Lista para almacenar las familias procesadas
                var familiasProcesadas = new HashSet<string>();

                // Procesar cada orden individualmente
                foreach (var loadDtl in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                {
                    var dtlnum = loadDtl.dtlnum;

                    // Buscar la orden según su dtlnum según la wave activa
                    var orden = await _context.ordenesEnProceso
                        .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum && o.wave == waveActivaActual.Wave);

                    if (orden == null)
                    {
                        _logger.LogError($"IF ORDEN NULL!!!!!!!!!!!!!!");
                        continue; // Si no se encuentra la orden, continuar con la siguiente
                    }

                    // Desactivar la orden
                    orden.estadoLuca = false; // Marcar como completada (KN)
                    orden.estado = false; // Marcar como completada (Senad)
                    orden.fechaProceso = DateTime.UtcNow.AddHours(-3); // Actualizar fecha de proceso

                    // Guardar la familia de la orden para su posterior verificación
                    familiasProcesadas.Add(orden.familia);

                    /*
                    // Desactivar la wave de la orden
                    try
                    {
                        var numOrden = orden.numOrden;
                        var codProducto = orden.codProducto;
                        SetAuthorizationHeader(_apiWaveReleaseClient);
                        var response = await desactivarWaveAsync(numOrden, codProducto);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation($"Orden {numOrden} ya desactivada!. StatusCode: {response.StatusCode}");
                        }
                        else
                        {
                            _logger.LogInformation($"Wave de la orden {numOrden} desactivada correctamente.");
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _logger.LogError($"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                    }
                    */
                }

                // Guardar los cambios en la base de datos después de procesar todas las órdenes
                await _context.SaveChangesAsync();

                // Verificar si todas las órdenes de las familias procesadas están completadas
                foreach (var familia in familiasProcesadas)
                {
                    // Obtener todas las órdenes de la familia en la wave actual
                    var ordenesFamilia = await _context.ordenesEnProceso
                        .Where(o => o.familia == familia && o.wave == waveActivaActual.Wave)
                        .ToListAsync();

                    // Verificar si todas las órdenes de la familia están completadas
                    bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);
                    _logger.LogInformation($"SHORT - Todas las órdenes completadas de la familia {familia}: {todasOrdenesCompletadas}");

                    if (todasOrdenesCompletadas)
                    {
                        // Obtener la tanda actual de FamilyMaster
                        var FamilyMaster = await _context.FamilyMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Familia == familia);

                        if (FamilyMaster == null)
                        {
                            _logger.LogError("Error. No se encontró la familia en FamilyMaster.");
                            return NotFound("Error. No se encontró la familia en FamilyMaster.");
                        }

                        var numTandaActual = FamilyMaster.NumTanda;

                        _logger.LogInformation($"Wave actual: {waveActivaActual.Wave}");
                        _logger.LogInformation($"Tanda actual: {numTandaActual}");


                        // Desactivar todas las waves de esta familia antes de activar la siguiente tanda
                        _logger.LogInformation("Short - Inicio func Desactivar waves de la familia: {Familia}", familia);
                        await DesactivarWavesDeFamiliaAsync(familia);

                        SetAuthorizationHeader(_apiWaveReleaseClient);
                        try
                        {
                            // Activar la siguiente tanda
                            var response = await activarTandaSinActivadaAsync(numTandaActual);
                            _logger.LogInformation("ACTIVACIÓN DE SIGUIENTE TANDA!!!!!");

                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogError($"Error. Fallo al activar la siguiente tanda en FamilyMaster. StatusCode: {response.StatusCode}");
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                        }
                    }
                }

                // Agrupamos por dtlnum y tomamos la primera ocurrencia de cada grupo
                var uniqueDetails = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG
                    .GroupBy(d => d.dtlnum)
                    .Select(g => g.First())
                    .ToList();

                // Guardar registros de confirmación en la BD
                foreach (var detail in uniqueDetails)
                {
                    var nuevaConfirmada = new Confirmada
                    {
                        WcsId = request.SORT_COMPLETE.wcs_id,
                        WhId = request.SORT_COMPLETE.wh_id,
                        MsgId = request.SORT_COMPLETE.msg_id,
                        TranDt = DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy HH:mm:ss"),
                        LodNum = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                        SubNum = detail.subnum,
                        DtlNum = detail.dtlnum,
                        StoLoc = detail.stoloc,
                        Qty = detail.qty,
                        accion = "Short-pick"
                    };

                    await _context.Confirmada.AddAsync(nuevaConfirmada);
                }

                // Guardar cambios en la BD
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ocurrió un error al procesar las órdenes. Message: {ex.Message}, InnerException: {ex.InnerException}");
                return StatusCode(500, $"Ocurrió un error al procesar las órdenes: {ex.Message}");
            }

            // Filtrar los detalles con qty > 0 antes de enviar a KN
            var detallesFiltrados = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG
                .Where(d => d.qty > 0)
                .ToList();

            var requestFiltrado = new SortCompleteKN
            {
                SORT_COMPLETE = new SortComplete
                {
                    wcs_id = request.SORT_COMPLETE.wcs_id,
                    wh_id = request.SORT_COMPLETE.wh_id,
                    msg_id = request.SORT_COMPLETE.msg_id,
                    trandt = request.SORT_COMPLETE.trandt,
                    SORT_COMP_SEG = new SortCompSeg
                    {
                        LOAD_HDR_SEG = new LoadHdrSeg
                        {
                            LODNUM = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                            LOAD_DTL_SEG = detallesFiltrados
                        }
                    }
                }
            };

            // Si no quedan detalles en la lista, no enviar a KN
            if (requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                _logger.LogInformation("No hay detalles para enviar a KN.");
                return Ok("No hay detalles para enviar a KN.");
            }


            var jsonFiltrado = JsonConvert.SerializeObject(requestFiltrado);
            _logger.LogInformation("Short - JSON FILTRADO: " + jsonFiltrado);


            // Enviar datos a KN
            try
            {
                var urlKN = _configuration["ExternalService:UrlKN"];
                _logger.LogInformation("URL KN:" + urlKN);

                using (var client = new HttpClient())
                {
                    // Basic Auth
                    var username = _configuration["BasicAuth:Username"];
                    var password = _configuration["BasicAuth:Password"];

                    var array = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(array));

                    // Serializar el JSON
                    var jsonContent = JsonConvert.SerializeObject(requestFiltrado);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // POST
                    var response = await client.PostAsync(urlKN, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Datos enviados correctamente a KN.");
                        return Ok("Datos enviados correctamente a KN.");
                    }
                    else
                    {
                        _logger.LogInformation("Error. Fallo al enviar confirmación a KN.");
                        return StatusCode((int)response.StatusCode, "Error. Fallo al enviar confirmación a KN.");
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"Ocurrió un error HTTP al enviar los datos a KN: Message: {httpEx.Message}, InnerException: {httpEx.InnerException}");
                return StatusCode(500, $"Ocurrió un error HTTP al enviar los datos a KN: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ocurrió un error al enviar los datos a KN:  Message: {ex.Message}, InnerException: {ex.InnerException}");
                return StatusCode(500, $"Ocurrió un error al enviar los datos a KN: {ex.Message}");
            }
        }

        [HttpPost("CerrarSalida")]
        public async Task<IActionResult> CerrarSalida([FromQuery] string familia, [FromQuery] int numSalida)
        {
            Console.WriteLine($"===> Recibida petición para cerrar salida {numSalida} de la familia {familia}");

            try
            {
                // Buscar la salida activa
                var salidaActual = await _context.FamilyMaster
                    .FirstOrDefaultAsync(f => f.Familia == familia && f.NumSalida == numSalida && f.estado == true);

                if (salidaActual == null)
                {
                    Console.WriteLine("No se encontró la salida activa especificada.");
                    return NotFound("No se encontró la salida activa especificada.");
                }

                // Desactivar la salida
                salidaActual.estado = false;
                _context.FamilyMaster.Update(salidaActual);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Salida {numSalida} de la familia {familia} cerrada correctamente.");

                // Verificar si quedan otras salidas activas para esta familia
                bool quedanSalidasActivas = await _context.FamilyMaster
                    .AnyAsync(f => f.Familia == familia && f.estado == true);

                if (quedanSalidasActivas)
                {
                    Console.WriteLine("Aún hay salidas activas para esta familia. No se activará la siguiente tanda.");
                    return Ok("Salida cerrada. Aún hay otras salidas activas para esta familia.");
                }

                Console.WriteLine("Última salida cerrada. Se procede a cerrar órdenes y activar siguiente tanda.");

                // Desactivar estadoWave solo para las órdenes activas de esta familia
                var wavesFamilia = await _context.WaveRelease
                    .Where(w => w.familia == familia && w.estadoWave == true)
                    .ToListAsync();

                foreach (var wave in wavesFamilia)
                {
                    wave.estadoWave = false;
                }

                _context.WaveRelease.UpdateRange(wavesFamilia);
                await _context.SaveChangesAsync();

                // Activar siguiente tanda
                var tandaActual = salidaActual.NumTanda;

                try
                {
                    SetAuthorizationHeader(_apiWaveReleaseClient);
                    var response = await activarSiguienteTandaAsyncFamily(tandaActual);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Tanda {tandaActual} activada correctamente.");
                        return Ok("Última salida cerrada y siguiente tanda activada correctamente.");
                    }
                    else
                    {
                        Console.WriteLine($"Error al activar la tanda. StatusCode: {response.StatusCode}");
                        return StatusCode((int)response.StatusCode, "Error al activar la siguiente tanda.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Excepción al activar tanda: {ex.Message}");
                    return StatusCode(500, "Error al activar la siguiente tanda: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general en CerrarSalida: {ex.Message}");
                return StatusCode(500, "Error al procesar la solicitud: " + ex.Message);
            }
        }



        [HttpPost("Split")]
        public async Task<IActionResult> splitPick([FromBody] SortCompleteKN request)
        {
            _logger.LogInformation("---------- INICIO SPLIT PICK ----------");

            var jsonLog = JsonConvert.SerializeObject(request);
            LogJsonToFile(jsonLog, "Split");

            if (request?.SORT_COMPLETE?.SORT_COMP_SEG?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null ||
                !request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Any())
            {
                _logger.LogError("Error. Datos en formato incorrecto.");
                return BadRequest("Error. Datos en formato incorrecto.");
            }


            // Obtener la wave activa actual (fuera del bucle)
            var waveActivaActual = await _context.WaveRelease
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.estadoWave == true);

            if (waveActivaActual == null)
            {
                _logger.LogError("Error. No se encontró la wave activa en WaveRelease.");
                return NotFound("Error. No se encontró la wave activa en WaveRelease.");
            }

            var familiasProcesadas = new HashSet<string>();

            try
            {
                foreach (var loadDtl in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                {
                    var dtlnum = loadDtl.dtlnum;
                    var qty = loadDtl.qty;

                    // Buscar la orden según su dtlnum según la wave activa
                    var orden = await _context.ordenesEnProceso
                        .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum && o.wave == waveActivaActual.Wave);

                    if (orden == null)
                    {
                        _logger.LogInformation("IF ORDEN NULL!!!!!");
                        //return NotFound($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        continue;
                    }

                    // Verificación: Si la cantidad recibida equivale a la cantidad total LPN!
                    if (qty == orden.cantidadLPN)
                    {
                        _logger.LogInformation($"Split completo detectado para dtlnum: {dtlnum}. Cantidad: {qty} igual a cantidadLPN: {orden.cantidadLPN}");

                        orden.estado = false; // Marcar como completada (Senad)
                        orden.estadoLuca = false; // Marcar como completada (KN)
                        orden.fechaProceso = DateTime.UtcNow.AddHours(-3); // Actualizar fecha de proceso

                        //Guardar la familia para verificación posterior
                        familiasProcesadas.Add(orden.familia);

                        /*
                        // Desactivar la wave de la orden
                        try
                        {
                            var numOrden = orden.numOrden;
                            var codProducto = orden.codProducto;
                            SetAuthorizationHeader(_apiWaveReleaseClient);
                            var response = await desactivarWaveAsync(numOrden, codProducto);
                            _logger.LogInformation($"Desactivando Wave de dtlnum {dtlnum} con orden {numOrden}, codProducto {codProducto}");

                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogInformation($"Orden {numOrden} ya desactivada!. StatusCode: {response.StatusCode}");
                            }
                            else
                            {
                                _logger.LogInformation($"Wave de la orden {numOrden} desactivada correctamente.");
                            }

                        }
                        catch (HttpRequestException httpEx)
                        {
                            _logger.LogError($"Error. Problema HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error. Problema al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                        }
                        */
                    }
                    else
                    {
                        _logger.LogInformation($"Split corto detectado para dtlnum: {dtlnum}. Cantidad: {qty}, cantidadLPN: {orden.cantidadLPN}");
                        orden.fechaProceso = DateTime.UtcNow.AddHours(-3);
                    }

                }
                // Guardar cambios a BD
                await _context.SaveChangesAsync();

                // Verificar si todas las órdenes de las familias procesadas están completadas
                foreach (var familia in familiasProcesadas)
                {
                    // Obtener todas las órdenes de la familia en la wave actual
                    var ordenesFamilia = await _context.ordenesEnProceso
                        .Where(o => o.familia == familia && o.wave == waveActivaActual.Wave)
                        .ToListAsync();

                    // Verificar si todas las órdenes de la familia están completadas
                    bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estadoLuca == false);
                    _logger.LogInformation($"SPLIT - Todas las órdenes completadas de la familia {familia}: {todasOrdenesCompletadas}");

                    if (todasOrdenesCompletadas)
                    {
                        // Obtener la tanda actual de FamilyMaster
                        var FamilyMaster = await _context.FamilyMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Familia == familia);

                        if (FamilyMaster == null)
                        {
                            _logger.LogError("Error. No se encontró la familia en FamilyMaster.");
                            return NotFound("Error. No se encontró la familia en FamilyMaster.");
                        }

                        var numTandaActual = FamilyMaster.NumTanda;

                        _logger.LogInformation($"Wave actual: {waveActivaActual.Wave}");
                        _logger.LogInformation($"Tanda actual: {numTandaActual}");


                        // Desactivar todas las waves de esta familia antes de activar la siguiente tanda
                        _logger.LogInformation("Split - Inicio Desactivar waves de la familia: {Familia}", familia);
                        await DesactivarWavesDeFamiliaAsync(familia);

                        SetAuthorizationHeader(_apiWaveReleaseClient);

                        try
                        {
                            // Activar la siguiente tanda
                            var response = await activarTandaSinActivadaAsync(numTandaActual);
                            _logger.LogInformation("ACTIVACIÓN DE SIGUIENTE TANDA!!!!!");

                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogError($"Error. Fallo al activar la siguiente tanda en FamilyMaster. StatusCode: {response.StatusCode}");
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ocurrió un error al procesar las órdenes: Message: {ex.Message}, InnerException: {ex.InnerException}");
                return StatusCode(500, $"Ocurrió un error al procesar las órdenes: {ex.Message}");
            }

            // Agrupamos por dtlnum y tomamos la primera ocurrencia de cada grupo
            var uniqueDetails = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG
                .GroupBy(d => d.dtlnum)
                .Select(g => g.First())
                .ToList();

            // Guardar registros de confirmación en la BD
            foreach (var detail in uniqueDetails)
            {
                var nuevaConfirmada = new Confirmada
                {
                    WcsId = request.SORT_COMPLETE.wcs_id,
                    WhId = request.SORT_COMPLETE.wh_id,
                    MsgId = request.SORT_COMPLETE.msg_id,
                    TranDt = DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy HH:mm:ss"),
                    LodNum = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                    SubNum = detail.subnum,
                    DtlNum = detail.dtlnum,
                    StoLoc = detail.stoloc,
                    Qty = detail.qty,
                    accion = "Split-short"
                };

                await _context.Confirmada.AddAsync(nuevaConfirmada);
            }

            // Guardar cambios a BD
            await _context.SaveChangesAsync();



            // INICIO DE FILTRO!!!!!!!!!!!!!!!!

            // Filtrar los detalles con qty > 0 antes de enviar a KN
            var detallesFiltrados = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG
                .Where(d => d.qty > 0)
                .ToList();

            var requestFiltrado = new SortCompleteKN
            {
                SORT_COMPLETE = new SortComplete
                {
                    wcs_id = request.SORT_COMPLETE.wcs_id,
                    wh_id = request.SORT_COMPLETE.wh_id,
                    msg_id = request.SORT_COMPLETE.msg_id,
                    trandt = request.SORT_COMPLETE.trandt,
                    SORT_COMP_SEG = new SortCompSeg
                    {
                        LOAD_HDR_SEG = new LoadHdrSeg
                        {
                            LODNUM = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                            LOAD_DTL_SEG = detallesFiltrados
                        }
                    }
                }
            };

            //Si no quedan detalles en la lista, no enviar a KN
            if (requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                _logger.LogInformation("No hay detalles para enviar a KN.");
                return Ok("No hay detalles para enviar a KN.");
            }

            // Console WriteLine del json filtrado
            var jsonFiltrado = JsonConvert.SerializeObject(requestFiltrado);
            _logger.LogInformation("Split - JSON FILTRADO: " + jsonFiltrado);

            
            //ENVIO DE DATOS A LA URL DE KN
            try
            {
                var urlKN = _configuration["ExternalService:UrlKN"];
                _logger.LogInformation("URL KN:" + urlKN);

                using (var client = new HttpClient())
                {
                    // Basic Auth
                    var username = _configuration["BasicAuth:Username"];
                    var password = _configuration["BasicAuth:Password"];

                    var array = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(array));

                    //Serializar el JSON.
                    var jsonContent = JsonConvert.SerializeObject(requestFiltrado);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    //POST
                    var response = await client.PostAsync(urlKN, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Datos enviados correctamente a KN.");
                        return Ok("Datos enviados correctamente a KN.");
                    }
                    else
                    {
                        _logger.LogError("Error. Fallo al enviar confirmación a KN.");
                        return StatusCode((int)response.StatusCode, "Error. Fallo al enviar confirmación a KN.");
                    }
                }

            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"Ocurrió un error HTTP al enviar los datos a KN: Message: {httpEx.Message}, InnerException: {httpEx.InnerException}");
                return StatusCode(500, $"Ocurrió un error HTTP al enviar los datos a KN: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ocurrió un error al enviar los datos a KN: Message: {ex.Message}, InnerException: {ex.InnerException}");
                return StatusCode(500, $"Ocurrió un error al enviar los datos a KN: {ex.Message}");
            }
            


        }

    }
}
