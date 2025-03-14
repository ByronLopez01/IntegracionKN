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
        /*
        [HttpPost("EliminarCache")]
        public async Task<IActionResult> BorrarCache()
        {
            var waveCache = await _context.WaveReleaseCache.ToListAsync();
            
            if (!waveCache.Any())
                return BadRequest("No hay datos en el cache");

            _context.WaveReleaseCache.RemoveRange(waveCache);
            await _context.SaveChangesAsync();
            return Ok("Datos enviados correctamente.");
        }

        [HttpPost("EnviarCache")]
        public async Task<IActionResult> EnviarCache()
        {

            var resultado = await EnviarPostEndpoint();

            if (resultado is OkObjectResult)
            {
                Console.WriteLine("OK. Cargando datos desde el cache ");
                return Ok("se cargo desde el cache."); 
            }

            return Ok();
        }
        */
        [HttpPost("GuardarCache")]
        public async Task<IActionResult> GuardarCache([FromBody] WaveReleaseKN waveReleaseKN) 
        {
            var result = await GuardarWaveCache(waveReleaseKN);

            return result;

        }

        [HttpPost("EnviarCache")]
        public async Task<IActionResult> EnviarCache()
        {
            var result = await EnviarPostEndpoint();

            return result;
        }
        /*
        [HttpPost("PostCache")]
        public async Task<IActionResult> PostOrderCache([FromBody] WaveReleaseKN waveReleaseKn)
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
                    //var existingWaveRelease = waveReleases.FirstOrDefault(wr => wr.NumOrden == orderSeg.ordnum && wr.CodProducto == pickDtlSeg.prtnum);

                    // Buscar si existen Waves activas en la tabla
                    var existingWaveRelease = waveReleases
                        .FirstOrDefault(wr => wr.NumOrden == orderSeg.ordnum && wr.CodProducto == pickDtlSeg.prtnum);

                    if (existingWaveRelease != null)
                    {

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



                // Llamar al endpoint "activar-tandas"
                var urlActivarTandas = "http://apifamilymaster:8080/api/FamilyMaster/activar-tandas";
                var responseTandas = await httpClientFam.PostAsync($"{urlActivarTandas}?salidasDisponibles={salidasDisponibles}", null);


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
        */


        [HttpPost]
        public async Task<IActionResult> PostOrderTransmission([FromBody] WaveReleaseKN waveReleaseKn)
        {

            var ordenesActivas = await _context.WaveRelease.AnyAsync(wr => wr.estadoWave == true);

            if (ordenesActivas)
            {

                //guardar datos en tabla WaveReleaseCache 
               // await GuardarWaveCache(waveReleaseKn);

                return StatusCode(407, "Existen órdenes en proceso en estado activo (1). enviando al cache.");
            }
            /*  else
              {


                      var resultado = await EnviarPostEndpoint();

                      if (resultado is OkObjectResult)
                      {
                          Console.WriteLine("OK. Cargando datos desde el cache ");
                          await GuardarWaveCache(waveReleaseKn);
                          return Ok("se cargo desde el cache."); // Retornar un OK si la llamada fue exitosa
                      }

              }*/


            var urlConfirm = "http://apiorderconfirmation:8080/api/OrderConfirmation/ResetTandas";

            //int salidasDisponibles = 15;
            int salidasDisponibles = 0;

            var url = "http://apifamilymaster:8080/api/FamilyMaster/obtener-total-salidas";
            var httpClientFam = _httpClientFactory.CreateClient("apiFamilyMaster");
            SetAuthorizationHeader(httpClientFam);


            var resetList = await httpClientFam.PostAsync(urlConfirm, null);

            if (resetList.IsSuccessStatusCode)
            {
                Console.WriteLine("Lista de ordenes reseteada");
            }
            else
            {
                return StatusCode((int)resetList.StatusCode, "Error al resetear la lista de ordenes.");
            }


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

                        // existingWaveRelease.CantMastr = pickDtlSeg.qty_mscs;
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
            
            

            //var haytandasActivas = await _context.FamilyMaster.AnyAsync(fm => fm.estado == true);

            //if (!haytandasActivas)
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

                if (tandaResponse != null)
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


        private async Task<IActionResult> GuardarWaveCache(WaveReleaseKN waveReleaseKn)
        {
            // Verificar si ya existen datos en el cache
            var existingCache = await _context.WaveReleaseCache.AsNoTracking().FirstOrDefaultAsync();

            // Si existen datos y la Wave no coincide, rechazar.
            if (existingCache != null && existingCache.Schbat != waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat)
            {
                return BadRequest("La nueva Wave no coincide con el de los registros existentes en el cache.");
            }


            foreach (var orden in waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG)
            {
               
                foreach (var shipSeg in orden.SHIP_SEG.PICK_DTL_SEG)
                {
                    var waveCache = new WaveReleaseCache
                    {
                        WcsId = waveReleaseKn.ORDER_TRANSMISSION.wcs_id,
                        WhId = waveReleaseKn.ORDER_TRANSMISSION.wh_id,
                        MsgId = waveReleaseKn.ORDER_TRANSMISSION.msg_id,
                        Trandt = waveReleaseKn.ORDER_TRANSMISSION.trandt,
                        Schbat = waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat,
                        Ordnum = orden.ordnum,
                        Cponum = orden.cponum,
                        Rtcust = orden.rtcust,
                        Stcust = orden.stcust,
                        Ordtyp = orden.ordtyp,
                        Adrpsz = orden.ADDRESS_SEG?.adrpsz,
                        State = orden.ADDRESS_SEG?.state,
                        ShipId = orden.SHIP_SEG?.ship_id,
                        Carcod = orden.SHIP_SEG?.carcod,
                        Srvlvl = orden.SHIP_SEG?.srvlvl,
                        Wrkref = shipSeg.wrkref,
                        Prtnum = shipSeg.prtnum,
                        Prtfam = shipSeg.prtfam,
                        AltPrtnum = shipSeg.alt_prtnum,
                        MscsEan = shipSeg.mscs_ean,
                        IncsEan = shipSeg.incs_ean,
                        QtyMscs = shipSeg.qty_mscs,
                        QtyIncs = shipSeg.qty_incs,
                        Qty = shipSeg.qty,
                        OrdCasCnt = shipSeg.ord_cas_cnt,
                        Stgloc = shipSeg.stgloc,
                        MovZoneCode = shipSeg.mov_zone_code,
                        Conveyable = shipSeg.conveyable,
                        CubicVol = shipSeg.cubic_vol
                    };

                    
                    _context.WaveReleaseCache.Add(waveCache);
                }
            }

            // Guardar todos los cambios en la base de datos
            await _context.SaveChangesAsync();
            return Ok("Datos Guardados correctamente en el cache.");
        }

        private async Task<IActionResult> EnviarPostEndpoint()
        {
            var waveCache = await _context.WaveReleaseCache.ToListAsync();

            if (!waveCache.Any())
            {
                return BadRequest("No hay datos en cache para enviar.");
            }

            // Construcción del JSON esperado
            var waveReleaseKn = new WaveReleaseKN
            {
                ORDER_TRANSMISSION = new OrderTransmission
                {
                    wcs_id = waveCache.First().WcsId,
                    wh_id = waveCache.First().WhId,
                    msg_id = waveCache.First().MsgId,
                    trandt = waveCache.First().Trandt,
                    ORDER_TRANS_SEG = new OrderTransSeg
                    {
                        schbat = waveCache.First().Schbat,
                        ORDER_SEG = waveCache
                            .GroupBy(c => c.Ordnum)
                            .Select(orderGroup => new OrderSeg
                            {
                                ordnum = orderGroup.Key,
                                cponum = orderGroup.First().Cponum,
                                rtcust = orderGroup.First().Rtcust,
                                stcust = orderGroup.First().Stcust,
                                ordtyp = orderGroup.First().Ordtyp,
                                ADDRESS_SEG = new AddressSeg
                                {
                                    adrpsz = orderGroup.First().Adrpsz,
                                    state = orderGroup.First().State
                                },
                                SHIP_SEG = new ShipSeg
                                {
                                    ship_id = orderGroup.First().ShipId,
                                    carcod = orderGroup.First().Carcod,
                                    srvlvl = orderGroup.First().Srvlvl,
                                    PICK_DTL_SEG = orderGroup.Select(pick => new PickDtlSeg
                                    {
                                        wrkref = pick.Wrkref,
                                        prtnum = pick.Prtnum,
                                        prtfam = pick.Prtfam,
                                        alt_prtnum = pick.AltPrtnum,
                                        mscs_ean = pick.MscsEan,
                                        incs_ean = pick.IncsEan,
                                        qty_mscs = pick.QtyMscs,
                                        qty_incs = pick.QtyIncs,
                                        qty = pick.Qty,
                                        ord_cas_cnt = pick.OrdCasCnt,
                                        stgloc = pick.Stgloc,
                                        mov_zone_code = pick.MovZoneCode,
                                        conveyable = pick.Conveyable,
                                        cubic_vol = pick.CubicVol
                                    }).ToList()
                                }
                            }).ToList()
                    }
                }
            };

            var usuario = "senad";
            var contrasena = "S3nad";
            var basicAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{usuario}:{contrasena}"));

            var httpCliente = _httpClientFactory.CreateClient();
            httpCliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            var jsonContentCache = JsonSerializer.Serialize(waveReleaseKn);
            var httpContentCache = new StringContent(jsonContentCache, Encoding.UTF8, "application/json");

            var urlCache = "http://apiwaverelease:8080/api/Waverelease";

            // Enviar datos al endpoint 'post'
            var response = await httpCliente.PostAsync(urlCache, httpContentCache);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Eliminando datos del cache");
                _context.WaveReleaseCache.RemoveRange(waveCache);
                await _context.SaveChangesAsync();

                return Ok("Datos enviados correctamente.");
            }
            else
            {
                return StatusCode((int)response.StatusCode, "Error al enviar los datos.");
            }
        }
    }
}
