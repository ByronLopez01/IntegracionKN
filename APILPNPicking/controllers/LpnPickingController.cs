using APILPNPicking.data;
using APILPNPicking.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers; // Asegúrate de importar este espacio de nombres

namespace APILPNPicking.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LpnPickingController : ControllerBase
    {
        private readonly LPNPickingContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _apiWaveReleaseClient;
        private readonly HttpClient _apiFamilyMasterClient;


        public LpnPickingController(LPNPickingContext context, HttpClient httpClient, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _apiWaveReleaseClient = httpClientFactory.CreateClient("apiWaveRelease");
            _apiFamilyMasterClient = httpClientFactory.CreateClient("apiFamilyMaster");
        }

        private void SetAuthorizationHeader(HttpClient client)
        {
            var jwtToken = _configuration["Jwt:Token"]; // Obtén el token JWT de la configuración
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        }

        [HttpPost]
        public async Task<IActionResult> PostLpnPicking([FromBody] LPNPickingKN request)
        {
            Console.WriteLine("Inicio del procesamiento de LPN Picking");

            if (request?.SORT_INDUCTION?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null || string.IsNullOrEmpty(request.SORT_INDUCTION.LOAD_HDR_SEG.lodnum))
            {
                Console.WriteLine("Datos en formato incorrecto.");
                return BadRequest("Datos en formato incorrecto.");
            }

            Console.WriteLine("Datos válidos. Procesando el encabezado de carga.");
            var loadHdrSeg = request.SORT_INDUCTION.LOAD_HDR_SEG;

            foreach (var loadDtlSeg in loadHdrSeg.LOAD_DTL_SEG)
            {
                Console.WriteLine($"Procesando detalle de carga con orden: {loadDtlSeg.ordnum} y producto: {loadDtlSeg.prtnum}");

                // Consultar la API WaveRelease el numero de orden
                SetAuthorizationHeader(_apiWaveReleaseClient);
                var waveReleaseResponseMessage = await _apiWaveReleaseClient.GetAsync($"api/WaveRelease/{loadDtlSeg.ordnum}");

                if (!waveReleaseResponseMessage.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al obtener datos de WaveRelease. Estado: {waveReleaseResponseMessage.StatusCode}");

                    if (waveReleaseResponseMessage.StatusCode == HttpStatusCode.NotFound)
                    {
                        return NotFound($"Orden {loadDtlSeg.ordnum} no registrada en WaveRelease.");
                    }

                    return StatusCode((int)waveReleaseResponseMessage.StatusCode, "Error al obtener datos de WaveRelease.");
                }

                var waveReleaseResponse = await waveReleaseResponseMessage.Content.ReadAsStringAsync();
                Console.WriteLine("RESPUESTA JSON WAVERELEASE " + waveReleaseResponse);

                Console.WriteLine($"Respuesta de WaveRelease: {waveReleaseResponse}");
                Console.WriteLine("---------------------- Antes de la serializacion ---------------------------");
                List<WaveRelease> waveReleaseData = null; // Declarar la variable aquí

                try
                {

                    Console.WriteLine("Respuesta de WaveRelease: " + waveReleaseResponse);

                    // Deserializar la respuesta
                    waveReleaseData = JsonConvert.DeserializeObject<List<WaveRelease>>(waveReleaseResponse);
                    Console.WriteLine("Deserialización exitosa. Cantidad de resultados: " + waveReleaseData.Count);
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine("Error en la deserialización: " + jsonEx.Message);
                    return StatusCode(500, "Error en la deserialización de WaveRelease.");
                }
                Console.WriteLine("JSON WAVERELEASE " + waveReleaseData);

                if (waveReleaseData == null || waveReleaseData.Count == 0)
                {
                    return NotFound($"No se encontraron datos para la orden {loadDtlSeg.ordnum} en WaveRelease.");
                }

                // Filtrar por número de orden y código de producto
                var waveRelease = waveReleaseData.FirstOrDefault(wr => wr.NumOrden == loadDtlSeg.ordnum && wr.CodProducto == loadDtlSeg.prtnum);

                if (waveRelease == null)
                {
                    return NotFound($"No se encontró un WaveRelease coincidente para la orden {loadDtlSeg.ordnum} y producto {loadDtlSeg.prtnum}.");
                }

                // Consultar la API de FamilyMaster
                Console.WriteLine($"Consultando FamilyMaster para la tienda: {waveRelease.Tienda} y familia: {waveRelease.Familia}");

                try
                {
                    SetAuthorizationHeader(_apiFamilyMasterClient);
                    // Modificar la consulta para incluir la familia
                    var familyMasterResponse = await _apiFamilyMasterClient.GetStringAsync($"api/FamilyMaster?tienda={waveRelease.Tienda}&familia={waveRelease.Familia}");
                    Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse}");
                    Console.WriteLine($"Respuesta de FamilyMaster: familyMasterResponse");

                    var familyMasterData = JsonConvert.DeserializeObject<List<FamilyMaster>>(familyMasterResponse);
                    var familyMaster = familyMasterData.FirstOrDefault();

                    if (familyMaster == null)
                    {
                        return NotFound($"No se encontró información en FamilyMaster para la tienda {waveRelease.Tienda} y familia {waveRelease.Familia}.");
                    }

                    var cantidadLPN = loadDtlSeg.SUBNUM_SEG.Sum(subnumSeg => subnumSeg.untqty);

                    if (cantidadLPN > waveRelease.Cantidad)
                    {
                        var errorMessage = $"La cantidad a procesar ({cantidadLPN}) excede la cantidad total ({waveRelease.Cantidad}) de la orden {waveRelease.NumOrden} y producto {waveRelease.CodProducto}.";
                        Console.WriteLine(errorMessage);
                        return BadRequest(errorMessage);
                    }

                    var existingOrden = _context.ordenesEnProceso
                        .FirstOrDefault(o => o.wave == waveRelease.Wave && o.numOrden == waveRelease.NumOrden && o.codProducto == waveRelease.CodProducto);

                    if (existingOrden != null)
                    {
                        existingOrden.cantidadLPN += cantidadLPN;

                        _context.ordenesEnProceso.Update(existingOrden);
                    }
                    else
                    {
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
                            dtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum,
                            estado = true,
                            familia = waveRelease.Familia,
                            numOrden = waveRelease.NumOrden,
                            wave = waveRelease.Wave,
                            tienda = waveRelease.Tienda,
                            numTanda = familyMaster.numTanda

                        };

                        _context.ordenesEnProceso.Add(ordenEnProceso);
                    }

                    await _context.SaveChangesAsync();

                    var lpnSorting = new LPNSorting
                    {
                        Wave = waveRelease.Wave,
                        IdOrdenTrabajo = waveRelease.NumOrden,
                        CantidadUnidades = cantidadLPN,
                        CodProducto = loadDtlSeg.prtnum,
                        DtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum ?? string.Empty
                    };

                    _context.LPNSorting.Add(lpnSorting);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Datos almacenados correctamente en la tabla LpnSorting.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al consultar FamilyMaster o enviar datos: {ex.Message}");
                    return StatusCode(500, "Error al consultar FamilyMaster o enviar datos.");
                }
            }
            return Ok("PRoceso Completado");
        }

    }
}
