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

        public SenadController(SenadContext context)
        {
            _context = context;
        }
        /*
        [HttpGet("{codItem}")]
        public async Task<ActionResult> consultaCodigo(string codItem)
        {
            // Verifica que el código de ítem no esté vacío
            if (string.IsNullOrEmpty(codItem))
            {
                return BadRequest("El código del ítem no puede estar vacío.");
            }

            // Busca el código en los campos codMastr, codInr y codProducto en la tabla ordenesEnProceso
            var ordenesEncontradas = await _context.ordenesEnProceso
                .Where(o => o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem)
                .ToListAsync();

            Console.WriteLine($"Buscando código: {codItem}");
            Console.WriteLine($"Registros encontrados: {ordenesEncontradas.Count}");

            if (ordenesEncontradas == null || !ordenesEncontradas.Any())
            {
                return NotFound($"No se encontró ningún registro con el código {codItem}.");
            }

            int salida = 0;
            string ordennum = "";

            foreach (var orden in ordenesEncontradas)
            {
                string tipoCodigo = "Desconocido";
                int cantidadProcesada = 0;

                if (orden.codMastr == codItem)
                {
                    tipoCodigo = "Master";
                    // Sumar la cantidad master a cantidad procesada
                    cantidadProcesada = orden.cantMastr + orden.cantidadProcesada;
                }
                else if (orden.codInr == codItem)
                {
                    tipoCodigo = "Inner";
                    // Sumar la cantidad del campo cantidadInr
                    cantidadProcesada = orden.cantInr + orden.cantidadProcesada;
                }
                else if (orden.codProducto == codItem)
                {
                    //No deberia pasar!!!!!
                    tipoCodigo = "Producto";
                    // Sumar la cantidad del campo cantidad
                    cantidadProcesada = orden.cantidad + orden.cantidadProcesada;
                }

                salida = orden.numSalida;
                ordennum = orden.numOrden;

                // Mensaje de depuración
                Console.WriteLine($"Registro encontrado: ID={orden.numOrden}, codMastr={orden.codMastr}, codInr={orden.codInr}, codProducto={orden.codProducto}, Tipo={tipoCodigo}");
                Console.WriteLine($"Cantidad procesada actualizada: {cantidadProcesada}");

                // Verifica si la cantidad procesada supera la cantidad total permitida
                if (cantidadProcesada > orden.cantidad)
                {
                    var response = new
                    {
                        CodigoEscaneado = codItem,
                        NumeroOrden = ordennum,
                        Salida = 0,//Asignar salida de error 
                        Error = "La cantidad a procesar supera la cantidad permitida."
                    };
                    return BadRequest(response);
                }

                // Actualiza la cantidad procesada solo si no supera la cantidad total
                orden.cantidadProcesada = cantidadProcesada;
                _context.ordenesEnProceso.Update(orden);
            }

            await _context.SaveChangesAsync();

            var respuestaSorter = new RespuestaEscaneo
            {
                codigoIngresado = codItem,
                numeroOrden = ordennum,
                salida = salida
            };

            return Ok(respuestaSorter);
        }
        */

        [HttpGet("{codItem}")]
        public async Task<ActionResult> consultaCodigo(string codItem)
        {
            if (string.IsNullOrEmpty(codItem))
            {
                return BadRequest("El código del ítem no puede estar vacío.");
            }

            // Buscar órdenes en proceso que coincidan con el código de ítem
            var ordenesEncontradas = await _context.ordenesEnProceso
                .Where(o => o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem)
                .ToListAsync();

            if (!ordenesEncontradas.Any())
            {
                // Si no se encuentra ninguna orden con el código, verificar si hay una tanda activa
                var tandaActiva = await _context.Familias
                    .Where(f => f.estado)
                    .ToListAsync();

                if (tandaActiva.Count > 0)
                {
                    // En caso de que haya una tanda activa, redirigir a salida de error
                    var response = new
                    {
                        CodigoEscaneado = codItem,
                        NumeroOrden = "N/A", // Sin número de orden disponible
                        Salida = -1, // Salida de error
                        Error = "El código no corresponde a ninguna orden activa."
                    };
                    return BadRequest(response);
                }
                else
                {
                    // Si no hay tanda activa, se puede manejar este caso como se desee
                    return NotFound($"No se encontró ninguna orden con el código {codItem}.");
                }
            }

            // Identificar la salida y familia de la orden procesada
            int salidaAsignada = ordenesEncontradas.First().numSalida;
            string ordennum = "";

            foreach (var orden in ordenesEncontradas)
            {
                string tipoCodigo = "Desconocido";
                int cantidadProcesada = 0;

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

                ordennum = orden.numOrden;

                // Verifica si la cantidad procesada supera la cantidad total permitida
                if (cantidadProcesada > orden.cantidad)
                {
                    var response = new
                    {
                        CodigoEscaneado = codItem,
                        NumeroOrden = ordennum,
                        Salida = 9, // Asignar salida de error 
                        Error = "La cantidad a procesar supera la cantidad permitida."
                    };
                    return BadRequest(response);
                }

                // Actualiza la cantidad procesada solo si no supera la cantidad total
                orden.cantidadProcesada = cantidadProcesada;
                _context.ordenesEnProceso.Update(orden);
            }

            await _context.SaveChangesAsync();

            // Verificar si quedan órdenes pendientes para la combinación de familia y salida actual
            bool quedanOrdenesParaTanda = await _context.ordenesEnProceso
                .AnyAsync(o => o.familia == ordenesEncontradas.First().familia && o.numSalida == salidaAsignada);

            // Si no quedan órdenes, desactivar la tanda actual para esta salida y activar la siguiente tanda
            if (!quedanOrdenesParaTanda)
            {
                var tandaActual = await _context.Familias
                    .FirstOrDefaultAsync(f => f.Familia == ordenesEncontradas.First().familia && f.NumSalida == salidaAsignada && f.estado);

                if (tandaActual != null)
                {
                    tandaActual.estado = false;
                    _context.Familias.Update(tandaActual);

                    // Activar la siguiente tanda para la misma salida
                    var siguienteTanda = await _context.Familias
                        .Where(f => f.Familia == ordenesEncontradas.First().familia && f.NumSalida == salidaAsignada && !f.estado)
                        .OrderBy(f => f.NumTanda)
                        .FirstOrDefaultAsync();

                    if (siguienteTanda != null)
                    {
                        siguienteTanda.estado = true;
                        _context.Familias.Update(siguienteTanda);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var respuestaSorter = new
            {
                codigoIngresado = codItem,
                numeroOrden = ordennum,
                salida = salidaAsignada
            };

            return Ok(respuestaSorter);
        }



    }
}
