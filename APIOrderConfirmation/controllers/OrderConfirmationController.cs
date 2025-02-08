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
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

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
        private static int _numSubNum = 1;

        public OrderConfirmationController(IOrderConfirmationService orderConfirmationService, OrderConfirmationContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory, HttpClient httpClient)
        {
            _httpClientFactory = httpClientFactory;
            _apiWaveReleaseClient = httpClientFactory.CreateClient("apiWaveRelease");
            _orderConfirmationService = orderConfirmationService;
            _context = context;
            _configuration = configuration;
        }

        private async Task<HttpResponseMessage> activarSiguienteTandaAsync(int numTandaActual)
        {
            var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster/activarSiguienteTanda?numTandaActual={numTandaActual}";
            Console.WriteLine("URL FamilyMaster: " + urlFamilyMaster);
            // Llamamos con un POST el endpoint de FamilyMaster para activar la siguiente tanda
            var familyMasterResponse = await _apiWaveReleaseClient.PostAsync(urlFamilyMaster, null);
            Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
            return(familyMasterResponse);
        }

        private async Task<HttpResponseMessage> desactivarWaveAsync(String numOrden, String codProducto)
        {
            var DesactivarWave = "http://apiwaverelease:8080/api/WaveRelease/DesactivarWave";
            var waveURL = $"{DesactivarWave}/{numOrden}/{codProducto}";
            Console.WriteLine("URL: " + waveURL);
            var desactivarWaveResponse = await _apiWaveReleaseClient.PostAsync(waveURL, null);
            return (desactivarWaveResponse);
        }
        private void LogJsonToFile(string jsonContent, string endpoint)
        {
            var logFilePath = "/app/logs/confirm_log.txt"; // Ruta del archivo de log en el contenedor Docker
            var logEntry = $"{DateTime.UtcNow}: {endpoint} - {jsonContent}{Environment.NewLine}";

            try
            {
                using (var writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al escribir en el archivo de log: {ex.Message}");
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

        [HttpPost("Procesado")]
        public async Task<IActionResult> Procesado([FromBody] SortCompleteKN request)
        {
            var jsonLog = JsonConvert.SerializeObject(request);
            LogJsonToFile(jsonLog, "Procesado");

            if (request?.SORT_COMPLETE?.SORT_COMP_SEG?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null ||
                request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                return BadRequest("Datos en formato incorrecto.");
            }
            try
            {
                foreach (var loadDtl in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                {
                    var dtlnum = loadDtl.dtlnum;

                    // Buscar la orden segun su dtlnum
                    var orden = await _context.ordenesEnProceso
                        .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum);
                    var ordenes = _context.ordenes;

                    if (orden == null)
                    {
                        // Not found si no encuentra la orden
                        Console.WriteLine($"IF ORDEN NULL!!!!!!!!!!!!!!");
                        //return NotFound($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        continue;
                    }

                    /*
                    if (!orden.estadoLuca)
                    {
                        // Si la orden ya fue procesada, retornar un error
                        Console.WriteLine($"La orden con dtlnum {dtlnum} ya fue procesada.");
                        //return BadRequest($"La orden con dtlnum {dtlnum} ya fue procesada.");
                        continue;
                    }
                    */

                    // Si la cantidad procesada es igual a la cantidad LPN, actualizar el estadoLuca a false
                    if (orden.cantidadProcesada == orden.cantidadLPN)
                    {
                        // Actualizar el estadoLuca a false
                        orden.estadoLuca = false;


                        // COMIENZO DE DESACTIVAR WAVE !!!!!!!!
                        try
                        {

                            var numOrden = orden.numOrden;
                            var codProducto = orden.codProducto;
                            SetAuthorizationHeader(_apiWaveReleaseClient);

                            var response = await desactivarWaveAsync(numOrden, codProducto);

                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Error al desactivar la wave de la orden {numOrden}. StatusCode: {response.StatusCode}");
                                //return StatusCode((int)response.StatusCode, $"Error al desactivar la wave de la orden {numOrden}.");
                            }

                            Console.WriteLine($"Wave de la orden {numOrden} desactivada correctamente.");
                        }
                        //Exception HTTP
                        catch (HttpRequestException httpEx)
                        {
                            Console.WriteLine($"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                            //return StatusCode(500, $"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                        }
                        //Exception general
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                            //return StatusCode(500, $"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                        }

                        //COMIENZO DE ACTIVAR SIGUIENTE TANDA!!!!!

                        // Verificar si todas las órdenes de la familia han sido completadas
                        var familia = orden.familia;
                        var ordenesFamilia = await _context.ordenesEnProceso
                            .Where(o => o.familia == familia)
                            .ToListAsync();

                        bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);


                        if (todasOrdenesCompletadas)
                        {
                            int numTandaActual = orden.numTanda;
                            SetAuthorizationHeader(_apiWaveReleaseClient);

                            try
                            {
                                var response = await activarSiguienteTandaAsync(numTandaActual);

                                if (!response.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster. StatusCode: {response.StatusCode}");
                                    //return StatusCode((int)response.StatusCode, $"Error al activar la siguiente tanda en FamilyMaster.");
                                }

                            }
                            catch (HttpRequestException ex)
                            {
                                Console.WriteLine($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                                return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                                return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            }

                        }


                    }

                    else if (orden.cantidadProcesada != orden.cantidadLPN)
                    {
                        Console.WriteLine("PROCESADO SIN SORTER!!!!!!!!");

                        // Actualizar estadoLuca (Confirmar a KN)
                        orden.estadoLuca = false;
                        // Actualizar estado(Confirmacion Senad)
                        orden.estado = false;
                        orden.fechaProceso = DateTime.Now.AddHours(-2);

                        // INICIO DE DESACTIVAR WAVE !!!!!!!!
                        try
                        {
                            var numOrden = orden.numOrden;
                            var codProducto = orden.codProducto;

                            SetAuthorizationHeader(_apiWaveReleaseClient);

                            var response = await desactivarWaveAsync(numOrden, codProducto);

                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Error al desactivar la wave de la orden {numOrden}. StatusCode: {response.StatusCode}");
                                //return StatusCode((int)response.StatusCode, $"Error al desactivar la wave de la orden {numOrden}.");
                            }
                            Console.WriteLine($"Wave de la orden {numOrden} desactivada correctamente.");
                        }
                        //Exception HTTP
                        catch (HttpRequestException httpEx)
                        {
                            Console.WriteLine($"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                            //return StatusCode(500, $"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                        }
                        //Exception general
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                            //return StatusCode(500, $"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                        }


                        // INICIO DE ACTIVAR SIGUIENTE TANDA!!!!!

                        // Verificar si todas las órdenes de la familia han sido completadas
                        var familia = orden.familia;
                        var ordenesFamilia = await _context.ordenesEnProceso
                            .Where(o => o.familia == familia)
                            .ToListAsync();

                        bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);

                        if (todasOrdenesCompletadas)
                        {
                            int numTandaActual = orden.numTanda;
                            SetAuthorizationHeader(_apiWaveReleaseClient);
                            try
                            {
                                var response = await activarSiguienteTandaAsync(numTandaActual);
                                if (!response.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster. StatusCode: {response.StatusCode}");
                                    //return StatusCode((int)response.StatusCode, $"Error al activar la siguiente tanda en FamilyMaster.");
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                Console.WriteLine($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                                return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                                return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            }


                            foreach (var detail in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                            {
                                var nuevaConfirmada = new Confirmada
                                {
                                    WcsId = request.SORT_COMPLETE.wcs_id,
                                    WhId = request.SORT_COMPLETE.wh_id,
                                    MsgId = request.SORT_COMPLETE.msg_id,
                                    TranDt = DateTime.Now.ToString("yyyyMMddHHmmss"),
                                    LodNum = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                                    SubNum = detail.subnum,
                                    DtlNum = detail.dtlnum,
                                    StoLoc = detail.stoloc,
                                    Qty = detail.qty,
                                    accion = "Completada"
                                };

                                await _context.Confirmada.AddAsync(nuevaConfirmada);
                            }
                            await _context.SaveChangesAsync();

                            Console.WriteLine("EstadoLuca actualizado correctamente.");

                            //return Ok("EstadoLuca actualizado correctamente.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al procesar las órdenes: " + ex.Message);
                return StatusCode(500, $"Ocurrió un error al procesar las órdenes: {ex.Message}");
            }

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


            //ENVIO DE DATOS A LA URL DE KN
            try
            {

                var urlKN = _configuration["ExternalService:UrlKN"];
                Console.WriteLine("URL KN:" + urlKN);

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
                        Console.WriteLine("Datos enviados correctamente a KN.");
                        return Ok("Datos enviados correctamente a KN.");
                    }
                    else
                    {
                        Console.WriteLine("Error al enviar datos a KN.");
                        return StatusCode((int)response.StatusCode, "Error al enviar datos a KN.");
                    }
                }

            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine("Ocurrió un error HTTP al enviar los datos a KN: " + httpEx.Message);
                return StatusCode(500, $"Ocurrió un error HTTP al enviar los datos a KN: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al enviar los datos a KN: " + ex.Message);
                return StatusCode(500, $"Ocurrió un error al enviar los datos a KN: {ex.Message}");
            }

        }

        [HttpPost("Short")]
        public async Task<IActionResult> shortPick([FromBody] SortCompleteKN request)
        {
            Console.WriteLine("------ SHORT PICK !!!!!! ----------");

            var jsonLog = JsonConvert.SerializeObject(request);
            LogJsonToFile(jsonLog, "Short");

            if (request?.SORT_COMPLETE?.SORT_COMP_SEG?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null ||
                request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                return BadRequest("Datos en formato incorrecto.");
            }

            try
            {
                foreach (var loadDtl in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                {
                    var dtlnum = loadDtl.dtlnum;

                    // Buscar la orden segun su dtlnum
                    var orden = await _context.ordenesEnProceso
                        .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum);
                    var ordenes = _context.ordenes;

                    if (orden == null)
                    {
                        // Not found si no encuentra la orden
                        Console.WriteLine($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        //return NotFound($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        continue;
                    }

                    /*
                    if (!orden.estadoLuca)
                    {
                        // Si la orden ya fue procesada, retornar un error
                        Console.WriteLine($"La orden con dtlnum {dtlnum} ya fue procesada.");
                        //return BadRequest($"La orden con dtlnum {dtlnum} ya fue procesada.");
                        continue;
                    }
                    */


                    // Actualizar estadoLuca (Confirmar a KN)
                    orden.estadoLuca = false;
                    // Actualizar estado(Confirmacion Senad)
                    orden.estado = false;
                    orden.fechaProceso = DateTime.UtcNow.AddHours(-2);


                    // INICIO DE DESACTIVAR WAVE !!!!!!!!
                    try
                    {
                        var numOrden = orden.numOrden;
                        var codProducto = orden.codProducto;
                        SetAuthorizationHeader(_apiWaveReleaseClient);
                        var response = await desactivarWaveAsync(numOrden, codProducto);
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Error al desactivar la wave de la orden {numOrden}. StatusCode: {response.StatusCode}");
                            //return StatusCode((int)response.StatusCode, $"Error al desactivar la wave de la orden {numOrden}.");
                        }
                        Console.WriteLine($"Wave de la orden {numOrden} desactivada correctamente.");
                    }
                    //Exception HTTP
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                        //return StatusCode(500, $"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                    }
                    //Exception general
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                        //return StatusCode(500, $"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                    }


                    // INICIO DE ACTIVAR SIGUIENTE TANDA!!!!!

                    // Verificar si todas las órdenes de la familia han sido completadas
                    var familia = orden.familia;
                    var ordenesFamilia = await _context.ordenesEnProceso
                        .Where(o => o.familia == familia)
                        .ToListAsync();

                    bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);

                    if (todasOrdenesCompletadas)
                    {
                        int numTandaActual = orden.numTanda;
                        SetAuthorizationHeader(_apiWaveReleaseClient);
                        try
                        {
                            var response = await activarSiguienteTandaAsync(numTandaActual);
                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster. StatusCode: {response.StatusCode}");
                                //return StatusCode((int)response.StatusCode, $"Error al activar la siguiente tanda en FamilyMaster.");
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.WriteLine($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                            return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                        }

                    }
                }

                // Guardar cambios a BD
                await _context.SaveChangesAsync();

                Console.WriteLine("EstadoLuca actualizado correctamente.");

                //return Ok("EstadoLuca actualizado correctamente.");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al procesar las órdenes: " + ex.Message);
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

            // estadoLuca = 0  =>  Orden procesada
            // estadoLuca = 1  =>  Orden activa
            // Recorrer el JSON filtrado y verificar si el dtlnum tiene estadoLuca 0 en la BD
            // Si tiene estadoLuca 0, eliminarlo de la Lista de detalles
            foreach (var detail in requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
            {
                var dtlnum = detail.dtlnum;
                // Buscar la orden segun su dtlnum
                var orden = await _context.ordenesEnProceso
                    .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum);

                if (orden == null)
                {
                    // Si no encuentra la orden o es una orden vacía.
                    Console.WriteLine($"No se encontró ninguna orden con el dtlnum {dtlnum}.");

                    // Se pasa al siguiente detalle en la lista.
                    continue;
                }

                if (!orden.estadoLuca)
                {
                    // Si la orden ya fue procesada.
                    Console.WriteLine($"La orden con dtlnum {dtlnum} ya fue procesada.");

                    // Se elimina de la lista a enviar.
                    requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Remove(detail);
                }
            }


            //ENVIO DE DATOS A LA URL DE KN
            try
            {
                var urlKN = _configuration["ExternalService:UrlKN"];
                Console.WriteLine("URL KN:" + urlKN);

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
                        Console.WriteLine("Datos enviados correctamente a KN.");
                        return Ok("Datos enviados correctamente a KN.");
                    }
                    else
                    {
                        Console.WriteLine("Error al enviar datos a KN.");
                        return StatusCode((int)response.StatusCode, "Error al enviar datos a KN.");
                    }
                }

            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine("Ocurrió un error HTTP al enviar los datos a KN: " + httpEx.Message);
                return StatusCode(500, $"Ocurrió un error HTTP al enviar los datos a KN: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al enviar los datos a KN: " + ex.Message);
                return StatusCode(500, $"Ocurrió un error al enviar los datos a KN: {ex.Message}");
            }
        }

        [HttpPost("Split")]
        public async Task<IActionResult> splitPick([FromBody] SortCompleteKN request)
        {
            Console.WriteLine("------ SPLIT PICK !!!!!! ----------");

            var jsonLog = JsonConvert.SerializeObject(request);
            LogJsonToFile(jsonLog, "Split");

            if (request?.SORT_COMPLETE?.SORT_COMP_SEG?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null ||
                !request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Any())
            {
                return BadRequest("Datos en formato incorrecto.");
            }
            try
            {
                foreach (var loadDtl in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                {
                    var dtlnum = loadDtl.dtlnum;

                    // Buscar la orden segun su dtlnum
                    var orden = await _context.ordenesEnProceso
                        .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum);
                    var ordenes = _context.ordenes;


                    if (orden == null)
                    {
                        // Not found si no encuentra la orden
                        Console.WriteLine("IF ORDEN NULL!!!!!");
                        //return NotFound($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        continue;
                    }

                    orden.fechaProceso = DateTime.UtcNow.AddHours(-2);


                    foreach (var detail in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                    {
                        var nuevaConfirmada = new Confirmada

                        {
                            WcsId = request.SORT_COMPLETE.wcs_id,
                            WhId = request.SORT_COMPLETE.wh_id,
                            MsgId = request.SORT_COMPLETE.msg_id,
                            TranDt = DateTime.Now.ToString("yyyyMMddHHmmss"),
                            LodNum = request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LODNUM,
                            SubNum = detail.subnum,
                            DtlNum = detail.dtlnum,
                            StoLoc = detail.stoloc,
                            Qty = detail.qty,
                            accion = "Split-short"

                        };

                        await _context.Confirmada.AddAsync(nuevaConfirmada);
                    }
                }

                // Guardar cambios a BD
                await _context.SaveChangesAsync();

                Console.WriteLine("EstadoLuca actualizado correctamente.");

                //return Ok("EstadoLuca actualizado correctamente.");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al procesar las órdenes: " + ex.Message);
                return StatusCode(500, $"Ocurrió un error al procesar las órdenes: {ex.Message}");
            }


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

            //ENVIO DE DATOS A LA URL DE KN
            try
            {
                var urlKN = _configuration["ExternalService:UrlKN"];
                Console.WriteLine("URL KN:" + urlKN);

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
                        Console.WriteLine("Datos enviados correctamente a KN.");
                        return Ok("Datos enviados correctamente a KN.");
                    }
                    else
                    {
                        Console.WriteLine("Error al enviar datos a KN.");
                        return StatusCode((int)response.StatusCode, "Error al enviar datos a KN.");
                    }
                }

            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine("Ocurrió un error HTTP al enviar los datos a KN: " + httpEx.Message);
                return StatusCode(500, $"Ocurrió un error HTTP al enviar los datos a KN: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al enviar los datos a KN: " + ex.Message);
                return StatusCode(500, $"Ocurrió un error al enviar los datos a KN: {ex.Message}");
            }

        }

    }
}
