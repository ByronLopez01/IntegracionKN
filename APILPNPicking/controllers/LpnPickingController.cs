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


        public LpnPickingController(LPNPickingContext context, HttpClient httpClient, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiWaveReleaseClient = httpClientFactory.CreateClient("apiWaveRelease");
            _apiFamilyMasterClient = httpClientFactory.CreateClient("apiFamilyMaster");
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
            Console.WriteLine("Inicio del procesamiento de LPN Picking");

            if (request?.SORT_INDUCTION?.LOAD_HDR_SEG == null || request.SORT_INDUCTION.LOAD_HDR_SEG.Count == 0)
            {
                Console.WriteLine("Datos en formato incorrecto.");
                return BadRequest("Datos en formato incorrecto.");
            }

            Console.WriteLine("Datos válidos. Procesando el encabezado de carga.");

            foreach (var loadHdrSeg in request.SORT_INDUCTION.LOAD_HDR_SEG)
            {
                foreach (var loadDtlSeg in loadHdrSeg.LOAD_DTL_SEG)
                {
                    Console.WriteLine($"Procesando detalle de carga con orden: {loadDtlSeg.ordnum} y producto: {loadDtlSeg.prtnum}");

                    SetAuthorizationHeader(_apiWaveReleaseClient);
                    var waveReleaseResponseMessage = await _apiWaveReleaseClient.GetAsync($"api/WaveRelease/{loadDtlSeg.ordnum}");

                    if (!waveReleaseResponseMessage.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error al obtener datos de WaveRelease. Estado: {waveReleaseResponseMessage.StatusCode}");

                        if (waveReleaseResponseMessage.StatusCode == HttpStatusCode.NotFound)
                        {
                            Console.WriteLine($"La orden {loadDtlSeg.ordnum} no existe en WaveRelease.");
                            return NotFound($"La orden {loadDtlSeg.ordnum} no existe en WaveRelease.");
                        }

                        return StatusCode((int)waveReleaseResponseMessage.StatusCode, "Error al obtener datos de WaveRelease.");
                    }

                    var waveReleaseResponse = await waveReleaseResponseMessage.Content.ReadAsStringAsync();
                    Console.WriteLine("---------------------- Antes de la serialización ---------------------------");
                    List<WaveRelease> waveReleaseData = new List<WaveRelease>();

                    try
                    {
                        Console.WriteLine("Respuesta de WaveRelease: " + waveReleaseResponse);

                        waveReleaseData = JsonConvert.DeserializeObject<List<WaveRelease>>(waveReleaseResponse) ?? new List<WaveRelease>();
                        Console.WriteLine("Deserialización exitosa. Cantidad de resultados: " + waveReleaseData.Count);
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine("Error en la deserialización: " + jsonEx.Message);
                        return StatusCode(500, "Error en la deserialización de WaveRelease.");
                    }

                    if (waveReleaseData == null || waveReleaseData.Count == 0)
                    {
                        Console.WriteLine($"La orden {loadDtlSeg.ordnum} no existe en WaveRelease.");
                        return NotFound($"La orden {loadDtlSeg.ordnum} no existe en WaveRelease.");
                    }

                    var waveRelease = waveReleaseData.FirstOrDefault(wr => wr.NumOrden == loadDtlSeg.ordnum && wr.CodProducto == loadDtlSeg.prtnum);

                    if (waveRelease == null)
                    {
                        Console.WriteLine($"No se encontró un WaveRelease coincidente para la orden {loadDtlSeg.ordnum} y producto {loadDtlSeg.prtnum}.");
                        return NotFound($"No se encontró un WaveRelease coincidente para la orden {loadDtlSeg.ordnum} y producto {loadDtlSeg.prtnum}.");
                    }

                    // Consultar la API de FamilyMaster
                    Console.WriteLine($"Consultando FamilyMaster para la tienda: {waveRelease.Tienda} y familia: {waveRelease.Familia}");

                    try
                    {
                        SetAuthorizationHeader(_apiFamilyMasterClient);

                        var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster?tienda={waveRelease.Tienda}&familia={waveRelease.Familia}";
                        Console.WriteLine("URL FamilyMaster: " + urlFamilyMaster);

                        var familyMasterResponse = await _apiFamilyMasterClient.GetStringAsync(urlFamilyMaster);
                        Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse}");

                        List<FamilyMaster> familyMasterData = new List<FamilyMaster>();

                        try
                        {
                            familyMasterData = JsonConvert.DeserializeObject<List<FamilyMaster>>(familyMasterResponse) ?? new List<FamilyMaster>();
                        }
                        catch (JsonException jsonEx)
                        {
                            Console.WriteLine($"Error en la deserialización de FamilyMaster: {jsonEx.Message}");
                            return StatusCode(500, "Error en la deserialización de FamilyMaster.");
                        }

                        var familyMaster = familyMasterData.FirstOrDefault();

                        if (familyMaster == null)
                        {
                            return NotFound($"No se encontró información en FamilyMaster para la tienda {waveRelease.Tienda} y familia {waveRelease.Familia}.");
                        }

                        if (loadDtlSeg.SUBNUM_SEG == null || loadDtlSeg.SUBNUM_SEG.Count == 0)
                        {
                            Console.WriteLine("No hay datos en SUBNUM_SEG.");
                            return BadRequest("No hay datos en SUBNUM_SEG.");
                        }

                        foreach (var subnumSeg in loadDtlSeg.SUBNUM_SEG)
                        {
                            Console.WriteLine($"Procesando SUBNUM_SEG con dtlnum: {subnumSeg.dtlnum} y subnum: {subnumSeg.subnum}");

                            var cantidadLPN = subnumSeg.untqty;

                            if (cantidadLPN > waveRelease.Cantidad)
                            {
                                var errorMessage = $"La cantidad a procesar ({cantidadLPN}) excede la cantidad total ({waveRelease.Cantidad}) de la orden {waveRelease.NumOrden} y producto {waveRelease.CodProducto}.";
                                Console.WriteLine(errorMessage);
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

                            Console.WriteLine("JSON LUCA:");
                            Console.WriteLine(jsonContent);

                            var httpClient = _httpClientFactory.CreateClient("apiLuca");
                            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                            var urlLucaBase = _configuration["ServiceUrls:luca"];
                            var urlLuca = $"{urlLucaBase}/api/sort/LpnSorter?sorterId={familyMaster.numSalida}";

                            try
                            {
                                var response = await httpClient.PostAsync(urlLuca, httpContent);
                                if (response.IsSuccessStatusCode)
                                {
                                    Console.WriteLine("El JSON fue enviado correctamente a LpnSorter.");
                                }
                                else
                                {
                                    Console.WriteLine("Error al enviar el JSON a LpnSorter.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error al enviar datos a LpnSorter: {ex.Message}");
                                return StatusCode(500, $"Error al enviar datos a LpnSorter: {ex.Message}");
                            }
                            
                        }

                        await _context.SaveChangesAsync();
                        Console.WriteLine("Datos almacenados correctamente en la tabla LPNSorting.");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al consultar FamilyMaster o enviar datos: {ex.Message}");
                        return StatusCode(500, $"Error al procesar datos: {ex.Message}");
                    }
                }
            }

            Console.WriteLine("Procesamiento completado.");
            return Ok("Proceso de LPN completado.");
        }

    }
}
