using APIOrderConfirmation.data;
using APIOrderConfirmation.models;
using APIOrderConfirmation.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIOrderConfirmation.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public class OrderConfirmationController : ControllerBase
    {

        private readonly IOrderConfirmationService _orderConfirmationService;
        private readonly OrderConfirmationContext _context;
        //Variable secuencial para generar el subnum 
        private static int _numSubNum = 1;

        public OrderConfirmationController(IOrderConfirmationService orderConfirmationService, OrderConfirmationContext context)
        {
            _orderConfirmationService = orderConfirmationService;
            _context = context;
        }


        [HttpPost("")]
        public async Task<IActionResult> ProcessOrders()
        {
            var (success, detalles) = await _orderConfirmationService.ProcesoOrdersAsync();

            if (success)
            {
                return Ok(new { message = "Órdenes procesadas correctamente.", detalles });
            }
            else
            {
                return BadRequest(new { message = "Error al procesar las órdenes.", detalles });
            }
        }
        [HttpGet]
        public IActionResult GetSortComplete()
        {
            // dar formato 00000000 
            string subnumcompleto = _numSubNum.ToString("D9");

            var response = new
            {
                SORT_COMPLETE = new
                {
                    wcs_id = "WCS_ID",
                    wh_id = "CLPUD01", //fijo 
                    msg_id = "MSG_ID",
                    trandt = "YYYYMMDDHHMISS",
                    SORT_COMP_SEG = new
                    {
                        LOAD_HDR_SEG = new
                        {
                            LODNUM = "SRCLOD",
                            LOAD_DTL_SEG = new[]
                            {
                                new
                                {
                                    subnum = subnumcompleto,
                                    dtlnum = "DTLNUM",
                                    stoloc = "DSTLOC",
                                    qty = 10
                                }
                            }
                        }
                    }
                }
            };
            _numSubNum++;

            return Ok(response);
        }

        [HttpPost("Procesado")]
        public async Task<IActionResult> Procesado([FromBody] SortCompleteKN request)
        {
            if (request?.SORT_COMPLETE?.SORT_COMP_SEG?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null ||
                !request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG.Any())
            {
                return BadRequest("Datos en formato incorrecto.");
            }

            try
            {
                foreach (var loadDtl in request.SORT_COMPLETE.SORT_COMP_SEG.LOAD_HDR_SEG.LOAD_DTL_SEG)
                {
                    var dtlnum = loadDtl.dtlnum;

                    // Buscar la orden segun su dtlnum
                    var orden = await _context.ordenesEnProceso
                        .FirstOrDefaultAsync(o => o.dtlNumber == dtlnum);

                    if (orden == null)
                    {
                        // Not found si no encuentra la orden
                        Console.WriteLine($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                        return NotFound($"No se encontró ninguna orden con el dtlnum {dtlnum}.");
                    }

                    if (!orden.estadoLuca)
                    {
                        // Si la orden ya fue procesada, retornar un error
                        Console.WriteLine($"La orden con dtlnum {dtlnum} ya fue procesada.");
                        return BadRequest($"La orden con dtlnum {dtlnum} ya fue procesada.");
                    }

                    // Actualizar el estadoLuca a false
                    orden.estadoLuca = false;
                    _context.ordenesEnProceso.Update(orden);
                }

                // Guardar cambios a BD
                await _context.SaveChangesAsync();

                Console.WriteLine("EstadoLuca actualizado correctamente.");
                return Ok("EstadoLuca actualizado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrió un error al procesar las órdenes: " + ex.Message);
                return StatusCode(500, $"Ocurrió un error al procesar las órdenes: {ex.Message}");
            }
        }
    }
}
