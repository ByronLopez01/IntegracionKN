﻿using APILPNPicking.data;
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


            // Comprobar si la lista no sea nula ni vacía.
            if (request?.SORT_INDUCTION?.LOAD_HDR_SEG == null || !request.SORT_INDUCTION.LOAD_HDR_SEG.Any())
            {
                Console.WriteLine("Datos en formato incorrecto.");
                return BadRequest("Datos en formato incorrecto.");
            }

            Console.WriteLine("Datos válidos. Procesando el encabezado de carga.");
            //var loadHdrSeg = request.SORT_INDUCTION.LOAD_HDR_SEG;

            foreach (var loadHdrSeg in request.SORT_INDUCTION.LOAD_HDR_SEG)
            {
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
                            return NotFound($"Orden {loadDtlSeg.ordnum} no registrada en WaveRelease.");
                        }

                        return StatusCode((int)waveReleaseResponseMessage.StatusCode, "Error al obtener datos de WaveRelease.");
                    }

                    var waveReleaseResponse = await waveReleaseResponseMessage.Content.ReadAsStringAsync();
                    Console.WriteLine("---------------------- Antes de la serialización ---------------------------");
                    List<WaveRelease> waveReleaseData = new List<WaveRelease>();

                    try
                    {
                        Console.WriteLine("Respuesta de WaveRelease: " + waveReleaseResponse);

                        // Deserializar la respuesta
                        waveReleaseData = JsonConvert.DeserializeObject<List<WaveRelease>>(waveReleaseResponse) ?? new List<WaveRelease>();
                        Console.WriteLine("Deserialización exitosa. Cantidad de resultados: " + waveReleaseData.Count);

                        // Imprimir el contenido de waveReleaseData
                        Console.WriteLine("JSON WAVERELEASE " + JsonConvert.SerializeObject(waveReleaseData, Formatting.Indented));
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine("Error en la deserialización: " + jsonEx.Message);
                        return StatusCode(500, "Error en la deserialización de WaveRelease.");
                    }

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

                        var cantidadLPN = loadDtlSeg.SUBNUM_SEG?.Sum(subnumSeg => subnumSeg.untqty) ?? 0;

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
                                dtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum ?? string.Empty,
                                subnum = loadDtlSeg.SUBNUM_SEG.FirstOrDefault()?.subnum ?? string.Empty,
                                estado = true,
                                familia = waveRelease.Familia,
                                numOrden = waveRelease.NumOrden,
                                wave = waveRelease.Wave,
                                tienda = waveRelease.Tienda,
                                numTanda = familyMaster.numTanda,
                                numSalida = familyMaster.numSalida,
                                estadoLuca = true                       //EstadoLuca
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
                            DtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum ?? string.Empty,
                            subnum = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.subnum ?? string.Empty
                            
                        };

                        _context.LPNSorting.Add(lpnSorting);
                        await _context.SaveChangesAsync();

                        Console.WriteLine("Datos almacenados correctamente en la tabla LpnSorting.");

                        // CREACIÓN OBJETO JSON PARA ENVIO A LUCA
                        var lucaRequest = new LucaRequest
                        {
                            codMastr = waveRelease.CodMastr,
                            codInr = waveRelease.CodInr,
                            cantMastr = waveRelease.CantMastr,
                            cantInr = waveRelease.CantInr,
                            familia = waveRelease.Familia,
                            numOrden = waveRelease.NumOrden,
                            codProducto = waveRelease.CodProducto,
                            onda = waveRelease.Wave,
                            numSalida = familyMaster.numSalida,
                            numTanda = familyMaster.numTanda,
                            dtlNumber = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.dtlnum ?? string.Empty,
                            //subnum = loadDtlSeg.SUBNUM_SEG?.FirstOrDefault()?.subnum ?? string.Empty,
                            tienda = waveRelease.Tienda
                        };

                        // ENVÍO DE JSON A LUCA
                        var jsonContent = JsonConvert.SerializeObject(lucaRequest);

                        Console.WriteLine("JSON LUCA:");
                        Console.WriteLine(jsonContent);


                        var httpClient = _httpClientFactory.CreateClient("apiLuca");
                        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        // Determinar el sorterId basado en el numSalida
                        int sorterId = familyMaster.numSalida;

                        var urlLucaBase = _configuration["ServiceUrls:luca"];
                        var urlLuca = $"{urlLucaBase}/api/sort/LpnSorter?sorterId={sorterId}";

                        Console.WriteLine("URL LUCA: " + urlLuca);

                        try
                        {
                            var response = await httpClient.PostAsync(urlLuca, httpContent);
                            Console.WriteLine("URL LUCA: " + urlLuca);

                            if (response.IsSuccessStatusCode)
                            {
                                Console.WriteLine("El JSON fue enviado correctamente a LpnSorter.");
                            }
                            else
                            {
                                Console.WriteLine("Error al enviar el JSON a LpnSorter.");
                            }
                        }
                        catch (HttpRequestException httpEx)
                        {
                            Console.WriteLine($"Error en la solicitud HTTP a Luca: {httpEx.Message}");
                            return StatusCode(500, $"Error en la solicitud HTTP a Luca: {httpEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ocurrió un error al enviar datos a LpnSorter: {ex.Message}");
                            return StatusCode(500, $"Error al enviar datos a LpnSorter: {ex.Message}");
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        if (httpEx.StatusCode == HttpStatusCode.NotFound)
                        {
                            Console.WriteLine($"No se encontró el registro en FamilyMaster: {httpEx.Message}");
                            return NotFound("No se encontró el registro en FamilyMaster.");
                        }
                        else
                        {
                            Console.WriteLine($"Error en la solicitud HTTP a FamilyMaster: {httpEx.Message}");
                            return StatusCode(500, $"Error en la solicitud HTTP a FamilyMaster: {httpEx.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al consultar FamilyMaster o enviar datos: {ex.Message}");
                        return StatusCode(500, "Error al consultar FamilyMaster o enviar datos.");
                    }
                }
            }
            return Ok("Proceso Completado");
        }

    }
}
