using APILPNPicking.data;
using APILPNPicking.models;
using APILPNPicking.services;
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
        private readonly ISenadServices _senadServices;


        public LpnPickingController(LPNPickingContext context, HttpClient httpClient, IConfiguration configuration, IHttpClientFactory httpClientFactory,ISenadServices senadServices)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _apiWaveReleaseClient = httpClientFactory.CreateClient("apiWaveRelease");
            _apiFamilyMasterClient = httpClientFactory.CreateClient("apiFamilyMaster");
            _senadServices = senadServices;
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

                // Consultar la API WaveRelease con el número de orden
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

                    // Preparar los datos para el servicio PkgWeight
                   // var senadRequest = new SenadRequest
                    //{
                      //  BillCode = request.SORT_INDUCTION.wcs_id,
                       // BoxCode = loadDtlSeg.prtnum,
                       // Weight = 1, // verificar si pueden ir en 0
                       // Length = 1,
                       // Width = 1,
                       // Height = 1,
                       // Rectangle = 1,
                       // OrgCode = "OrgCode", // Asigna el valor correcto
                       // WarehouseId = request.SORT_INDUCTION.wh_id, // Asigna el valor correcto
                       // ScanType = 1,
                        //DeviceId = "DeviceId", // Asigna el valor correcto
                        //SendTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                    //};

                    //var senadResponse = await _senadServices.SendPackageDataAsync(senadRequest);

                   // if (senadResponse.Status != 0)
                    //{
                      //  var errorMessage = $"Error en la respuesta de PkgWeight: {senadResponse.Message}";
                        //Console.WriteLine(errorMessage);
                        //return BadRequest(errorMessage);
                    //}

                    //Console.WriteLine("Datos de peso del paquete enviados correctamente.");

                    // Lógica de creación de nuevos registros en OrdenEnProceso si el código de producto es diferente
                    var existingOrden = _context.ordenesEnProceso
                        .FirstOrDefault(o => o.wave == waveRelease.wave && o.numOrden == waveRelease.numOrden && o.codProducto == loadDtlSeg.prtnum);

                    if (existingOrden == null)
                    {
                        Console.WriteLine("No se encontró un registro existente con el mismo código de producto. Creando nuevo objeto de OrdenEnProceso.");

                        var ordenEnProceso = new OrdenEnProceso
                        {
                            codMastr = waveRelease.codMastr ?? loadDtlSeg.prtnum,
                            codInr = waveRelease.codInr ?? loadDtlSeg.prtnum,
                            cantidad = waveRelease.cantidad,
                            cantInr = waveRelease.cantInr,
                            cantMastr = waveRelease.cantMastr,
                            cantidadLPN = loadDtlSeg.lod_cas_cnt,
                            cantidadProcesada = 0,
                            codProducto = waveRelease.codProducto,
                            dtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum,
                            estado = "Activo",//loadDtlSeg.lod_cas_cnt == waveRelease.cantidad ? "Cerrado" : "Activo",
                            familia = waveRelease.familia,
                            numOrden = waveRelease.numOrden,
                            numSalida = familyMaster.numSalida,
                            numTanda = familyMaster.numTanda,
                            tienda = waveRelease.tienda,
                            wave = waveRelease.wave
                        };

                        _context.ordenesEnProceso.Add(ordenEnProceso);
                    }

                    // LpnSorting: Mantener la inserción en la tabla LpnSorting
                    var lpnSorting = new LPNSorting
                    {
                        Wave = request.SORT_INDUCTION.wcs_id,
                        IdOrdenTrabajo = waveRelease.numOrden,
                        CantidadUnidades = loadDtlSeg.lod_cas_cnt,
                        CodProducto = loadDtlSeg.prtnum,
                        DtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum
                    };

                    _context.LPNSorting.Add(lpnSorting);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error procesando FamilyMaster: {ex.Message}");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error al procesar datos en FamilyMaster.");
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Procesamiento de LPN Picking completado.");
            return Ok("LPN Picking procesado correctamente.");
        }


    }
}
