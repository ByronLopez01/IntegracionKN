using APILPNPicking.data;
using APILPNPicking.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
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

            var lpn_rechazados = new List<LpnRechazado>();


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
                .AsNoTracking()
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


            // Obtener los dtlNumbers ya existentes en la Base de Datos.
            var existingNumbers = new HashSet<string>();
            if (dtlNumbers.Count() != 0)
            {
                existingNumbers = _context.ordenesEnProceso
                    .Where(o => o.wave == activeWave && dtlNumbers.Contains(o.dtlNumber))
                    .AsNoTracking()
                    .Select(o => o.dtlNumber)
                    .Distinct()
                    .ToHashSet();
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
                        //_logger.LogInformation("Respuesta de WaveRelease: {Response}", waveReleaseResponse);

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
                        _logger.LogError($"Error. No se encontró un WaveRelease coincidente para la orden {loadDtlSeg.ordnum} y producto {loadDtlSeg.prtnum}.");
                        return NotFound($"Error. No se encontró un WaveRelease coincidente para la orden {loadDtlSeg.ordnum} y producto {loadDtlSeg.prtnum}.");
                    }


                    // Verificar que la Wave de la orden está activa en WaveRelease
                    if (waveRelease.EstadoWave == false)
                    {
                        var razonRechazo = $"La familia {waveRelease.Familia} de la orden {loadDtlSeg.ordnum} está inactiva (estadoWave = 0).";
                        _logger.LogInformation(razonRechazo + " Rechazando dtlnum correspondientes.");

                        // Se rechaza cada dtlnum asociado al detalle
                        if (loadDtlSeg.SUBNUM_SEG != null)
                        {
                            foreach (var subnumSeg in loadDtlSeg.SUBNUM_SEG)
                            {
                                if (!string.IsNullOrEmpty(subnumSeg.dtlnum))
                                {
                                    _logger.LogInformation($"LPN rechazado: {subnumSeg.dtlnum} - {razonRechazo}");
                                    lpn_rechazados.Add(new LpnRechazado(subnumSeg.dtlnum, razonRechazo));
                                }
                            }
                        }
                        continue; // Continuar con el siguiente detalle de carga si la Wave está inactiva
                    }



                    // Consultar la API de FamilyMaster
                    _logger.LogInformation($"Consultando FamilyMaster para la tienda: {waveRelease.Tienda} y familia: {waveRelease.Familia}");


                    string? familyMasterResponse = null;
                    try
                    {
                        SetAuthorizationHeader(_apiFamilyMasterClient);

                        var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster?tienda={waveRelease.Tienda}&familia={waveRelease.Familia}";
                        _logger.LogInformation("URL FamilyMaster: " + urlFamilyMaster);

                        familyMasterResponse = await _apiFamilyMasterClient.GetStringAsync(urlFamilyMaster);
                        _logger.LogInformation($"Respuesta de FamilyMaster: {familyMasterResponse}");

                        //List<FamilyMaster> familyMasterData = new List<FamilyMaster>();
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _logger.LogError($"Error. Fallo HTTP al consultar FamilyMaster: {httpEx.Message}");
                        return StatusCode(500, $"Error. Fallo HTTP al consultar FamilyMaster: {httpEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error. Fallo al consultar FamilyMaster: {ex.Message}");
                        return StatusCode(500, $"Error. Fallo al consultar FamilyMaster: {ex.Message}");
                    }


                    List<FamilyMaster>? familyMasterData = null;
                    try
                    {
                        familyMasterData = JsonConvert.DeserializeObject<List<FamilyMaster>>(familyMasterResponse) ?? new List<FamilyMaster>();
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError($"Error. Fallo en la deserialización de FamilyMaster: {jsonEx.Message}");
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
                        _logger.LogError("Error. No hay datos en SUBNUM_SEG.");
                        return BadRequest("Error. No hay datos en SUBNUM_SEG.");
                    }

                    foreach (var subnumSeg in loadDtlSeg.SUBNUM_SEG)
                    {
                        _logger.LogInformation($"Procesando SUBNUM_SEG con dtlnum: {subnumSeg.dtlnum} y subnum: {subnumSeg.subnum}");

                        // INICIO DE LA TRANSACCIÓN
                        _logger.LogInformation("Iniciando transacción en BD para dtlnum {Dtlnum}", subnumSeg.dtlnum);
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try 
                        {
                            // Verificar si el dtlnum ya existe en la base de datos (DTLNUM DUPLICADO)
                            if (!string.IsNullOrEmpty(subnumSeg.dtlnum))
                            {
                                var isDuplicate = await _context.ordenesEnProceso
                                    .AnyAsync(o => o.wave == activeWave && o.dtlNumber == subnumSeg.dtlnum);

                                if (isDuplicate || existingNumbers.Contains(subnumSeg.dtlnum))
                                {
                                    var razonRechazo = $"Dtlnum '{subnumSeg.dtlnum}' duplicado ya que existe en la Wave activa ({activeWave}).";
                                    _logger.LogWarning($"LPN rechazado: {razonRechazo}");
                                    lpn_rechazados.Add(new LpnRechazado(subnumSeg.dtlnum, razonRechazo));
                                    await transaction.RollbackAsync(); // Revertir la transacción iniciada
                                    continue; // Saltar este dtlnum y seguir con el siguiente
                                }
                            }

                            var cantidadLPN = subnumSeg.untqty;


                            // 1. Obtener la cantidad ya registrada para la orden y producto en la wave activa
                            var cantidadRegistrada = _context.ordenesEnProceso
                                .Where(o => o.wave == waveRelease.Wave
                                    && o.numOrden == waveRelease.NumOrden
                                    && o.codProducto == waveRelease.CodProducto)
                                .AsNoTracking()
                                .Sum(o => o.cantidadLPN);

                            _logger.LogInformation(
                                "Verificación de cantidad para LPN: dtlnum={Dtlnum}, numOrden={NumOrden}, codProducto={CodProducto}, wave={Wave}. " +
                                "Cantidad permitida en orden: {CantidadOrden}, Cantidad ya registrada: {CantidadRegistrada}, Cantidad recibida: {CantidadLPN}, Total después de agregar: {Total}",
                                subnumSeg.dtlnum, waveRelease.NumOrden, waveRelease.CodProducto, waveRelease.Wave,
                                waveRelease.Cantidad, cantidadRegistrada, cantidadLPN, cantidadRegistrada + cantidadLPN
                            );

                            // 2. Verificar si la suma de lo registrado + lo recibido excede la cantidad de la orden
                            if (cantidadRegistrada + cantidadLPN > waveRelease.Cantidad)
                            {
                                var razonRechazo = $"Dtlnum ({subnumSeg.dtlnum}) excede la cantidad total ({waveRelease.Cantidad}) para la orden {waveRelease.NumOrden} y producto {waveRelease.CodProducto}.";
                                _logger.LogWarning($"LPN rechazado: {razonRechazo}");
                                if (!string.IsNullOrEmpty(subnumSeg.dtlnum))
                                    lpn_rechazados.Add(new LpnRechazado(subnumSeg.dtlnum, razonRechazo));
                                // Continúa con el siguiente LPN sin agregar este
                                await transaction.RollbackAsync();
                                continue;
                            }
                            else
                            {
                                _logger.LogInformation("LPN aceptado: Total después de agregar: {Total}", cantidadRegistrada + cantidadLPN);
                            }

                            /*
                            if (cantidadLPN > waveRelease.Cantidad)
                            {
                                var errorMessage = $"Error. La cantidad a procesar ({cantidadLPN}) excede la cantidad total ({waveRelease.Cantidad}) de la orden {waveRelease.NumOrden} y producto {waveRelease.CodProducto}.";
                                _logger.LogError(errorMessage);
                                return BadRequest(errorMessage);
                            }
                            */


                        
                        //try
                        //{
                            // Guardar la orden en proceso
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
                            //_context.ordenesEnProceso.Add(ordenEnProceso);

                            //await _context.SaveChangesAsync();

                            

                            //FILTRO LPN A LUCA !!!!!!!!!!!!!!!! QUITAR MAS ADELANTE !!!!!
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

                                //_logger.LogInformation($"JSON LUCA: {jsonContent}");

                                var httpClient = _httpClientFactory.CreateClient("apiLuca");
                                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                                var urlLucaBase = _configuration["ServiceUrls:luca"];
                                var urlLuca = $"{urlLucaBase}/api/sort/LpnSorter?sorterId={familyMaster.numSalida}";
                                _logger.LogInformation("URL LUCA: " + urlLuca);


                                var response = await httpClient.PostAsync(urlLuca, httpContent);
                                if (!response.IsSuccessStatusCode)
                                {
                                    var errorContent = await response.Content.ReadAsStringAsync();
                                    _logger.LogError($"Error al enviar datos del DTLNUM {lucaRequest.dtlNumber} a LUCA. StatusCode: {response.StatusCode}. Detalles: {errorContent}");
                                    throw new Exception($"Fallo al enviar a LUCA (Status: {response.StatusCode})");
                                }
                                


                            }
                            else
                            {
                                // Si la cantidadLPN es menor que CantInr, no se envía a LUCA y se desactiva.
                                // Modificamos la instancia que ya tenemos en memoria ANTES de agregarla al contexto.
                                _logger.LogInformation($"Desactivando orden con dtlNumber {ordenEnProceso.dtlNumber} por cantidadLPN menor que cantInr.");
                                ordenEnProceso.estado = false;
                                ordenEnProceso.estadoLuca = false;
                                _logger.LogError($"Registro ({ordenEnProceso.dtlNumber}) con cantidadLPN {ordenEnProceso.cantidadLPN} menor que cantInr {ordenEnProceso.cantInr}. No se envía a LUCA.");
                            }

                            _context.ordenesEnProceso.Add(ordenEnProceso);

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

                            // Guardamos todos los cambios en la BD y luego confirmamos la transacción.
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            _logger.LogInformation($"Transacción exitosa para dtlnum {subnumSeg.dtlnum}");
                            _logger.LogInformation("Datos almacenados correctamente en la BD.");
                        }
                        catch (HttpRequestException httpEx)
                        {
                            await transaction.RollbackAsync();
                            var razonRechazo = $"Error al enviar a LUCA: {httpEx.Message}";
                            _logger.LogError($"Transacción revertida para dtlnum {subnumSeg.dtlnum}: {razonRechazo}");
                            lpn_rechazados.Add(new LpnRechazado(subnumSeg.dtlnum ?? "null", razonRechazo));
                            continue;
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            var razonRechazo = $"Error en la transacción: {ex.Message}";
                            _logger.LogError($"Transacción revertida para dtlnum {subnumSeg.dtlnum}: {razonRechazo}");
                            lpn_rechazados.Add(new LpnRechazado(subnumSeg.dtlnum ?? "null", razonRechazo));
                            continue;
                        }
                    }
                }
            }


            // Contar el total de LPN recibidos
            var totalLpnRecibidos = request.SORT_INDUCTION.LOAD_HDR_SEG
                .SelectMany(hdr => hdr.LOAD_DTL_SEG)
                .SelectMany(dtl => dtl.SUBNUM_SEG)
                .Select(sub => sub.dtlnum)
                .Where(dtlnum => !string.IsNullOrEmpty(dtlnum))
                .Distinct()
                .Count();

            // Si todos los LPN fueron rechazados, devolver un mensaje de error
            if (lpn_rechazados.Count == totalLpnRecibidos && totalLpnRecibidos > 0)
            {
                _logger.LogInformation($"Error. Todos los LPN fueron rechazados.");
                return BadRequest(new
                {
                    msg = "Todos los LPNs fueron rechazados.",
                    lpn_rechazados
                });
            }

            // Si se lograron procesar, devolver un mensaje de éxito con los LPN rechazados si los hay
            _logger.LogInformation($"Proceso de LPN completado.");

            object lpnRechazadosResponse;
            if (lpn_rechazados.Any())
            {
                lpnRechazadosResponse = lpn_rechazados;
            }
            else
            {
                lpnRechazadosResponse = "No se rechazó ningún LPN";
            }

            return Ok(new
            {
                msg = "Ok. Proceso de LPN completado.",
                lpn_rechazados = lpnRechazadosResponse
            });
        }

    }
}