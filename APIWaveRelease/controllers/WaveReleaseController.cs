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
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<WaveReleaseController> _logger;

        public WaveReleaseController(WaveReleaseContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WaveReleaseController> logger)
        {
            _context = context;
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

        [HttpPost("EliminarCache")]
        public async Task<IActionResult> EliminarCache()
        {
            try
            {
                var waveCache = await _context.WaveReleaseCache.ToListAsync();

                if (!waveCache.Any())
                {
                    return Ok("La tabla WaveReleaseCache ya está vacía.");
                }

                _context.WaveReleaseCache.RemoveRange(waveCache);
                await _context.SaveChangesAsync();
                return Ok("Datos de WaveReleaseCache eliminados correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar datos de WaveReleaseCache: {ex.Message}");
                return StatusCode(500, "Error interno al eliminar los datos.");
            }
        }

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

        [HttpPost("ValidarUsuario")]
        [AllowAnonymous]
        public IActionResult ValidarUsuario([FromBody] UsuarioModel credenciales)
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

            return usuarioValido != null ? Ok() : Unauthorized();
        }



        [HttpPost]
        public async Task<IActionResult> PostOrderTransmission([FromBody] WaveReleaseKN waveReleaseKn)
        {
            // Validación de la entrada
            if (waveReleaseKn?.ORDER_TRANSMISSION?.ORDER_TRANS_SEG?.ORDER_SEG == null ||
                string.IsNullOrEmpty(waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat))
            {
                return BadRequest("Datos en formato no válido.");
            }

            // Paso 1: Obtener información de FamilyMaster
            int salidasDisponibles = 0;
            var httpClientFam = _httpClientFactory.CreateClient("apiFamilyMaster");
            SetAuthorizationHeader(httpClientFam);

            var urlConfirm = "http://apiorderconfirmation:8080/api/OrderConfirmation/ResetTandas";
            var resetList = await httpClientFam.PostAsync(urlConfirm, null);
            if (!resetList.IsSuccessStatusCode)
            {
                return StatusCode((int)resetList.StatusCode, "Error al resetear la lista de órdenes.");
            }

            var urlFamily = "http://apifamilymaster:8080/api/FamilyMaster/obtener-total-salidas";
            var respuesta = await httpClientFam.GetAsync(urlFamily);
            if (!respuesta.IsSuccessStatusCode)
            {
                return StatusCode((int)respuesta.StatusCode, "No hay FamilyMaster cargado o error al llamar a la API.");
            }

            var content = await respuesta.Content.ReadAsStringAsync();
            try
            {
                using JsonDocument jsonDocument = JsonDocument.Parse(content);
                if (jsonDocument.RootElement.TryGetProperty("totalSalidas", out JsonElement totalSalidasElement) &&
                    totalSalidasElement.TryGetInt32(out int totalSalidas))
                {
                    if (totalSalidas == 0)
                    {
                        return StatusCode((int)respuesta.StatusCode, "No hay FamilyMaster cargado.");
                    }
                    salidasDisponibles = totalSalidas;
                }
                else
                {
                    return StatusCode((int)respuesta.StatusCode, "El formato de la respuesta no es válido.");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Error al deserializar el JSON: {ex.Message}");
                return StatusCode(500, "Error al deserializar el JSON.");
            }

            // Paso 2: Operaciones en la BD dentro de una transacción incluyendo el envío a Luca
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Evitar guardar si hay órdenes activas
                    var ordenesActivas = await _context.WaveRelease.AnyAsync(wr => wr.estadoWave == true);
                    if (ordenesActivas)
                    {
                        return StatusCode(407, "Existen órdenes en proceso en estado activo (1). No se guardan datos.");
                    }

                    // Construir la lista de objetos WaveRelease a partir de la información recibida
                    var waveReleases = new List<WaveRelease>();

                    foreach (var orderSeg in waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG)
                    {
                        if (orderSeg?.SHIP_SEG?.PICK_DTL_SEG == null)
                        {
                            return BadRequest("El PICK_DTL_SEG viene null");
                        }

                        foreach (var pickDtlSeg in orderSeg.SHIP_SEG.PICK_DTL_SEG)
                        {
                            var existingWaveRelease = waveReleases
                                .FirstOrDefault(wr => wr.NumOrden == orderSeg.ordnum && wr.CodProducto == pickDtlSeg.prtnum);

                            if (existingWaveRelease != null)
                            {
                                existingWaveRelease.Cantidad += pickDtlSeg.qty;
                                _logger.LogInformation($"Cantidad actualizada para Orden: {orderSeg.ordnum}, Producto: {pickDtlSeg.prtnum}");
                            }
                            else
                            {
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
                                _logger.LogInformation($"Nuevo registro creado para Orden: {orderSeg.ordnum}, Producto: {pickDtlSeg.prtnum}");
                            }
                        }
                    }

                    // Guardar en la base de datos
                    _context.WaveRelease.AddRange(waveReleases);
                    await _context.SaveChangesAsync();

                    //// Envío de JSON a Luca dentro de la transacción
                    var httpClientLuca = _httpClientFactory.CreateClient("apiLuca");
                    SetAuthorizationHeader(httpClientLuca);
                    var jsonContent = JsonSerializer.Serialize(waveReleaseKn);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var urlLucaBase = _configuration["ServiceUrls:luca"];
                    var urlLuca = $"{urlLucaBase}/api/sort/waveRelease";

                    var responseLuca = await httpClientLuca.PostAsync(urlLuca, httpContent);
                    _logger.LogInformation("URL LUCA: " + urlLuca);

                    if (!responseLuca.IsSuccessStatusCode)
                    {
                        var errorDetails = await responseLuca.Content.ReadAsStringAsync();
                        _logger.LogError($"Error. Fallo al enviar JSON a LUCA. Status: {responseLuca.StatusCode}. Detalles: {errorDetails}");
                        throw new Exception($"Error. Fallo al enviar JSON a Luca. Status: {responseLuca.StatusCode}. Detalles: {errorDetails}");
                    }
                    _logger.LogInformation("El JSON fue enviado correctamente a Luca.");

                    // Si el envío a Luca es correcto, confirmar la transacción
                    await transaction.CommitAsync();

                }
                catch (HttpRequestException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error HTTP durante la transacción (envío a Luca fallido): {ex.Message}");
                    return StatusCode(500, $"Error HTTP durante la transacción (posible error de envío a Luca): {ex.Message}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error inesperado durante la transacción (envío a Luca fallido): {ex.Message}");
                    return StatusCode(500, $"Error inesperado durante la transacción: {ex.Message}");
                }
            }

            // Fuera de la transacción

            // Paso 3: Activar tandas
            try
            {
                var httpClient = _httpClientFactory.CreateClient("apiLuca");
                SetAuthorizationHeader(httpClient);
                var urlActivarTandas = "http://apifamilymaster:8080/api/FamilyMaster/activar-tandas";
                var responseTandas = await httpClient.PostAsync($"{urlActivarTandas}?salidasDisponibles={salidasDisponibles}", null);
                var responseContent = await responseTandas.Content.ReadAsStringAsync();
                _logger.LogInformation("Respuesta de activar-tandas: " + responseContent);

                var tandaResponse = JsonSerializer.Deserialize<ActivarTandasResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (tandaResponse == null)
                {
                    return Ok(new { Message = "La respuesta no contiene las propiedades esperadas.", TandasActivadas = new List<int>() });
                }

                return Ok(new { tandaResponse.Message, tandaResponse.TandasActivadas });
            }
            catch (JsonException ex)
            {
                _logger.LogError("Error al deserializar activar-tandas: " + ex.Message);
                return StatusCode(500, "Error al deserializar el JSON de activar-tandas.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Error en la solicitud de activar-tandas: " + ex.Message);
                return StatusCode(500, "Error en la solicitud HTTP de activar-tandas.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado en activar-tandas: " + ex.Message);
                return StatusCode(500, "Ocurrió un error inesperado en activar-tandas.");
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
            // Obtener datos existentes en la base de datos con clave de tipo tupla
            var existingCacheData = await _context.WaveReleaseCache
                .ToDictionaryAsync(x => (x.Ordnum, x.Prtnum));


            // Verificar que la Wave sea la misma
            var existingCache = existingCacheData.Values.FirstOrDefault();
            if (existingCache != null && existingCache.Schbat != waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat)
            {
                return BadRequest("La nueva Wave no coincide con los registros existentes en el cache.");
            }

            // Verificar que no haya prtfam nulos o vacíos
            foreach (var orden in waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG)
            {
                foreach (var shipSeg in orden.SHIP_SEG.PICK_DTL_SEG)
                {
                    if (string.IsNullOrWhiteSpace(shipSeg.prtfam))
                    {
                        return BadRequest($"Error: Familia de producto (prtfam) vacía o nula en ordnum {orden.ordnum} y prtnum {shipSeg.prtnum}");
                    }
                }
            }


            // *** VALIDACIÓN 1: Verificar que las familias (prtfam) existan en FamilyMaster ***
            var familiasRecibidas = waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG
                .SelectMany(orden => orden.SHIP_SEG.PICK_DTL_SEG
                    .Select(seg => seg.prtfam))
                .Where(prtfam => !string.IsNullOrWhiteSpace(prtfam))
                .Distinct()
                .ToList();

            var familiasExistentes = await _context.FamilyMaster
                .Select(f => f.Familia)
                .Distinct()
                .ToListAsync();

            var familiasNoEncontradas = familiasRecibidas
                .Where(fam => !familiasExistentes.Contains(fam))
                .ToList();

            if (familiasNoEncontradas.Any())
            {
                return BadRequest($"Las siguientes familias no existen en FamilyMaster: {string.Join(", ", familiasNoEncontradas)}");
            }



            // *** VALIDACIÓN 2: Verificar que las tiendas (rtcust/stcust) existan en FamilyMaster ***
            var tiendasRecibidas = waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG
                .SelectMany(orden => new[] { orden.rtcust, orden.stcust })
                .Where(tienda => !string.IsNullOrWhiteSpace(tienda))
                .Distinct()
                .ToList();

            // Traer todas las entidades de FamilyMaster primero y luego hacer las validaciones en memoria
            var todasLasTiendas = await _context.FamilyMaster.ToListAsync();

            // Extraer todas las tiendas válidas en memoria
            var tiendasExistentes = todasLasTiendas
                .SelectMany(f => new[] {
            f.Tienda1, f.Tienda2, f.Tienda3, f.Tienda4, f.Tienda5, f.Tienda6,
            f.Tienda7, f.Tienda8, f.Tienda9, f.Tienda10, f.Tienda11, f.Tienda12
                })
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            var tiendasNoEncontradas = tiendasRecibidas
                .Where(tienda => !tiendasExistentes.Contains(tienda))
                .ToList();

            if (tiendasNoEncontradas.Any())
            {
                return BadRequest($"Las siguientes tiendas no existen en FamilyMaster: {string.Join(", ", tiendasNoEncontradas)}");
            }


            // Diccionario para agrupar por ordnum y prtnum
            var groupedData = new Dictionary<(string ordnum, string prtnum), WaveReleaseCache>();

            foreach (var orden in waveReleaseKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG)
            {
                foreach (var shipSeg in orden.SHIP_SEG.PICK_DTL_SEG)
                {
                    var key = (orden.ordnum, shipSeg.prtnum);  // Clave en formato de tupla

                    // Verificar si ya existe en la BD
                    if (existingCacheData.TryGetValue(key, out var existingWaveCache))
                    {
                        // Si existe en la BD, actualizar cantidades
                        existingWaveCache.Qty += shipSeg.qty;
                        _context.WaveReleaseCache.Update(existingWaveCache);
                    }
                    else if (groupedData.TryGetValue(key, out var cachedWaveCache))
                    {
                        // Si ya existe en la agrupación temporal, sumar cantidades
                        cachedWaveCache.Qty += shipSeg.qty;
                    }
                    else
                    {
                        // Si no existe ni en la BD ni en la agrupación, crear nuevo registro
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

                        groupedData[key] = waveCache;
                    }
                }
            }

            // Guardar nuevos registros
            _context.WaveReleaseCache.AddRange(groupedData.Values);
            await _context.SaveChangesAsync();

            return Ok("Datos guardados correctamente en el cache.");
        }


        private async Task<IActionResult> EnviarPostEndpoint()
        {
            try
            {
                var waveCache = await _context.WaveReleaseCache.ToListAsync();

                if (!waveCache.Any())
                {
                    return BadRequest("No hay datos en cache para enviar.");
                }
                bool hayWaveActiva = await _context.WaveRelease.AnyAsync(wr => wr.estadoWave == true);
                if (hayWaveActiva)
                {
                    _logger.LogWarning("Intento de envío desde caché bloqueado: WaveRelease activa (estadoWave = 1).");
                    return Conflict("Error al enviar datos, WaveRelease activa");
                }
                _logger.LogInformation("No hay WaveRelease activa. Procediendo a enviar datos desde caché.");

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
                _logger.LogInformation($"Enviando datos a: {urlCache}");

                // Enviar datos al endpoint 'post'
                var response = await httpCliente.PostAsync(urlCache, httpContentCache);

                // Leer el contenido de la respuesta independientemente del status code
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Respuesta del servidor: Status {(int)response.StatusCode} - {response.StatusCode}");
                _logger.LogInformation($"Contenido de la respuesta: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Eliminando datos del cache");
                    _context.WaveReleaseCache.RemoveRange(waveCache);
                    await _context.SaveChangesAsync();

                    return Ok("Datos enviados correctamente.");
                }
                else
                {
                    // Capturar más detalles sobre el error HTTP
                    return StatusCode((int)response.StatusCode,
                        $"Error al enviar los datos. Status: {(int)response.StatusCode} {response.StatusCode}. " +
                        $"\nDetalle: {responseContent}" +
                        $"\nSE REALIZÓ UN ROLLBACK A LOS CAMBIOS HECHOS EN LA BD.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error de solicitud HTTP: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Error interno: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Error de conexión HTTP: {ex.Message}. Inner: {ex.InnerException?.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Error de serialización JSON: {ex.Message}");
                return StatusCode(500, $"Error al procesar JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción no controlada: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Error interno: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Error inesperado: {ex.Message}. Inner: {ex.InnerException?.Message}");
            }
        }

    }
}
