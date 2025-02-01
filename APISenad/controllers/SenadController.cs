using System.Net.Http.Headers;
using System.Text;
using APISenad.data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        private readonly ILogger<SenadController> _logger;
        

        public SenadController(SenadContext context, HttpClient httpClient, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<SenadController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiFamilyMasterClient = _httpClientFactory.CreateClient();
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

        [HttpGet("{codItem}")]
        public async Task<ActionResult> CodigoEscaneado(string codItem)
        {

            _logger.LogInformation("Ininio del Proceso....");
            // !!!!!!!!!!!!!!!!!!!!!!!!
            // VARIABLES DE SALIDA!!!!!
            // !!!!!!!!!!!!!!!!!!!!!!!!
            int Error = 17;
            int Reinsercion = 2;
            // Cambiar esto si se quiere cambiar la salida de error / reinsercion

            // Verifica que el código de ítem no esté vacío
            if (string.IsNullOrEmpty(codItem))
            {
                _logger.LogError("El codigo de item no puede estar vacío");
                return BadRequest("El código del ítem no puede estar vacío.");
            }

            _logger.LogInformation("Buscando orden...");
            // Busca el código en los campos codMastr, codInr y codProducto en la tabla ordenesEnProceso
            // Selecciona solamente las ordenes No Procesadas.
            var ordenEncontrada = await _context.ordenesEnProceso
                .Where(o => (o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem) && o.estado == true)
                .OrderBy(o => o.id)
                .FirstOrDefaultAsync();

            

            if (ordenEncontrada == null)
            {
                _logger.LogInformation("Orden encontrada {ordenEncontrada}", ordenEncontrada.numOrden);

                _logger.LogInformation("Buscando Familia Activa");
                // Verificar si el código pertenece a una familia con tanda activa en FamilyMaster
                var familiaActiva = await _context.familias
                    .Where(f => (f.Tienda1 == codItem || f.Tienda2 == codItem || f.Tienda3 == codItem ||
                                 f.Tienda4 == codItem || f.Tienda5 == codItem || f.Tienda6 == codItem ||
                                 f.Tienda7 == codItem || f.Tienda8 == codItem || f.Tienda9 == codItem ||
                                 f.Tienda10 == codItem || f.Tienda11 == codItem || f.Tienda12 == codItem) &&
                                 f.estado == true)
                    .FirstOrDefaultAsync();
                _logger.LogInformation("Familia Activa: " + familiaActiva);

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
                    _logger.LogError("La orden no pertenece a una familia activa");
                    // Si no pertenece a una familia activa
                    var responseError = new
                    {
                        CodigoEscaneado = codItem,
                        NumeroOrden = "Código sin familia con tanda activa",
                        Salida = Error, // Salida de error
                        //Error = "El código no pertenece a una familia con tanda activa."
                    };
                    return Ok(responseError);
                }
            }

            // Procesar orden encontrada
            string tipoCodigo = "Desconocido";
            int cantidadProcesada = 0;

            _logger.LogInformation("Buscando familia activa para la orden: {numOrden}, Familia: {Familia}", ordenEncontrada.numOrden, ordenEncontrada.familia);
            var familiasActivas = await _context.familias
                .FirstOrDefaultAsync(f => f.Familia == ordenEncontrada.familia && f.estado == true &&
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
                _logger.LogWarning("No se encontro familia activa para la orden: {NumOrden}, Familia: {Familia}", ordenEncontrada.numOrden, ordenEncontrada.familia);
               // Console.WriteLine($"No se encontró una familia para la orden: {ordenEncontrada.numOrden}, Familia: {ordenEncontrada.familia}");
                var repuestaError = new
                {
                    codigoIngresado = codItem,
                    numeroOrden = "No hay FAMILIA activa",
                    salida = Error, // Salida de error
                };
                _logger.LogWarning("No se encontró una familia ACTIVADA para la orden");
                return Ok(repuestaError);
            }


            _logger.LogInformation("Familia activa: {familiasActivas.Familia}", familiasActivas.Familia);


            if (ordenEncontrada.codMastr == codItem)
            {
                _logger.LogInformation("Orden: {ordenEncontrada.numOrder} con codigo de producto: {codItem} es Master", ordenEncontrada.numOrden, codItem);
                tipoCodigo = "Master";
                cantidadProcesada = ordenEncontrada.cantMastr + ordenEncontrada.cantidadProcesada;
            }
            else if (ordenEncontrada.codInr == codItem)
            {
                _logger.LogInformation("Orden: {ordenEncontrada.numOrder} con codigo de producto: {codItem} es Inner", ordenEncontrada.numOrden, codItem);

                tipoCodigo = "Inner";
                cantidadProcesada = ordenEncontrada.cantInr + ordenEncontrada.cantidadProcesada;
            }
            else if (ordenEncontrada.codProducto == codItem)
            {
                _logger.LogInformation("Orden: {ordenEncontrada.numOrder} con codigo de producto: {codItem} es Codigo de producto", ordenEncontrada.numOrden, codItem);

                tipoCodigo = "Producto";
                //cantidadProcesada = ordenEncontrada.cantidad + ordenEncontrada.cantidadProcesada;
            }

            // Si el tipo de Codigo es Producto, se envía a la salida de ERROR
            if (tipoCodigo == "Producto")
            {

                _logger.LogInformation("Orden: {ordenEncontrada.numOrder} con codigo de producto: {tipoCodigo} es Codigo un codigo Producto", ordenEncontrada.numOrden, tipoCodigo);

                var responseError = new
                {
                    CodigoEscaneado = codItem,
                    NumeroOrden = "Codigo de Producto. Enviando a Error",
                    Salida = Error // Salida de error
                };

                _logger.LogInformation("Codigo de producto detectado enviando a Salida de ERROR");
                Console.WriteLine("Codigo de Producto detectado. Enviando a ERROR ");
                return Ok(responseError);
            }

            // Verifica si la cantidad procesada supera la cantidad total permitida
            if (cantidadProcesada > ordenEncontrada.cantidadLPN)
            {
                _logger.LogInformation("La cantidad procesada supera la cantidad en el lpn ");

                var cantidadExcedente = cantidadProcesada - ordenEncontrada.cantidadLPN;

                _logger.LogInformation("Cantidad Excedente: {cantidadExcedente} ", cantidadExcedente) ;

                cantidadProcesada = ordenEncontrada.cantidadLPN;  // Establecer la cantidad procesada máxima permitida


                _logger.LogInformation("Buscando Orden excedente...");

                var ordenExcedente = await _context.ordenesEnProceso
                    .Where(o => o.estado == true &&
                          (o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem) &&
                           o.id != ordenEncontrada.id && // Excluir la orden actual
                          (o.cantMastr + o.cantidadProcesada <= o.cantidadLPN)) // Verificar que no sobrepase la cantidadLPN
                    .OrderBy(o => o.id)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Orden excedente: {ordenExcedente}", ordenExcedente);

                if (ordenExcedente != null)

                {
                    _logger.LogInformation("Orden excedente encontrada: {ordenExcedente.numOrden} ", ordenExcedente.numOrden);

                    // Verifica si la nueva orden tiene suficiente capacidad para aceptar toda la cantidad excedente
                    if (ordenExcedente.cantidadLPN - ordenExcedente.cantidadProcesada >= cantidadExcedente)
                    {
                        _logger.LogInformation("Orden excedente: {ordeExcedente.numOrden} Cantidad excedente: {cantidadExcedente} cantidad procesada: {ordenExcedente.cantidadProcesada}", ordenExcedente.numOrden,cantidadExcedente,ordenExcedente.cantidadProcesada);
                        // Solo se transfiere el excedente a la nueva orden
                        ordenExcedente.cantidadProcesada = ordenExcedente.cantMastr + ordenExcedente.cantidadProcesada;

                        // Verifica si la nueva orden se ha completado
                        if (ordenExcedente.cantidadProcesada == ordenExcedente.cantidadLPN)
                        {

                            _logger.LogInformation("Orden Excedente: {ordenExcedente.numOrden} Se cumplio la cantidad Solicitada: {ordenExcedente.cantidadLPN} ", ordenExcedente.numOrden, ordenExcedente.cantidadLPN);
                            ordenExcedente.fechaProceso = DateTime.Now.AddHours(-2);
                            ordenExcedente.estado = false; // Marca la nueva orden como completada
                        }

                        // La orden original no se modifica, sigue con su cantidad procesada original

                        // Actualiza la nueva orden en la base de datos
                        
                        _context.ordenesEnProceso.Update(ordenExcedente);  // Asegúrate de actualizar la nueva orden

                        _logger.LogInformation("Guardando datos en la BD...");
                        // Guarda los cambios en la base de datos
                        await _context.SaveChangesAsync();

                        // Responde indicando que la cantidad fue movida a otra orden
                        var response = new
                        {
                            CodigoEscaneado = codItem,
                            NumeroOrden = ordenExcedente.numOrden,
                            Salida = ordenExcedente.numSalida
                        };

                        return Ok(response);
                    }

                    else
                    {
                        
                        // Si no cabe la cantidad excedente en la otra orden, se envía a la salida de REINSERCIÓN
                        var responseError = new
                        {
                            CodigoEscaneado = codItem,
                            NumeroOrden = "Cantidad solicitada no puede ser procesada",
                            Salida = Reinsercion // Salida de reinsercion
                        };
                        _logger.LogInformation("No se encuentra orden que acepte la cantidad ");
                        _logger.LogInformation("Enviando a reinsercíon...");
                        Console.WriteLine("La cantidad solicitada no puede ser procesada en ninguna orden.");
                        return Ok(responseError);
                    }
                }
                else
                {
                    // Si no se encuentra una orden que acepte la cantidad excedente
                    var responseError = new
                    {
                        CodigoEscaneado = codItem,
                        NumeroOrden = "No se encontro enviando a Reinsercion",
                        Salida = Reinsercion // Salida de reinsercion
                    };
                    _logger.LogInformation("No se encuentra orden que acepte la cantidad ");
                    _logger.LogInformation("Enviando a reinsercíon...");
                    Console.WriteLine("No se encontró una orden disponible para procesar la cantidad solicitada.");
                    return Ok(responseError);
                }
            }

            _logger.LogInformation("Procesando Cantidad: {cantidadProcesada} ", cantidadProcesada);
            // Actualiza la cantidad procesada solo si no supera la cantidad total
            ordenEncontrada.cantidadProcesada = cantidadProcesada;

            // Verificar si se completó la orden entera
            if (cantidadProcesada == ordenEncontrada.cantidadLPN)
            {

                _logger.LogInformation("Orden: {ordenEncontrada.numOrden} con cantidad Procesada: {CantidadProcesada} " +
                    "es igual a la cantidad procesada: {ordenEncontrada.cantidadLPN} "
                    ,ordenEncontrada.numOrden, cantidadProcesada, ordenEncontrada.cantidadLPN);

                ordenEncontrada.fechaProceso = DateTime.Now.AddHours(-2);
                ordenEncontrada.estado = false;
            }


            _logger.LogInformation("Guardando datos en la BD...");
            _context.ordenesEnProceso.Update(ordenEncontrada);

            await _context.SaveChangesAsync();



            
            // Verificar si todas las órdenes de la familia han sido completadas
            var familiaOrden = ordenEncontrada.familia;
            var ordenesFamilia = await _context.ordenesEnProceso
                .Where(o => o.familia == familiaOrden)
                .ToListAsync();

            bool todasOrdenesCompletadas = ordenesFamilia.All(o => o.estado == false);

            _logger.LogInformation("Verificando que todas las ordenes de la familia: {familiaOrden} esten completadas ", familiaOrden);

            // Si todas las órdenes de la familia han sido completadas
            if (todasOrdenesCompletadas)
            {
                // Buscamos la familia actual en FamilyMaster
                var familyMasterActual = await _context.familias
                    .Where(f => f.Familia == familiaOrden)
                    .FirstOrDefaultAsync();

               // Console.WriteLine($"Todas las órdenes de la familia {familiaOrden} han sido completadas.");
                _logger.LogInformation("Todas las órdenes de la familia: {familiaOrden} fueron completadas " ,familiaOrden);
                // Console.WriteLine($"Familia actual: {familyMasterActual.Familia}");

                _logger.LogInformation(" Familia En proceso {FamilyMasterActual.Familia} ", familyMasterActual.Familia);

                // OBTENEMOS EL NÚMERO DE TANDA ACTUAL
                int numTandaActual = familyMasterActual.NumTanda;

               // Console.WriteLine("Número de tanda actual: " + numTandaActual);
                _logger.LogInformation(" Número de tanda actual: {numTandaActual} ", numTandaActual);

                await ActivatSiguienteTanda(numTandaActual);

               // try
                //{
                  //  SetAuthorizationHeader(_apiFamilyMasterClient);

                    //var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster/activarSiguienteTanda?numTandaActual={numTandaActual}";
                    //Console.WriteLine("URL FamilyMaster: " + urlFamilyMaster);

                    // Llamamos con un POST el endpoint de FamilyMaster para activar la siguiente tanda
                    //var familyMasterResponse = await _apiFamilyMasterClient.PostAsync(urlFamilyMaster, null);
                    //Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
                    //Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
                    //Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");


               // }
               // catch (HttpRequestException ex)
               // {
                 //   Console.WriteLine($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                  //  return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
               // }
               // catch (Exception ex)
               // {
                 //   Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                 //   return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
               // }

            }

            var respuestaSorter = new RespuestaEscaneo
            {
                codigoIngresado = codItem,
                numeroOrden = ordenEncontrada.numOrden,
                salida = ordenEncontrada.numSalida
            };
            _logger.LogInformation("codItem: {respuestaSorter.codigoIngresado} numOrden: {respuestaSorter.numeroOrden} salida: {respuestaSorter.salida}"
                , respuestaSorter.codigoIngresado,respuestaSorter.numeroOrden,respuestaSorter.salida);
         //   Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
          //  Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
            //Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
            //Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");
            //Console.WriteLine($"codItem: {respuestaSorter.codigoIngresado}, numOrden: {respuestaSorter.numeroOrden}, salida: {respuestaSorter.salida}");

            return Ok(respuestaSorter);

        }


        public async Task<IActionResult> ActivatSiguienteTanda(int numTandaActual)
        {

            _logger.LogInformation("Activando Siguiente Tanda...");

            try
            {
                SetAuthorizationHeader(_apiFamilyMasterClient);

                var urlFamilyMaster = $"http://apifamilymaster:8080/api/FamilyMaster/activarSiguienteTanda?numTandaActual={numTandaActual}";
                Console.WriteLine("URL FamilyMaster: " + urlFamilyMaster);
                _logger.LogInformation("URL FamilyMaster: {urlFamilyMaster} ", urlFamilyMaster);

                // Llamamos con un POST el endpoint de FamilyMaster para activar la siguiente tanda
                var familyMasterResponse = await _apiFamilyMasterClient.PostAsync(urlFamilyMaster, null);

                _logger.LogInformation("Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}",familyMasterResponse.StatusCode);

              //  Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
               // Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");
               // Console.WriteLine($"Respuesta de FamilyMaster: {familyMasterResponse.StatusCode}");

                return StatusCode(200, "TandaActivada");


            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message} ", ex.Message);

                //Console.WriteLine($"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                return StatusCode(500, $"Error HTTP al activar la siguiente tanda en FamilyMaster: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al activar la siguiente tanda en FamilyMaster: {ex.Message}",ex.Message);
           //     Console.WriteLine($"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
                return StatusCode(500, $"Error al activar la siguiente tanda en FamilyMaster: {ex.Message}");
            }

        }
    }

}