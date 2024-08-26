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

            if (request?.SORT_INDUCTION?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null || string.IsNullOrEmpty(request.SORT_INDUCTION.wcs_id))
            {
                Console.WriteLine("Datos en formato incorrecto.");
                return BadRequest("Datos en formato incorrecto.");
            }

            Console.WriteLine("Datos válidos. Procesando el encabezado de carga.");

            var loadHdrSeg = request.SORT_INDUCTION.LOAD_HDR_SEG;

            foreach (var loadDtlSeg in loadHdrSeg.LOAD_DTL_SEG)
            {
                Console.WriteLine($"Procesando detalle de carga con orden: {loadDtlSeg.ordnum} y producto: {loadDtlSeg.prtnum}");

                // Consultar la API WaveRelease con el wave y la orden
                SetAuthorizationHeader(_apiWaveReleaseClient);
                var waveReleaseResponseMessage = await _apiWaveReleaseClient.GetAsync($"api/WaveRelease/{loadDtlSeg.ordnum}");

                if (!waveReleaseResponseMessage.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al obtener datos de WaveRelease. Estado: {waveReleaseResponseMessage.StatusCode}");

                    if (waveReleaseResponseMessage.StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("Orden no registrada en WaveRelease. Omitiendo.");
                        continue;
                    }

                    return StatusCode((int)waveReleaseResponseMessage.StatusCode, "Error al obtener datos de WaveRelease.");
                }

                var waveReleaseResponse = await waveReleaseResponseMessage.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta de WaveRelease: {waveReleaseResponse}");

                var waveReleaseData = JsonConvert.DeserializeObject<List<WaveRelease>>(waveReleaseResponse);

                if (waveReleaseData == null || waveReleaseData.Count == 0)
                {
                    Console.WriteLine("No se encontraron datos para la combinación de orden en WaveRelease. Pasando al siguiente.");
                    continue;
                }

                // Filtrar por el producto después de obtener la respuesta completa
                var waveRelease = waveReleaseData.FirstOrDefault(wr => wr.wave == request.SORT_INDUCTION.wcs_id && wr.codProducto == loadDtlSeg.prtnum);

                if (waveRelease == null)
                {
                    Console.WriteLine("No se encontró un WaveRelease coincidente para el producto. Pasando al siguiente.");
                    continue;
                }

                // Consultar la API de FamilyMaster
                Console.WriteLine($"Consultando FamilyMaster para la familia: {waveRelease.familia}");

                try
                {
                    SetAuthorizationHeader(_apiFamilyMasterClient);
                    var familyMasterResponse = await _apiFamilyMasterClient.GetStringAsync($"api/FamilyMaster?tienda={waveRelease.tienda}");
                    Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse}");

                    var familyMasterData = JsonConvert.DeserializeObject<List<FamilyMaster>>(familyMasterResponse);
                    var familyMaster = familyMasterData.FirstOrDefault() ?? new FamilyMaster();

                    // Calcular cantidadLPN a partir de SUBNUM_SEG
                    var cantidadLPN = loadDtlSeg.SUBNUM_SEG
                        .Sum(subnumSeg => subnumSeg.untqty);

                    if (cantidadLPN > waveRelease.cantidad)
                    {
                        var errorMessage = $"La cantidad a procesar ({cantidadLPN}) excede la cantidad total ({waveRelease.cantidad}) de la orden {waveRelease.numOrden} y producto {waveRelease.codProducto}.";
                        Console.WriteLine(errorMessage);
                        return BadRequest(errorMessage);
                    }

                    // Buscar el registro de la orden existente
                    var existingOrden = _context.ordenesEnProceso
                        .FirstOrDefault(o => o.wave == waveRelease.wave && o.numOrden == waveRelease.numOrden && o.codProducto == waveRelease.codProducto);

                    if (existingOrden != null)
                    {
                        Console.WriteLine("Registro existente encontrado en OrdenEnProceso.");

                        // Sumar la cantidad de LPN
                        existingOrden.cantidadLPN += cantidadLPN;

                        _context.ordenesEnProceso.Update(existingOrden);
                    }
                    else
                    {
                        Console.WriteLine("No se encontró un registro existente. Creando nuevo objeto de OrdenEnProceso.");

                        //if (cantidadLPN > waveRelease.cantidad)
                        //{
                          //  var errorMessage = $"La cantidad a procesar ({cantidadLPN}) excede la cantidad total ({waveRelease.cantidad}) de la orden {waveRelease.numOrden} y producto {waveRelease.codProducto}.";
                            //Console.WriteLine(errorMessage);
                            //return BadRequest(errorMessage);
                        //}

                        var ordenEnProceso = new OrdenEnProceso
                        {
                            codMastr = waveRelease.codMastr ,
                            codInr = waveRelease.codInr ,
                            cantidad = waveRelease.cantidad,
                            cantidadLPN = cantidadLPN,
                            cantInr = waveRelease.cantInr,
                            cantMastr = waveRelease.cantMastr,
                            cantidadProcesada = 0,
                            codProducto = loadDtlSeg.prtnum,
                            dtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum,
                            estado = true, // Siempre activo para nuevos registros
                            familia = waveRelease.familia,
                            numOrden = waveRelease.numOrden,
                            wave = waveRelease.wave
                        };

                        _context.ordenesEnProceso.Add(ordenEnProceso);
                    }

                    // Guardar los cambios en la base de datos
                    await _context.SaveChangesAsync();

                    // Registrar en la tabla LPN Sorting
                    var lpnSorting = new LPNSorting
                    {
                        Wave = waveRelease.wave,
                        IdOrdenTrabajo = waveRelease.numOrden,
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

            return Ok("Proceso completado.");
        }


    }
}
