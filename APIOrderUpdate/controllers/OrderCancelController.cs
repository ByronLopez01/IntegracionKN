using System.Text;
using APIOrderUpdate.data;
using APIOrderUpdate.enums;
using APIOrderUpdate.models;
using APIOrderUpdate.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace APIOrderUpdate.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderCancelController : ControllerBase
    {
        private readonly IOrderCancelService _orderCancelService;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderCancelController> _logger;

        public OrderCancelController(
            IOrderCancelService orderCancelService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<OrderCancelController> logger)
        {
            _orderCancelService = orderCancelService;
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient();
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostOrderCancel([FromBody] OrderCancelKN orderCancelKn)
        {

            if (orderCancelKn?.ORDER_CANCEL?.ORDER_CANCEL_SEG == null ||
        !orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.Any(seg => !string.IsNullOrEmpty(seg.schbat)))
            {
                _logger.LogError("Error. El JSON recibido no es válido o no contiene la información necesaria.");
                return BadRequest("Error. El JSON recibido no es válido o no contiene la información necesaria.");
            }

            var (result, ordersNotCancelled) = await _orderCancelService.HandleOrderCancelAsync(orderCancelKn);

            var ordersToCancel = orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG
                .Where(seg => !ordersNotCancelled.Contains(seg.ordnum))
                .ToList();

            if (ordersToCancel.Any())
            {
                var orderCancelJson = new
                {
                    ORDER_CANCEL = new
                    {
                        orderCancelKn.ORDER_CANCEL.wcs_id,
                        orderCancelKn.ORDER_CANCEL.wh_id,
                        orderCancelKn.ORDER_CANCEL.msg_id,
                        orderCancelKn.ORDER_CANCEL.trandt,
                        ORDER_CANCEL_SEG = ordersToCancel
                    }
                };

                // VERIFICAR SI LA URL APUNTA A PROD O QA!!!
                var jsonContent = new StringContent(JsonSerializer.Serialize(orderCancelJson), Encoding.UTF8, "application/json");
                var urlLuca = _configuration["ServiceUrls:luca"];

                _logger.LogInformation("URL Luca: {UrlLuca}", $"{urlLuca}/api/sort/OrderUpdate");
                _logger.LogInformation("Contenido JSON: {Json}", JsonSerializer.Serialize(orderCancelJson));

                // ENVIO A LUCA!!
                var response = await _httpClient.PostAsync($"{urlLuca}/api/sort/OrderUpdate", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error. Fallo al enviar el OrderCancel a Luca. Código de estado: {StatusCode}", response.StatusCode);
                    return StatusCode((int)response.StatusCode, "Error. Fallo al enviar el OrderCancel a Luca.");
                }
                
            }

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
                        message = "Las órdenes fueron canceladas. Exceptuando las que se encuentran procesadas.",
                        ordersNotCancelled
                    });
                case OrderCancelResult.NotFound:
                    return NotFound("La orden no fue encontrada y se ha registrado con un mensaje.");

                case OrderCancelResult.Cancelled:
                    return Ok("La orden ha sido cancelada exitosamente.");

                default:
                    return StatusCode((int)Response.StatusCode, "Ocurrió un error al cancelar las ordenes.");
            }
        }
    }
}
