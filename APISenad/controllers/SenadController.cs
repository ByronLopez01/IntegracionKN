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


    }
}
