using System.Net.Http.Headers;
using System.Text;
using APISenad.data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APISenad.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SenadController : Controller
    {
        private readonly SenadContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _apiFamilyMasterClient;

        public SenadController(SenadContext context, HttpClient httpClient, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiFamilyMasterClient = _httpClientFactory.CreateClient();

        }

        private void SetAuthorizationHeader(HttpClient client)
        {
            var username = _configuration["BasicAuth:Username"];
            var password = _configuration["BasicAuth:Password"];
            var credentials = $"{username}:{password}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }

        // TEST!!
        [HttpGet("{codItem}")]
        public async Task<ActionResult> CodigoEscaneado(string codItem)
        {
            // Verifica que el código de ítem no esté vacío
            if (string.IsNullOrEmpty(codItem))
            {
                return BadRequest("El código del ítem no puede estar vacío.");
            }

            // Busca el código en los campos codMastr, codInr y codProducto en la tabla ordenesEnProceso
            // Selecciona solamente las ordenes No Procesadas.
            var ordenesEncontradas = await _context.ordenesEnProceso
                .Where(o => (o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem) && o.estado == true)
                .ToListAsync();

            if (ordenesEncontradas.Count == 0)
            {
                // Verificar si el código pertenece a una familia con tanda activa en FamilyMaster
                var familiaActiva = await _context.familias
                    .Where(f => (f.Tienda1 == codItem || f.Tienda2 == codItem || f.Tienda3 == codItem ||
                                 f.Tienda4 == codItem || f.Tienda5 == codItem || f.Tienda6 == codItem ||
                                 f.Tienda7 == codItem || f.Tienda8 == codItem || f.Tienda9 == codItem ||
                                 f.Tienda10 == codItem || f.Tienda11 == codItem || f.Tienda12 == codItem) &&
                                 f.estado == true)
                    .FirstOrDefaultAsync();

                if (familiaActiva != null)
                {
                    // Si pertenece a una familia activa, devolver información de la familia y tanda
                    var response = new
                    {
                        CodigoEscaneado = codItem,
                        Familia = familiaActiva.Familia,
                        NumeroTanda = familiaActiva.NumTanda,
                        Salida = familiaActiva.NumSalida,
                        Estado = "Tanda Activa"
                    };
                    return Ok(response);
                }
                else
                {
                    // Si no pertenece a una familia activa
                    var responseError = new
                    {
                        CodigoEscaneado = codItem,
                        NumeroOrden = "Código sin familia con tanda activa",
                        Salida = 9, // Salida de error
                        //Error = "El código no pertenece a una familia con tanda activa."
                    };
                    return Ok(responseError);
                }
            }


            
            // Procesar orden encontrada
            foreach (var orden in ordenesEncontradas)
            {
                string tipoCodigo = "Desconocido";
                int cantidadProcesada = 0;

                Console.WriteLine($"Buscando familia activa para la orden {orden.numOrden}, Familia: {orden.familia}");
                var familiasActivas = await _context.familias
                    .FirstOrDefaultAsync(f => f.Familia == orden.familia && f.estado == true &&                                                     
                                (f.Tienda1 != null || f.Tienda2 != null || f.Tienda3 != null || 
                                f.Tienda4 != null || f.Tienda5 != null || f.Tienda6 != null || 
                                f.Tienda7 != null || f.Tienda8 != null || f.Tienda9 != null || 
                                f.Tienda10 != null || f.Tienda11 != null || f.Tienda12 != null));
                // ^^^^ Verificación para que acepte FamilyMasters con al menos una Tienda con NULL
                // SI SE CARGAN FAMILIAS CON TIENDAS NULL NO VA A FUNCIONAR!!

                // Buscar la familia activa para la orden
                if (familiasActivas == null)
                {
                    // Retornar la salida de error
                    Console.WriteLine($"No se encontró una familia para la orden: {orden.numOrden}, Familia: {orden.familia}");
                    var repuestaError = new
                    {
                        codigoIngresado = codItem,
                        numeroOrden = "No hay FAMILIA activa",
                        salida = 9, // Salida de error
                        //error = "No se encontró una familia ACTIVADA para la orden."
                    };
                    Console.WriteLine("No se encontró una familia ACTIVADA para la orden");
                    return Ok(repuestaError);
                }

                Console.WriteLine($"Familia activa: {familiasActivas.Familia}");

                if (orden.codMastr == codItem)
                {
                    tipoCodigo = "Master";
                    cantidadProcesada = orden.cantMastr + orden.cantidadProcesada;
                }
                else if (orden.codInr == codItem)
                {
                    tipoCodigo = "Inner";
                    cantidadProcesada = orden.cantInr + orden.cantidadProcesada;
                }
                else if (orden.codProducto == codItem)
                {
                    tipoCodigo = "Producto";
                    cantidadProcesada = orden.cantidad + orden.cantidadProcesada;
                }

                // Verifica si la cantidad procesada supera la cantidad total permitida
                if (cantidadProcesada > orden.cantidadLPN)
                {
                    /*
                    // Si el código ingresado es de Tipo Master se envía a la salida 2!
                    if (tipoCodigo == "Master")
                    {
                        var response = new
                        {
                            CodigoEscaneado = codItem,
                            NumeroOrden = "Cantidad procesada de tipo MASTER supera limite",
                            Salida = 2
                        };
                        Console.WriteLine("La cantidad de tipo MASTER supera la cantidad limite.");
                        return Ok(response);
                    }
                    // En cualquier otro caso se envía a la salida 9!
                    else
                    {
                        var response = new
                        {
                            CodigoEscaneado = codItem,
                            NumeroOrden = "Cantidad procesada de tipo INNER/PRODUCTO supera limite",
                            Salida = 9 // Salida de error
                            //Error = "La cantidad a procesar supera la cantidad permitida."
                        };
                        Console.WriteLine("La cantidad de tipo INNER/PRODUCTO a procesar de supera la cantidad.");
                        return Ok(response);
                    }
                    */

                    var response = new
                    {
                        CodigoEscaneado = codItem,
                        NumeroOrden = "Cantidad a procesar supera limite",
                        Salida = 9 // Salida de error
                    };
                    Console.WriteLine("La cantidad a procesar supera el limite.");
                    return Ok(response);
                }

                // Actualiza la cantidad procesada solo si no supera la cantidad total
                orden.cantidadProcesada = cantidadProcesada;
                
                // Verificar si se completó la orden entera
                if (cantidadProcesada == orden.cantidadLPN)
                {
                    orden.estado = false;
                }

                _context.ordenesEnProceso.Update(orden);
            }

            await _context.SaveChangesAsync();


            
            // Verificar si todas las órdenes de la familia han sido completadas
            var familiaOrden = ordenesEncontradas.First().familia;
            var ordenesFamilia = await _context.ordenesEnProceso
                .Where(o => o.familia == familiaOrden)
                .ToListAsync();

            bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);

            // Si todas las órdenes de la familia han sido completadas
            if (todasOrdenesCompletadas)
            {
                // Buscamos la familia actual en FamilyMaster
                var familyMasterActual = await _context.familias
                    .Where(f => f.Familia == familiaOrden)
                    .FirstOrDefaultAsync();

                Console.WriteLine($"Todas las órdenes de la familia {familiaOrden} han sido completadas.");

                Console.WriteLine($"Familia actual: {familyMasterActual.Familia}");


                // OBTENEMOS EL NÚMERO DE TANDA ACTUAL
                int numTandaActual = familyMasterActual.NumTanda;

                Console.WriteLine("Número de tanda actual: " + numTandaActual);

                try
                {
                    SetAuthorizationHeader(_apiFamilyMasterClient);
                    
                    var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster/activarSiguienteTanda?numTandaActual={numTandaActual}";
                    Console.WriteLine("URL FamilyMaster: " + urlFamilyMaster);

                    // Llamamos con un POST el endpoint de FamilyMaster para activar la siguiente tanda
                    var familyMasterResponse = await _apiFamilyMasterClient.PostAsync(urlFamilyMaster, null);
                    Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
                    Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
                    Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");


                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                    return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                    return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                }

            }

            var respuestaSorter = new RespuestaEscaneo
            {
                codigoIngresado = codItem,
                numeroOrden = ordenesEncontradas.First().numOrden,
                salida = ordenesEncontradas.First().numSalida
            };

            Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
            Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
            Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
            Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
            Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");

            return Ok(respuestaSorter);

        }
    }
}
