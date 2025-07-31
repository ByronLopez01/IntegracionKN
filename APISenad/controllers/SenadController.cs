using System.Net.Http.Headers;
using System.Text;
using APISenad.data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        [HttpGet("{codItem}")]
        public async Task<ActionResult> CodigoEscaneado(string codItem)
        {

            _logger.LogInformation("Inicio del Proceso APISenad....");
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
                .AsNoTracking()
                .Where(o => (o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem) && o.estado == true)
                .OrderBy(o => o.id)
                .FirstOrDefaultAsync();


            // Validar si no se encontró la orden
            if (ordenEncontrada == null)
            {
                _logger.LogError("No se encontró ninguna orden activa para el código: {codItem}", codItem);
                var responseError = new
                {
                    CodigoEscaneado = codItem,
                    NumeroOrden = $"No se encontró ninguna orden activa para el código: {codItem}",
                    Salida = Error // Salida de error
                };
                return Ok(responseError);
            }

            /*
            if (ordenEncontrada == null)
            {
                Console.WriteLine($"Orden encontrada {ordenEncontrada.numOrden} ");

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
            */

            // Procesar orden encontrada
            string tipoCodigo = "Desconocido";
            int cantidadProcesada = 0;

            _logger.LogInformation("Buscando familia activa para la orden: {numOrden}, Familia: {Familia}", ordenEncontrada.numOrden, ordenEncontrada.familia);
            var familiasActivas = await _context.familias
                .AsNoTracking()
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
                var repuestaError = new
                {
                    codigoIngresado = codItem,
                    numeroOrden = "No hay más Ordenes con FAMILIA activa ó Ordenes para el código actual COMPLETAS",
                    salida = Error, // Salida de error
                };
                _logger.LogWarning("No se encontró una familia ACTIVADA para la orden");
                return Ok(repuestaError);
            }

            _logger.LogInformation("Familia activa: {familiasActivas.Familia}", familiasActivas.Familia);


            if (ordenEncontrada.codMastr == codItem)
            {
                _logger.LogInformation("Orden: {ordenEncontrada.numOrden} con codigo de producto: {codItem} es Master", ordenEncontrada.numOrden, codItem);
                tipoCodigo = "Master";
                cantidadProcesada = ordenEncontrada.cantMastr + ordenEncontrada.cantidadProcesada;

                // Verifica si la cantidad procesada supera la cantidad total permitida solo para Master
                if (cantidadProcesada > ordenEncontrada.cantidadLPN)
                {
                    _logger.LogInformation("La cantidad procesada supera la cantidad en el LPN para el código Master");
                    var cantidadExcedente = cantidadProcesada - ordenEncontrada.cantidadLPN;
                    _logger.LogInformation("Cantidad Excedente: {cantidadExcedente}", cantidadExcedente);
                    cantidadProcesada = ordenEncontrada.cantidadLPN;  // Limitar la cantidad procesada

                    _logger.LogInformation("Buscando Orden excedente...");

                    var ordenExcedente = await _context.ordenesEnProceso
                        .Where(o => o.estado == true &&
                                    (o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem) &&
                                    o.id != ordenEncontrada.id &&
                                    (o.cantMastr + o.cantidadProcesada <= o.cantidadLPN))
                        .OrderBy(o => o.id)
                        .FirstOrDefaultAsync();

                    if (ordenExcedente != null)
                    {
                        _logger.LogInformation("Orden excedente encontrada: {ordenExcedente.numOrden}", ordenExcedente.numOrden);
                        // Verificar si la nueva orden tiene la capacidad para aceptar el excedente
                        if (ordenExcedente.cantidadLPN - ordenExcedente.cantidadProcesada >= cantidadExcedente)
                        {
                            _logger.LogInformation("Transferencia del excedente a la orden: {ordenExcedente.numOrden}", ordenExcedente.numOrden);
                            // Actualiza la cantidad procesada en la orden excedente
                            ordenExcedente.cantidadProcesada = ordenExcedente.cantMastr + ordenExcedente.cantidadProcesada;
                            if (ordenExcedente.cantidadProcesada == ordenExcedente.cantidadLPN)
                            {
                                _logger.LogInformation("Orden Excedente: {ordenExcedente.numOrden} se completó", ordenExcedente.numOrden);
                                ordenExcedente.fechaProceso = DateTime.Now.AddHours(-2);
                                ordenExcedente.estado = false;
                            }

                            _context.ordenesEnProceso.Update(ordenExcedente);
                            _logger.LogInformation("Guardando datos en la BD...");
                            await _context.SaveChangesAsync();

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
                            var responseError = new
                            {
                                CodigoEscaneado = codItem,
                                NumeroOrden = "Cantidad solicitada no puede ser procesada",
                                Salida = Reinsercion // Salida de reinserción
                            };
                            _logger.LogInformation("No se encontró orden que acepte la cantidad, enviando a reinserción");
                            return Ok(responseError);
                        }
                    }
                    else
                    {
                        var responseError = new
                        {
                            CodigoEscaneado = codItem,
                            NumeroOrden = "No se encontró enviando a Reinserción",
                            Salida = Reinsercion // Salida de reinserción
                        };
                        _logger.LogInformation("No se encontró una orden excedente, enviando a reinserción");
                        return Ok(responseError);
                    }
                }
            }

            // ACTUALIZACION al detectar inner o producto.
            else if (ordenEncontrada.codInr == codItem)
            {
                _logger.LogInformation("Orden: {ordenEncontrada.numOrden} con código de producto: {codItem} es Inner", ordenEncontrada.numOrden, codItem);
                tipoCodigo = "Inner";

                // Se calcula la capacidad disponible en la orden encontrada
                int capacidadDisponible = ordenEncontrada.cantidadLPN - ordenEncontrada.cantidadProcesada;

                // Si la capacidad disponible es insuficiente para procesar la cantidad inner...
                if (capacidadDisponible < ordenEncontrada.cantInr)
                {
                    _logger.LogInformation("La capacidad disponible ({0}) es inferior a la cantidad inner ({1}). Se buscará una orden alternativa.",
                        capacidadDisponible, ordenEncontrada.cantInr);

                    var ordenAlternativa = await _context.ordenesEnProceso
                        .AsNoTracking()
                        .Where(o => o.estado == true &&
                                    o.codInr == codItem &&
                                    o.id != ordenEncontrada.id &&
                                    (o.cantidadLPN - o.cantidadProcesada) >= ordenEncontrada.cantInr)
                        .OrderBy(o => o.id)
                        .FirstOrDefaultAsync();

                    if (ordenAlternativa != null)
                    {
                        _logger.LogInformation("Orden alternativa encontrada: {ordenAlternativa.numOrden}", ordenAlternativa.numOrden);
                        cantidadProcesada = ordenAlternativa.cantInr + ordenAlternativa.cantidadProcesada;
                        ordenEncontrada = ordenAlternativa;
                    }
                    else
                    {
                        _logger.LogWarning("No hay más ordenes disponibles. Enviado a error.");
                        var responseError = new
                        {
                            CodigoEscaneado = codItem,
                            NumeroOrden = "No hay más ordenes disponibles. Enviado a error",
                            Salida = Error // Salida de error
                        };
                        return Ok(responseError);
                    }
                }
                else
                {
                    cantidadProcesada = ordenEncontrada.cantInr + ordenEncontrada.cantidadProcesada;
                }
            }


            else if (ordenEncontrada.codProducto == codItem)
            {
                _logger.LogInformation("Orden: {ordenEncontrada.numOrder} con codigo de producto: {codItem} es Codigo de producto", ordenEncontrada.numOrden, codItem);

                tipoCodigo = "Producto";
                //cantidadProcesada = ordenEncontrada.cantidad + ordenEncontrada.cantidadProcesada;
            }

            // ACTUALIZACION al detectar producto
            /*
            else if (ordenEncontrada.codProducto == codItem)
            {
                _logger.LogInformation("Orden: {ordenEncontrada.numOrden} con código de producto: {codItem} es Código de producto", ordenEncontrada.numOrden, codItem);
                tipoCodigo = "Producto";

                // Se calcula la capacidad disponible para el código de producto
                int capacidadDisponible = ordenEncontrada.cantidadLPN - ordenEncontrada.cantidadProcesada;

                // Si no hay capacidad para agregar una unidad de producto, buscar una orden alternativa
                if (capacidadDisponible < 1)
                {
                    _logger.LogInformation("La orden {ordenEncontrada.numOrden} no tiene capacidad para procesar más 'Producto'. Buscando orden alternativa...", ordenEncontrada.numOrden);

                    var ordenAlternativa = await _context.ordenesEnProceso
                        .AsNoTracking()
                        .Where(o => o.estado == true &&
                                    o.codProducto == codItem &&
                                    o.id != ordenEncontrada.id &&
                                    (o.cantidadLPN - o.cantidadProcesada) >= 1)
                        .OrderBy(o => o.id)
                        .FirstOrDefaultAsync();

                    if (ordenAlternativa != null)
                    {
                        _logger.LogInformation("Orden alternativa encontrada: {ordenAlternativa.numOrden}", ordenAlternativa.numOrden);
                        ordenEncontrada = ordenAlternativa;
                    }
                    else
                    {
                        _logger.LogWarning("No hay orden disponible para el código de producto: {codItem}", codItem);
                        var responseError = new
                        {
                            CodigoEscaneado = codItem,
                            NumeroOrden = $"No hay orden disponible para el código de producto: {CodigoEscaneado}",
                            Salida = Error // Salida de error
                        };
                        return Ok(responseError);
                    }
                }

                // Incrementar la cantidad procesada en una unidad
                cantidadProcesada = ordenEncontrada.cantidadProcesada + 1;
            }
            */


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
                return Ok(responseError);
            }



            // Actualiza la cantidad procesada luego de escanear el código
            _logger.LogInformation("Procesando Cantidad: {cantidadProcesada} ", cantidadProcesada);
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

            var respuestaSorter = new RespuestaEscaneo
            {
                codigoIngresado = codItem,
                numeroOrden = ordenEncontrada.numOrden,
                salida = ordenEncontrada.numSalida
            };

            _logger.LogInformation("codItem: {respuestaSorter.codigoIngresado} numOrden: {respuestaSorter.numeroOrden} salida: {respuestaSorter.salida}"
                , respuestaSorter.codigoIngresado,respuestaSorter.numeroOrden,respuestaSorter.salida);

            return Ok(respuestaSorter);

        }




        private static Dictionary<int, int> conteoPorSalida = Enumerable.Range(1, 16)
        .ToDictionary(i => i, i => 0);

        private static int salidaActual = 1;

        [HttpGet("PruebaSorter/{codItem}")]
        public async Task<ActionResult> PruebaSorter(string codItem)
        {
            _logger.LogInformation("Inicio del Proceso APISenad....");
            _logger.LogInformation("Código escaneado: {codigo}", codItem);

            // Verificar si ya se asignaron 10 códigos a la salida actual
            if (conteoPorSalida[salidaActual] >= 3)
            {
                salidaActual++;

                // Reiniciar si se supera la salida 16
                if (salidaActual > 16)
                    salidaActual = 1;
            }

            // Asignar el código a la salida actual
            conteoPorSalida[salidaActual]++;

            var respuestaSorter = new RespuestaEscaneo
            {
                codigoIngresado = codItem,
                numeroOrden = $"No se encontró ninguna orden activa para el código: {codItem}",
                salida = salidaActual
            };

            return Ok(respuestaSorter);
        }
    }

}