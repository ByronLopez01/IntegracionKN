using APILPNPicking.data;
using APILPNPicking.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text; // Import para el Encoding
using System.Net;
using System.Net.Http.Headers; // Asegúrate de importar este espacio de nombres
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace APILPNPicking.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public class LpnPickingController : ControllerBase
    {
        private readonly LPNPickingContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _apiWaveReleaseClient;
        private readonly HttpClient _apiFamilyMasterClient;
        private readonly ILogger<LpnPickingController> _logger;


        public LpnPickingController(
            LPNPickingContext context,
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<LpnPickingController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiWaveReleaseClient = httpClientFactory.CreateClient("apiWaveRelease");
            _apiFamilyMasterClient = httpClientFactory.CreateClient("apiFamilyMaster");
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

        [HttpPost]
        public async Task<IActionResult> PostLpnPicking([FromBody] LPNPickingKN request)
        {
            _logger.LogInformation("Inicio del procesamiento de LPN Picking");

            if (request?.SORT_INDUCTION?.LOAD_HDR_SEG == null || request.SORT_INDUCTION.LOAD_HDR_SEG.Count == 0)
            {
                _logger.LogError("Error. Datos en formato incorrecto.");
                return BadRequest("Error. Datos en formato incorrecto.");
            }


            // Verificar si existen dtlnum duplicados en los datos recibidos
            var allDtlNumbers = request.SORT_INDUCTION.LOAD_HDR_SEG
                                        .SelectMany(hdr => hdr.LOAD_DTL_SEG)
                                        .SelectMany(dtl => dtl.SUBNUM_SEG)
                                        .Select(sub => sub.dtlnum)
                                        .Where(dtlnum => !string.IsNullOrEmpty(dtlnum))
                                        .ToList();

            var duplicateDtl = allDtlNumbers
                                    .GroupBy(dtlnum => dtlnum)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => g.Key)
                                    .ToList();

            if (duplicateDtl.Any())
            {
                var msg = $"Error. Dtlnum (detail) duplicado en el json recibido: {string.Join(", ", duplicateDtl)}.";
                _logger.LogError(msg);
                return BadRequest(msg);
            }




            // Obtener la Wave activa actual
            var activeWave = await _context.WaveRelease
                                    .Where(w => w.estadoWave)
                                    .OrderBy(w => w.Id)
                                    .Select(w => w.Wave)
                                    .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(activeWave))
            {
                _logger.LogError("Error. No se encontró una Wave activa.");
                return BadRequest("No se encontró una Wave activa.");
            }

            _logger.LogInformation($"Wave activa actual: {activeWave}");


            // Extraer los dtlNumbers del request
            var dtlNumbers = request.SORT_INDUCTION.LOAD_HDR_SEG
                                .SelectMany(hdr => hdr.LOAD_DTL_SEG)
                                .SelectMany(dtl => dtl.SUBNUM_SEG)
                                .Select(sub => sub.dtlnum)
                                .Where(dtlnum => !string.IsNullOrEmpty(dtlnum))
                                .Distinct()
                                .ToList();

            // Verificar si alguno de los dtlNumbers ya existe en la tabla OrdenEnProceso para la Wave activa
            if (dtlNumbers.Any())
            {
                var existingNumbers = await _context.ordenesEnProceso
                    .Where(o => o.wave == activeWave && dtlNumbers.Contains(o.dtlNumber))
                    .Select(o => o.dtlNumber)
                    .Distinct()
                    .ToListAsync();

                if (existingNumbers.Count > 0)
                {
                    var msg = $"Error. El dtlnum '{string.Join(", ", existingNumbers)}' ya existe en la Wave activa ({activeWave}) de OrdenEnProceso.";
                    _logger.LogError(msg);
                    return BadRequest(msg);
                }
            }

            _logger.LogInformation("Datos válidos. Procesando el encabezado de carga.");


            foreach (var loadHdrSeg in request.SORT_INDUCTION.LOAD_HDR_SEG)
            {
                foreach (var loadDtlSeg in loadHdrSeg.LOAD_DTL_SEG)
                {
                    _logger.LogInformation("Procesando detalle de carga con orden: {Ordnum} y producto: {Prtnum}", loadDtlSeg.ordnum, loadDtlSeg.prtnum);

                    SetAuthorizationHeader(_apiWaveReleaseClient);
                    var waveReleaseResponseMessage = await _apiWaveReleaseClient.GetAsync($"api/WaveRelease/{loadDtlSeg.ordnum}");

                    if (!waveReleaseResponseMessage.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Procesando detalle de carga con orden: {Ordnum} y producto: {Prtnum}", loadDtlSeg.ordnum, loadDtlSeg.prtnum);

                        if (waveReleaseResponseMessage.StatusCode == HttpStatusCode.NotFound)
                        {
                            _logger.LogError("Error. La orden {Ordnum} no existe en WaveRelease.", loadDtlSeg.ordnum);
                            return NotFound($"Error. La orden {loadDtlSeg.ordnum} no existe en WaveRelease.");
                        }

                        _logger.LogError("Error. No se pudo obtener datos de WaveRelease. Código de estado: {StatusCode}", waveReleaseResponseMessage.StatusCode);
                        return StatusCode((int)waveReleaseResponseMessage.StatusCode, "Error. No se pudo obtener datos de WaveRelease.");
                    }

                    var waveReleaseResponse = await waveReleaseResponseMessage.Content.ReadAsStringAsync();
                    _logger.LogInformation("---------------------- Antes de la serialización ---------------------------");
                    List<WaveRelease> waveReleaseData = new List<WaveRelease>();

                    try
                    {
                        _logger.LogInformation("Respuesta de WaveRelease: {Response}", waveReleaseResponse);

                        waveReleaseData = JsonConvert.DeserializeObject<List<WaveRelease>>(waveReleaseResponse) ?? new List<WaveRelease>();
                        _logger.LogInformation("Deserialización exitosa. Cantidad de resultados: {Count}", waveReleaseData.Count);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError("Error. Fallo al deserializar la WaveRelease: {ExceptionMessage}", jsonEx.Message);
                        return StatusCode(500, "Error. Fallo al deserializar la WaveRelease.");
                    }

                    if (waveReleaseData == null || waveReleaseData.Count == 0)
                    {
                        _logger.LogError("Error. La orden {Ordnum} no existe en WaveRelease.", loadDtlSeg.ordnum);
                        return NotFound($"Error. La orden {loadDtlSeg.ordnum} no existe en WaveRelease.");
                    }

                    var waveRelease = waveReleaseData.FirstOrDefault(wr => wr.NumOrden == loadDtlSeg.ordnum && wr.CodProducto == loadDtlSeg.prtnum);

                    if (waveRelease == null)
                    {
                        _logger.LogInformation($"Error. No se encontró un WaveRelease coincidente para la orden {loadDtlSeg.ordnum} y producto {loadDtlSeg.prtnum}.");
                        return NotFound($"Error. No se encontró un WaveRelease coincidente para la orden {loadDtlSeg.ordnum} y producto {loadDtlSeg.prtnum}.");
                    }

                    // Consultar la API de FamilyMaster
                    _logger.LogInformation($"Consultando FamilyMaster para la tienda: {waveRelease.Tienda} y familia: {waveRelease.Familia}");

                    try
                    {
                        SetAuthorizationHeader(_apiFamilyMasterClient);

                        var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster?tienda={waveRelease.Tienda}&familia={waveRelease.Familia}";
                        _logger.LogInformation("URL FamilyMaster: " + urlFamilyMaster);

                        var familyMasterResponse = await _apiFamilyMasterClient.GetStringAsync(urlFamilyMaster);
                        _logger.LogInformation($"Respuesta de FamilyMaster: {familyMasterResponse}");

                        List<FamilyMaster> familyMasterData = new List<FamilyMaster>();

                        try
                        {
                            familyMasterData = JsonConvert.DeserializeObject<List<FamilyMaster>>(familyMasterResponse) ?? new List<FamilyMaster>();
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogInformation($"Error. Fallo en la deserialización de FamilyMaster: {jsonEx.Message}");
                            return StatusCode(500, "Error. Fallo en la deserialización de FamilyMaster.");
                        }

                        var familyMaster = familyMasterData.FirstOrDefault();

                        if (familyMaster == null)
                        {
                            _logger.LogError($"Error. No se encontró información en FamilyMaster para la tienda {waveRelease.Tienda} y familia {waveRelease.Familia}.");
                            return NotFound($"Error. No se encontró información en FamilyMaster para la tienda {waveRelease.Tienda} y familia {waveRelease.Familia}.");
                        }

                        if (loadDtlSeg.SUBNUM_SEG == null || loadDtlSeg.SUBNUM_SEG.Count == 0)
                        {
                            _logger.LogInformation("Error. No hay datos en SUBNUM_SEG.");
                            return BadRequest("Error. No hay datos en SUBNUM_SEG.");
                        }

                        foreach (var subnumSeg in loadDtlSeg.SUBNUM_SEG)
                        {
                            _logger.LogInformation($"Procesando SUBNUM_SEG con dtlnum: {subnumSeg.dtlnum} y subnum: {subnumSeg.subnum}");

                            var cantidadLPN = subnumSeg.untqty;

                            if (cantidadLPN > waveRelease.Cantidad)
                            {
                                var errorMessage = $"Error. La cantidad a procesar ({cantidadLPN}) excede la cantidad total ({waveRelease.Cantidad}) de la orden {waveRelease.NumOrden} y producto {waveRelease.CodProducto}.";
                                _logger.LogInformation(errorMessage);
                                return BadRequest(errorMessage);
                            }

                            // Verificar si la orden ya existe en la tabla OrdenesEnProceso
                            var existingOrden = _context.ordenesEnProceso
                                .FirstOrDefault(o => o.wave == waveRelease.Wave && o.numOrden == waveRelease.NumOrden && o.codProducto == waveRelease.CodProducto && o.dtlNumber == subnumSeg.dtlnum && o.subnum == subnumSeg.subnum);

                            if (existingOrden != null)
                            {
                                existingOrden.cantidadLPN += cantidadLPN;
                                _context.ordenesEnProceso.Update(existingOrden);
                            }
                            else
                            {
                                // Guardar la orden en proceso (si no existe, crearla)
                                var ordenEnProceso = new OrdenEnProceso
                                {
                                    codMastr = waveRelease.CodMastr,
                                    codInr = waveRelease.CodInr,
                                    cantidad = waveRelease.Cantidad,
                                    cantidadLPN = cantidadLPN,
                                    cantInr = waveRelease.CantInr,
                                    cantMastr = waveRelease.CantMastr,
                                    cantidadProcesada = 0,
                                    codProducto = loadDtlSeg.prtnum,
                                    dtlNumber = subnumSeg.dtlnum ?? string.Empty,
                                    subnum = subnumSeg.subnum ?? string.Empty,
                                    estado = true,
                                    familia = waveRelease.Familia,
                                    numOrden = waveRelease.NumOrden,
                                    wave = waveRelease.Wave,
                                    tienda = waveRelease.Tienda,
                                    numTanda = familyMaster.numTanda,
                                    numSalida = familyMaster.numSalida,
                                    estadoLuca = true
                                };
                                _context.ordenesEnProceso.Add(ordenEnProceso);
                            }

                            // Guardar LPNSorting
                            var lpnSorting = new LPNSorting
                            {
                                Wave = waveRelease.Wave,
                                IdOrdenTrabajo = waveRelease.NumOrden,
                                CantidadUnidades = cantidadLPN,
                                CodProducto = loadDtlSeg.prtnum,
                                DtlNumber = subnumSeg.dtlnum ?? string.Empty,
                                subnum = subnumSeg.subnum ?? string.Empty
                            };
                            _context.LPNSorting.Add(lpnSorting);

                            //FILTRO TEST.
                            if (cantidadLPN >= waveRelease.CantInr)
                            {
                                // CREACIÓN OBJETO JSON PARA ENVIO A LUCA
                                var lucaRequest = new LucaRequest
                                {
                                    codMastr = waveRelease.CodMastr,
                                    codInr = waveRelease.CodInr,
                                    cantMastr = waveRelease.CantMastr,
                                    cantInr = waveRelease.CantInr,
                                    cantidad = cantidadLPN,
                                    familia = waveRelease.Familia,
                                    numOrden = waveRelease.NumOrden,
                                    codProducto = waveRelease.CodProducto,
                                    onda = waveRelease.Wave,
                                    numSalida = familyMaster.numSalida,
                                    numTanda = familyMaster.numTanda,
                                    dtlNumber = subnumSeg.dtlnum ?? string.Empty,
                                    subnum = subnumSeg.subnum ?? string.Empty,
                                    tienda = waveRelease.Tienda
                                };


                                // ENVÍO DE JSON A LUCA REGISTRO POR REGISTRO
                                var jsonContent = JsonConvert.SerializeObject(lucaRequest);

                                _logger.LogInformation("JSON LUCA CREADO");
                                _logger.LogInformation(jsonContent);

                                var httpClient = _httpClientFactory.CreateClient("apiLuca");
                                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                                var urlLucaBase = _configuration["ServiceUrls:luca"];
                                var urlLuca = $"{urlLucaBase}/api/sort/LpnSorter?sorterId={familyMaster.numSalida}";
                                _logger.LogInformation("URL LUCA: " + urlLuca);


                                try
                                {
                                    var response = await httpClient.PostAsync(urlLuca, httpContent);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        _logger.LogInformation("Ok. El JSON fue enviado correctamente a LUCA.");
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Error. Fallo al enviar el JSON a LUCA.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Error. Fallo al enviar datos a LUCA: {ex.Message}");
                                    return StatusCode(500, $"Error. Fallo al enviar datos a LUCA: {ex.Message}");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"Registro con cantidadLPN {cantidadLPN} menor que cantInr {waveRelease.CantInr}. No se envía a LUCA.");
                            }
                            // FILTRO TEST.
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Datos almacenados correctamente en la BD.");

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error. Fallo al consultar FamilyMaster o enviar datos: {ex.Message}. InnerException: {ex.InnerException}");
                        return StatusCode(500, $"Error al procesar datos: {ex.Message}");
                    }
                }
            }

            _logger.LogInformation("Proceso de LPN completado.");
            return Ok("Proceso de LPN completado.");
        }

    }
}
