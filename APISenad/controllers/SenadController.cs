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
<<<<<<< HEAD
        
        [HttpGet("{codItem}")]
        public async Task<ActionResult> consultaCodigo(string codItem)
        {
            // Verifica que el código de ítem no esté vacío
            if (string.IsNullOrEmpty(codItem))
            {
                //cambiar a json con salida de error 
                var repuestaError = new RespuestaEscaneo
                {
                    codigoIngresado = codItem,
                    numeroOrden = "Sincodigo",
                    salida = 0 // Indica una salida de error
                };

                return Ok(repuestaError);
            }

            // Busca el código en los campos codMastr, codInr y codProducto en la tabla ordenesEnProceso
            var ordenesEncontradas = await _context.ordenesEnProceso
                .Where(o => o.codMastr == codItem || o.codInr == codItem || o.codProducto == codItem)
                .ToListAsync();

            Console.WriteLine($"Buscando código: {codItem}");
            Console.WriteLine($"Registros encontrados: {ordenesEncontradas.Count}");

            if (ordenesEncontradas == null || !ordenesEncontradas.Any())
            {
                //cambiar a retornar un json con la salida de error 
                var repuestaError = new RespuestaEscaneo
                {
                    codigoIngresado = codItem,
                    numeroOrden = "NoSeEnCuentranOrdenes",
                    salida = 0 // Indica una salida de error
                };

                return Ok(repuestaError);
            }


            int salida = 0;
            string ordennum = "";

            foreach (var orden in ordenesEncontradas)
            {
                string tipoCodigo = "Desconocido";
                int cantidadProcesada = 0;
                var familiasActivas = await _context.familias
                    .Where(f =>f.Familia ==orden.familia && f.estado == true) // Verificar si la tanda está activa
                    .FirstOrDefaultAsync();
                if (familiasActivas == null) {
                    //retornar a la salida de error 
                    Console.WriteLine("No se encontro familia para el codigo " + codItem);
                    var repuestaError = new RespuestaEscaneo
                    {
                        codigoIngresado = codItem,
                        numeroOrden = orden.numOrden,
                        salida = 0 // Indica una salida de error
                    };

                    return Ok(repuestaError);

                }
                else
                {
                    if (orden.codMastr == codItem)
                    {
                        tipoCodigo = "Master";
                        // Sumar la cantidad master solo si la cantidad es igual o menor a la cantidad lpn
                        if (orden.cantidadProcesada + orden.cantMastr <= orden.cantidadLPN)
                        {
                            cantidadProcesada = orden.cantMastr + orden.cantidadProcesada;
                        }
                        else
                        {

                            var repuestaError = new RespuestaEscaneo
                            {
                                codigoIngresado = codItem,
                                numeroOrden = orden.numOrden,
                                salida = 0 // Indica una salida de error
                            };

                            return Ok(repuestaError);
                        }
                        
                    }
                    else if (orden.codInr == codItem)
                    {
                        tipoCodigo = "Inner";
                        // Sumar la cantidad del campo cantidadInr
                        if (orden.cantidadProcesada + orden.cantInr <= orden.cantidadLPN)
                        {
                            cantidadProcesada = orden.cantInr + orden.cantidadProcesada;
                        }
                        else
                        {

                            var repuestaError = new RespuestaEscaneo
                            {
                                codigoIngresado = codItem,
                                numeroOrden = orden.numOrden,
                                salida = 0 // Indica una salida de error
                            };

                            return Ok(repuestaError);
                        }
                        
                    }
                    else if (orden.codProducto == codItem)
                    {

                        //No deberia pasar!!!!!
                        tipoCodigo = "Producto";
                        // Sumar la cantidad del campo cantidad
                        cantidadProcesada = orden.cantidad + orden.cantidadProcesada;
                    }
                }
                

                salida = orden.numSalida;
                ordennum = orden.numOrden;

                // Mensaje de depuración
                Console.WriteLine($"Registro encontrado: ID={orden.numOrden}, codMastr={orden.codMastr}, codInr={orden.codInr}, codProducto={orden.codProducto}, Tipo={tipoCodigo}");
                Console.WriteLine($"Cantidad procesada actualizada: {cantidadProcesada}");

                // Verifica si la cantidad procesada supera la cantidad total permitida
                if (cantidadProcesada > orden.cantidadLPN)
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
                
                if (cantidadProcesada == orden.cantidadLPN)
                {

                    //Cabiar estado de senad a false
                    orden.estado = false;
                }
                
                

                _context.ordenesEnProceso.Update(orden);
            }

            await _context.SaveChangesAsync();

            // Verificar si hay más órdenes en la misma familia
            var familiaProcesada = ordenesEncontradas.First().familia;
            var ordenesFamilia = await _context.ordenesEnProceso
                .Where(o => o.familia == familiaProcesada && o.estado == true)
                .ToListAsync();


            if (!ordenesFamilia.Any())
            {
                // Si todas las órdenes de la familia están completas, actualizar la tanda en FamilyMaster
                var familyMaster = await _context.familias
                    .Where(fm => fm.Familia == familiaProcesada && fm.estado == true)
                    .FirstOrDefaultAsync();

                
            }


            var respuestaSorter = new RespuestaEscaneo
            {
                codigoIngresado = codItem,
                numeroOrden = ordennum,
                salida = salida
            };

            return Ok(respuestaSorter);
        }
        
        
        
        
        /*  [HttpGet("{codItem}")]
=======

        [HttpGet("{codItem}")]
>>>>>>> 0093c8d525e452609ce5db5192669a8b6ba40d75
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
<<<<<<< HEAD
      */
