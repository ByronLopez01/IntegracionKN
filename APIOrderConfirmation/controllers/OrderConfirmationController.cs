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

        private static List<int> _activatedTandas = new();

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
                Console.WriteLine($"La tanda {numTandaActual} ya fue activada en esta Wave.");
                // Devuelve un OK simulado.
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
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

        // Endpoint para resetear la lista de tandas activadas
        [HttpPost("ResetTandas")]
        public IActionResult ResetTandas()
        {
            _activatedTandas.Clear();
            Console.WriteLine("Reset de tandas activadas.");
            return Ok("Reset completado");
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
                    Console.WriteLine("DENTRO DEL FOR!!!");
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
                        return BadRequest($"La orden con dtlnum {dtlnum} ya fue procesada.");
                    }
                    */

                    // Si la cantidad procesada es igual a la cantidad LPN, actualizar el estadoLuca a false
                    if (orden.cantidadProcesada == orden.cantidadLPN)
                    {
                        Console.WriteLine("PROCESADO CON SORTER!!!!!");
                        // Actualizar el estadoLuca a false
                        orden.estadoLuca = false;

                        // COMIENZO DE DESACTIVAR WAVE !!!!!!!!
                        try
                        {
                            Console.WriteLine("COMIENZO DE DESACTIVAR WAVE!!!");
                            var numOrden = orden.numOrden;
                            var codProducto = orden.codProducto;
                            SetAuthorizationHeader(_apiWaveReleaseClient);

                            var response = await desactivarWaveAsync(numOrden, codProducto);

                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Orden {numOrden} ya desactivada!. StatusCode: {response.StatusCode}");
                                //return StatusCode((int)response.StatusCode, $"Error al desactivar la wave de la orden {numOrden}.");
                            }
                            else
                            {
                                Console.WriteLine($"Wave de la orden {numOrden} desactivada correctamente.");
                            }

                            
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

                        // FamilyMaster para obtener la tanda.
                        var FamilyMaster = await _context.FamilyMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Familia == orden.familia);

                        if (FamilyMaster == null)
                        {
                            Console.WriteLine("No se encontró la familia en FamilyMaster.");
                            return NotFound("No se encontró la familia en FamilyMaster.");
                        }

                        var waveActivaActual = await _context.WaveRelease
                            .AsNoTracking()
                            .FirstOrDefaultAsync(w => w.estadoWave == true);

                        if (waveActivaActual == null)
                        {
                            Console.WriteLine("No se encontró la wave activa en WaveRelease.");
                            return NotFound("No se encontró la wave activa en WaveRelease.");
                        }

                        // Recargar la orden para obtener los cambios
                        await _context.Entry(orden).ReloadAsync();

                        // Verificar si todas las órdenes de la familia han sido completadas
                        var familia = orden.familia;
                        //var wave = orden.wave;


                        // Buscar las ordenes de la familia en la wave actual.
                        var ordenesFamilia = await _context.ordenesEnProceso
                            .Where(o => o.familia == familia && o.wave == waveActivaActual.Wave)
                            .ToListAsync();

                        bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);
                        Console.WriteLine($"PROCESADO - Todas las Ordenes Completadas de la familia {familia}: {todasOrdenesCompletadas}");


                        if (todasOrdenesCompletadas)
                        {
                            //int numTandaActual = orden.numTanda;
                            int numTandaActual = FamilyMaster.NumTanda;

                            Console.WriteLine($"Tanda actual: {numTandaActual}");
                            SetAuthorizationHeader(_apiWaveReleaseClient);
                            Console.WriteLine("ACTIVACION DE SIGUIENTE TANDA!!!!!");
                            Console.WriteLine($"Tanda actual: {numTandaActual}");

                            try
                            {
                                var response = await activarTandaSinActivadaAsync(numTandaActual);

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
                        Console.WriteLine("PROCESADO SIN SORTER!!!!!!");

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
                                Console.WriteLine($"Orden {numOrden} ya desactivada!. StatusCode: {response.StatusCode}");
                                //return StatusCode((int)response.StatusCode, $"Error al desactivar la wave de la orden {numOrden}.");
                            }
                            else
                            {
                                Console.WriteLine($"Wave de la orden {numOrden} desactivada correctamente.");
                            }
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

                        // FamilyMaster para obtener la tanda.
                        var FamilyMaster = await _context.FamilyMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Familia == orden.familia);

                        //Verificar que FamilyMaster no sea nulo
                        if (FamilyMaster == null)
                        {
                            Console.WriteLine("No se encontró la familia en FamilyMaster.");
                            return NotFound("No se encontró la familia en FamilyMaster.");
                        }

                        var waveActivaActual = await _context.WaveRelease
                            .AsNoTracking()
                            .FirstOrDefaultAsync(w => w.estadoWave == true);

                        if (waveActivaActual == null)
                        {
                            Console.WriteLine("No se encontró la wave activa en WaveRelease.");
                            return NotFound("No se encontró la wave activa en WaveRelease.");
                        }

                        // Recargar la orden para obtener los cambios
                        await _context.Entry(orden).ReloadAsync();

                        // Verificar si todas las órdenes de la familia han sido completadas
                        var familia = orden.familia;

                        // Buscar las ordenes de la familia en la wave actual.
                        var ordenesFamilia = await _context.ordenesEnProceso
                            .Where(o => o.familia == familia && o.wave == waveActivaActual.Wave)
                            .ToListAsync();

                        bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);

                        if (todasOrdenesCompletadas)
                        {
                            
                            int numTandaActual = FamilyMaster.NumTanda;

                            SetAuthorizationHeader(_apiWaveReleaseClient);
                            try
                            {
                                var response = await activarTandaSinActivadaAsync(numTandaActual);
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

                    foreach (var detail in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                    {

                        // Verificar si ya existe un registro con los mismos valores clave
                        var existeEnConfirmada = await _context.Confirmada
                            .AsNoTracking()
                            .AnyAsync(c => c.DtlNum == detail.dtlnum);


                        if (!existeEnConfirmada)
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
                                accion = "Procesado"
                            };

                            await _context.Confirmada.AddAsync(nuevaConfirmada);
                        }
                    }

                    await _context.SaveChangesAsync();
                    _context.ordenesEnProceso.Update(orden);
                }

                await _context.SaveChangesAsync();
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

            //Si no quedan detalles en la lista, no enviar a KN
            if (requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                Console.WriteLine("No hay detalles con qty > 0 para enviar a KN.");
                return Ok("No hay detalles con qty > 0 para enviar.");
            }

            // Console WriteLine del json filtrado
            var jsonFiltrado = JsonConvert.SerializeObject(requestFiltrado);
            Console.WriteLine("JSON FILTRADO: " + jsonFiltrado);


            Console.WriteLine("ENVIADO A KN CORRECTAMENTE");
            //return Ok("ENVIADO A KN CORRECTAMENTE");

            

            
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
                        Console.WriteLine($"IF ORDEN NULL!!!!!!!!!!!!!!");
                        //return NotFound($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        continue;
                    }

                    /*
                    if (!orden.estadoLuca)
                    {
                        // Si la orden ya fue procesada, retornar un error
                        Console.WriteLine($"La orden con dtlnum {dtlnum} ya fue procesada.");
                        return BadRequest($"La orden con dtlnum {dtlnum} ya fue procesada.");
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
                            Console.WriteLine($"Orden {numOrden} ya desactivada!. StatusCode: {response.StatusCode}");
                            //return StatusCode((int)response.StatusCode, $"Error al desactivar la wave de la orden {numOrden}.");
                        }
                        else
                        {
                            Console.WriteLine($"Wave de la orden {numOrden} desactivada correctamente.");
                        }
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

                    // FamilyMaster para obtener la tanda.
                    var FamilyMaster = await _context.FamilyMaster
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f => f.Familia == orden.familia);

                    //Verificar que FamilyMaster no sea nulo
                    if (FamilyMaster == null)
                    {
                        Console.WriteLine("No se encontró la familia en FamilyMaster.");
                        return NotFound("No se encontró la familia en FamilyMaster.");
                    }

                    var waveActivaActual = await _context.WaveRelease
                        .AsNoTracking()
                        .FirstOrDefaultAsync(w => w.estadoWave == true);

                    if (waveActivaActual == null)
                    {
                        Console.WriteLine("No se encontró la wave activa en WaveRelease.");
                        return NotFound("No se encontró la wave activa en WaveRelease.");
                    }

                    // Recargar la orden para obtener los cambios
                    await _context.Entry(orden).ReloadAsync();

                    // Verificar si todas las órdenes de la familia han sido completadas
                    var familia = orden.familia;

                    // Buscar las ordenes de la familia en la wave actual.
                    var ordenesFamilia = await _context.ordenesEnProceso
                        .Where(o => o.familia == familia && o.wave == waveActivaActual.Wave)
                        .ToListAsync();

                    bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);
                    Console.WriteLine($"SHORT - Todas las Ordenes Completadas de la familia {familia}: {todasOrdenesCompletadas}");

                    if (todasOrdenesCompletadas)
                    {
                        // Buscar la familia de la orden en el FamilyMaster
                        var numTandaActual = FamilyMaster.NumTanda;

                        var wave = waveActivaActual.Wave;

                        Console.WriteLine($"Wave actual: {wave}");

                        SetAuthorizationHeader(_apiWaveReleaseClient);
                        try
                        {
                            Console.WriteLine($"Tanda actual: {numTandaActual}");
                            var response = await activarTandaSinActivadaAsync(numTandaActual);
                            Console.WriteLine("ACTIVACION DE SIGUIENTE TANDA!!!!!");
                            Console.WriteLine($"Tanda actual: {numTandaActual}");

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

                    // GUARDAR REGISTRO DE LA CONFIRMACIÓN EN LA BD
                    foreach (var detail in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                    {
                        // Verificar si ya existe un registro con los mismos valores clave
                        var existeEnConfirmada = await _context.Confirmada
                            .AsNoTracking()
                            .AnyAsync(c => c.DtlNum == detail.dtlnum);


                        if (!existeEnConfirmada)
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
                                accion = "Short-pick"
                            };

                            await _context.Confirmada.AddAsync(nuevaConfirmada);
                        }

                        _context.ordenesEnProceso.Update(orden);
                    }

                    // Guardar cambios a BD
                    await _context.SaveChangesAsync();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al procesar las órdenes: " + ex.Message);
                Console.WriteLine("Inner Exception: " + ex.InnerException);
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

          

            //Si no quedan detalles en la lista, no enviar a KN
            if (requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                Console.WriteLine("No hay detalles para enviar a KN.");
                return BadRequest("No hay detalles para enviar a KN.");
            }

            // Console WriteLine del json filtrado
            var jsonFiltrado = JsonConvert.SerializeObject(requestFiltrado);
            Console.WriteLine("JSON FILTRADO: " + jsonFiltrado);


            Console.WriteLine("ENVIADO A KN CORRECTAMENTE");
            //return Ok("ENVIADO A KN CORRECTAMENTE");

            
            
            
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
                        // Verificar si ya existe un registro con los mismos valores clave
                        var existeEnConfirmada = await _context.Confirmada
                            .AnyAsync(c => c.DtlNum == detail.dtlnum);


                        if (!existeEnConfirmada)
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

                        _context.ordenesEnProceso.Update(orden);
                    }

                    // Guardar cambios a BD
                    await _context.SaveChangesAsync();

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


            //Si no quedan detalles en la lista, no enviar a KN
            if (requestFiltrado.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Count == 0)
            {
                Console.WriteLine("No hay detalles para enviar a KN.");
                return BadRequest("No hay detalles para enviar a KN.");
            }


            // Console WriteLine del json filtrado
            var jsonFiltrado = JsonConvert.SerializeObject(requestFiltrado);
            Console.WriteLine("JSON FILTRADO: " + jsonFiltrado);


            Console.WriteLine("ENVIADO A KN CORRECTAMENTE");    
            //return Ok("ENVIADO A KN CORRECTAMENTE");

            
            
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
