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
                string.IsNullOrEmpty(orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.schbat))
            {
                return BadRequest("JSON inválido.");
            }

            var result = await _orderCancelService.HandleOrderCancelAsync(orderCancelKn);

            switch (result)
            {
                case OrderCancelResult.Cancelled:
                    return Ok("La orden ha sido cancelada exitosamente.");
                case OrderCancelResult.NotFound:
                    return NotFound("La orden no fue encontrada y se ha registrado con un mensaje.");
                default:
                    return StatusCode(500, "Ocurrió un error inesperado.");
            }
        }
    }
}
