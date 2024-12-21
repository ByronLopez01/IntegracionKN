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

                    if (orden == null)
                    {
                        // Not found si no encuentra la orden
                        Console.WriteLine($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        return NotFound($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                    }

                    if (!orden.estadoLuca)
                    {
                        // Si la orden ya fue procesada, retornar un error
                        Console.WriteLine($"La orden con dtlnum {dtlnum} ya fue procesada.");
                        return BadRequest($"La orden con dtlnum {dtlnum} ya fue procesada.");
                    }

                    
                    // Si la cantidad procesada es igual a la cantidad LPN, actualizar el estadoLuca a false
                    if (orden.cantidadProcesada == orden.cantidadLPN)
                    {
                        // Actualizar el estadoLuca a false
                        orden.estadoLuca = false;

                        try
                        {
                            // Llamar al endpoint "DesactivarWave"
                            var DesactivarWave = "http://apiwaverelease:8080/api/WaveRelease/DesactivarWave";

                            //SetAuthorizationHeader(_apiWaveReleaseClient);
                            var httpClient = _httpClientFactory.CreateClient("apiWaveRelease");
                            SetAuthorizationHeader(httpClient);

                            var numOrden = orden.numOrden;
                            Console.WriteLine($"Desactivando wave para la orden: {numOrden}");

                            var waveURL = $"{DesactivarWave}/{numOrden}";
                            Console.WriteLine("URL: " + waveURL);

                            var response = await httpClient.PostAsync(waveURL, null);

                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Error al desactivar la wave de la orden {numOrden}. StatusCode: {response.StatusCode}");
                                return StatusCode((int)response.StatusCode, $"Error al desactivar la wave de la orden {numOrden}.");
                                
                            }

                            Console.WriteLine($"Wave de la orden {numOrden} desactivada correctamente.");
                        }
                        //Exception HTTP
                        catch (HttpRequestException httpEx)
                        {
                            Console.WriteLine($"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                            return StatusCode(500, $"Ocurrió un error HTTP al desactivar la wave de la orden {orden.numOrden}: {httpEx.Message}");
                        }
                        //Exception general
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                            return StatusCode(500, $"Ocurrió un error al desactivar la wave de la orden {orden.numOrden}: {ex.Message}");
                        }
                    }
                    _context.ordenesEnProceso.Update(orden);
                }

                // Guardar cambios a BD
                await _context.SaveChangesAsync();

                Console.WriteLine("EstadoLuca actualizado correctamente.");
                return Ok("EstadoLuca actualizado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al procesar las órdenes: " + ex.Message);
                return StatusCode(500, $"Ocurrió un error al procesar las órdenes: {ex.Message}");
            }

            /*
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
                    var jsonContent = JsonConvert.SerializeObject(request);
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
            */
        }
    }
}
