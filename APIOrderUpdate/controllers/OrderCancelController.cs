using APIOrderUpdate.data;
using APIOrderUpdate.enums;
using APIOrderUpdate.models;
using APIOrderUpdate.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIOrderUpdate.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderCancelController : ControllerBase
    {
        private readonly IOrderCancelService _orderCancelService;

        public OrderCancelController(IOrderCancelService orderCancelService)
        {
            _orderCancelService = orderCancelService;
        }

        [HttpPost]
        public async Task<IActionResult> PostOrderCancel([FromBody] OrderCancelKN orderCancelKn)
        {

            if (orderCancelKn?.ORDER_CANCEL?.ORDER_CANCEL_SEG == null ||
        !orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.Any(seg => !string.IsNullOrEmpty(seg.schbat)))
            {
                return BadRequest("JSON inválido.");
            }

            var (result, ordersNotCancelled) = await _orderCancelService.HandleOrderCancelAsync(orderCancelKn);

            switch (result)
            {
                case OrderCancelResult.AllOrdersInProcess:
                    return BadRequest(new
                    {
                        message = "No es posible cancelar órden/es en proceso.",
                        ordersNotCancelled
                    });
                case OrderCancelResult.PartiallyCancelled:
                    return BadRequest(new
                    {
                        message = "Las órdenes fueron canceladas. Exceptuando las que faltan por procesar.",
                        ordersNotCancelled
                    });
                case OrderCancelResult.NotFound:
                    return NotFound("La orden no fue encontrada y se ha registrado con un mensaje.");
                case OrderCancelResult.Cancelled:
                    return Ok("La orden ha sido cancelada exitosamente.");
                default:
                    return StatusCode(500, "Ocurrió un error inesperado.");
            }
        }
    }
}