=======

        // TEST!!
>>>>>>> 0093c8d525e452609ce5db5192669a8b6ba40d75
        [HttpGet("test/{codItem}")]
        public async Task<ActionResult> codigoEscaneado(string codItem)
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

<<<<<<< HEAD
            if (!ordenesEncontradas.Any())
            {
                // Verificar si el código pertenece a una familia con tanda activa en FamilyMaster
                var familiaActiva = await _context.familias
                    .Where(f => (f.Tienda1 == codItem || f.Tienda2 == codItem || f.Tienda3 == codItem ||
                                 f.Tienda4 == codItem || f.Tienda5 == codItem || f.Tienda6 == codItem ||
                                 f.Tienda7 == codItem || f.Tienda8 == codItem || f.Tienda9 == codItem ||
                                 f.Tienda10 == codItem || f.Tienda11 == codItem || f.Tienda12 == codItem) // ||
                                 //f.tienda13 == codItem || f.tienda14 == codItem)
                                 && f.estado == true) // Verificar si la tanda está activa
                    .FirstOrDefaultAsync();

                if (familiaActiva != null)
                {
                    // Si pertenece a una familia activa, devolver información de la familia y tanda
                    var response = new
                    {
                        CodigoEscaneado = codItem,
                        Familia = familiaActiva.Familia,
                        NumeroTanda = familiaActiva.NumTanda,
                        Salida = familiaActiva.NumSalida,//output 
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
                        Error = "El código no pertenece a una familia con tanda activa."
                    };
                    return NotFound(responseError);
                }
=======
            Console.WriteLine($"Buscando código: {codItem}");
            Console.WriteLine($"Registros encontrados: {ordenesEncontradas.Count}");

            if (ordenesEncontradas == null || !ordenesEncontradas.Any())
            {
                return NotFound($"No se encontró ningún registro con el código {codItem}.");
>>>>>>> 0093c8d525e452609ce5db5192669a8b6ba40d75
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
<<<<<<< HEAD
                        Salida = 9,//error 
=======
                        Salida = 0,//Asignar salida de error 
>>>>>>> 0093c8d525e452609ce5db5192669a8b6ba40d75
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
    }
}
