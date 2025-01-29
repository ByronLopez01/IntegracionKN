using APIWaveRelease.data;
using APIWaveRelease.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIWaveRelease.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public class WaveReleaseController : ControllerBase
    {
        private readonly WaveReleaseContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
     
        

        public WaveReleaseController(WaveReleaseContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
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

        [HttpPost]
        public async Task<IActionResult> PostOrderTransmission([FromBody] WaveReleaseKN waveReleaseKn)
        {

            // Buscar si existen Waves activas en la tabla
            var ordenesActivas = await _context.WaveRelease.AnyAsync(wr => wr.estadoWave == true);

            if (ordenesActivas)
            {
                // Si existe
                return StatusCode(407, "Existen órdenes en proceso en estado activo (1)\n No se puede agregar otra Wave");
            }

            //int salidasDisponibles = 15;
            int salidasDisponibles = 0;

            var url = "http://apifamilymaster:8080/api/FamilyMaster/obtener-total-salidas";
            var httpClientFam = _httpClientFactory.CreateClient("apiFamilyMaster");
            SetAuthorizationHeader(httpClientFam);

            var respuesta = await httpClientFam.GetAsync(url);

            if (respuesta.IsSuccessStatusCode)
            {
                var content = await respuesta.Content.ReadAsStringAsync();
                try
                {
                    var jsonDocument = JsonDocument.Parse(content);
                    if (jsonDocument.RootElement.TryGetProperty("totalSalidas", out JsonElement totalSalidasElement) && totalSalidasElement.TryGetInt32(out int totalSalidas))
                    {
                        if (totalSalidas == 0)
                        {
                            return StatusCode((int)respuesta.StatusCode, "No hay FamilyMaster cargado o hubo un error al llamar a la API.");
                        }

                        if (totalSalidas > 0)
                        {
                            salidasDisponibles = totalSalidas;
                            Console.WriteLine($"Salidas disponibles: {salidasDisponibles}");
                            Console.WriteLine($"Salidas disponibles: {salidasDisponibles}");
                            Console.WriteLine($"Salidas disponibles: {salidasDisponibles}");
                            Console.WriteLine($"Salidas disponibles: {salidasDisponibles}");
                        }
                    }
                    else
                    {
                        return StatusCode((int)respuesta.StatusCode, "El formato de la respuesta no es válido.");
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error al deserializar el JSON: {ex.Message}");
                    return StatusCode(500, "Error al deserializar el JSON.");
                }

            }
            else
            {
                return StatusCode((int)respuesta.StatusCode, "No hay FamilyMaster cargado o hubo un error al llamar a la API.");
            }


            if (waveReleaseKn?.ORDER_TRANSMISSION?.ORDER_TRANS_SEG?.ORDER_SEG == null || string.IsNullOrEmpty(waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat))
            {
                return BadRequest("Datos en formato no válido.");
            }

            var waveReleases = new List<WaveRelease>();

            // Itera sobre cada ORDER_SEG en la lista
            foreach (var orderSeg in waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG)
            {
                if (orderSeg?.SHIP_SEG?.PICK_DTL_SEG == null)
                {
                    return BadRequest("El PICK_DTL_SEG viene null");
                }

                foreach (var pickDtlSeg in orderSeg.SHIP_SEG.PICK_DTL_SEG)
                {

                    // Busca si ya existe un WaveRelease con el mismo número de orden y producto
                    var existingWaveRelease = waveReleases
                        .FirstOrDefault(wr => wr.NumOrden == orderSeg.ordnum && wr.CodProducto == pickDtlSeg.prtnum);

                    if (existingWaveRelease != null)
                    {

                        //existingWaveRelease.CantMastr = pickDtlSeg.qty_mscs;
                        //existingWaveRelease.CantInr = pickDtlSeg.qty_incs;
                        existingWaveRelease.Cantidad += pickDtlSeg.qty;

                        // Mensaje de depuración para indicar que se ha encontrado y actualizado un registro existente
                        Console.WriteLine($"Cantidad actualizada para Orden: {orderSeg.ordnum}, Producto: {pickDtlSeg.prtnum}");
                    }
                    else
                    {
                        // Si no existe, crea un nuevo registro
                        var newWaveRelease = new WaveRelease
                        {
                            CodMastr = pickDtlSeg.mscs_ean,
                            CodInr = pickDtlSeg.incs_ean,
                            CantMastr = pickDtlSeg.qty_mscs,
                            CantInr = pickDtlSeg.qty_incs,
                            Cantidad = pickDtlSeg.qty,
                            Familia = pickDtlSeg.prtfam,
                            NumOrden = orderSeg.ordnum,
                            CodProducto = pickDtlSeg.prtnum,
                            Wave = waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat,
                            tienda = orderSeg.rtcust,
                            estadoWave = true
                        };
                        
                        waveReleases.Add(newWaveRelease);

                        // Mensaje de depuración para indicar que se ha creado un nuevo registro
                        Console.WriteLine($"Nuevo registro creado para Orden: {orderSeg.ordnum}, Producto: {pickDtlSeg.prtnum}");
                    }
                }
            }

            _context.WaveRelease.AddRange(waveReleases);
            await _context.SaveChangesAsync();


            // ENVIO DE JSON A LUCA!!
            var jsonContent = JsonSerializer.Serialize(waveReleaseKn);
            var httpClient = _httpClientFactory.CreateClient("apiLuca");
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            SetAuthorizationHeader(httpClient);

            var urlLucaBase = _configuration["ServiceUrls:luca"];
            var urlLuca = $"{urlLucaBase}/api/sort/waveRelease";
            
            try
            {
                var response = await httpClient.PostAsync(urlLuca, httpContent);
                Console.WriteLine("URL LUCA: " + urlLuca);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("El JSON fue enviado correctamente a Luca.");
                }
                else
                {
                    Console.WriteLine("Error al enviar el JSON a Luca.");
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

            var haytandasActivas = await _context.FamilyMaster.AnyAsync(fm => fm.estado == true);

            if (!haytandasActivas)
            {


                // Llamar al endpoint "activar-tandas"
                var urlActivarTandas = "http://apifamilymaster:8080/api/FamilyMaster/activar-tandas";
                var responseTandas = await httpClient.PostAsync($"{urlActivarTandas}?salidasDisponibles={salidasDisponibles}", null);


                try
                {
                    var responseContent = await responseTandas.Content.ReadAsStringAsync();
                    Console.WriteLine("Respuesta JSON recibida: " + responseContent);

                    // Deserializa el JSON a la clase ActivarTandasResponse
                    var tandaResponse = JsonSerializer.Deserialize<ActivarTandasResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Por si las propiedades tienen diferente casing
                    });

                    if (tandaResponse != null && tandaResponse.TandasActivadas != null)
                    {
                        Console.WriteLine($"Mensaje: {tandaResponse.Message}");
                        Console.WriteLine($"Tandas activadas: {string.Join(", ", tandaResponse.TandasActivadas)}");

                        // Devuelve el mensaje y las tandas activadas en la respuesta
                        return Ok(new { tandaResponse.Message, tandaResponse.TandasActivadas });

                    }
                    else
                    {
                        Console.WriteLine("La respuesta no contiene las propiedades esperadas.");

                        return Ok(new { Message = "La respuesta no contiene las propiedades esperadas.", TandasActivadas = new List<int>() });
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("Error al deserializar el JSON: " + ex.Message);
                    return StatusCode(500, "Error al deserializar el JSON.");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("Error en la solicitud HTTP: " + ex.Message);
                    return StatusCode(500, "Error en la solicitud HTTP.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ocurrió un error inesperado: " + ex.Message);
                    return StatusCode(500, "Ocurrió un error inesperado.");
                }
            }
            else
            {

                return Ok("Se recibió correctamente la Wave.\n Existian tandas activas y no se volvieron a activar.");
            }
        }


        [HttpGet("{idOrdenTrabajo}")]
        public async Task<IActionResult> GetWaveByIdOrdenTrabajo(string idOrdenTrabajo)
        {
            var waveReleases = await _context.WaveRelease
                .Where(w => w.NumOrden == idOrdenTrabajo)
                .ToListAsync();

            if (waveReleases == null || waveReleases.Count == 0)
            {
                return NotFound($"Orden no registrada en la wave {idOrdenTrabajo}");
            }

            return Ok(waveReleases);
        }

        // POST DesactivarWave
        [HttpPost("DesactivarWave/{numOrden}/{codProducto}")]
        public async Task<IActionResult> DesactivarWave(string numOrden, string codProducto)
        {
            var waveRelease = await _context.WaveRelease
                .FirstOrDefaultAsync(wr => wr.NumOrden == numOrden && wr.CodProducto == codProducto && wr.estadoWave == true);

            if (waveRelease == null)
            {
                return NotFound($"No se encontró una wave asociada a la orden {numOrden} y producto {codProducto}");
            }

            waveRelease.estadoWave = false; // Cambiar el estado a procesado
            _context.WaveRelease.Update(waveRelease);
            await _context.SaveChangesAsync();

            return Ok($"El estado de la wave asociada a la orden {numOrden} y producto {codProducto} ha sido actualizado a procesado.");
        }


        // PUT api/<WaveReleaseController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }


        // DELETE api/<WaveReleaseController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
