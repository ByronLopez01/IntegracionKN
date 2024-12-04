using APIOrderConfirmation.data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace APIOrderConfirmation.services
{
    public class OrderConfirmationService : IOrderConfirmationService
    {
        private readonly OrderConfirmationContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _urlKN = "https://scl02i1.int.kn:8010/ws/mconductor/inb/CLPUD01/Senad/SORT_COMPLETE";


        public OrderConfirmationService(OrderConfirmationContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        public async Task<(bool Success, string Detalles)> ProcesoOrdersAsync()
        {
            try
            {
                // Filtrar órdenes con estado 
                var ordersToProcess = await _context.ordenesEnProceso
                    .Where(o => o.estado == true)
                    .ToListAsync();

                var logDetails = new List<string>();

                foreach (var order in ordersToProcess)
                {
                    // Crear el JSON
                    var payload = new
                    {
                        SORT_COMPLETE = new
                        {
                            wcs_id = "WCS_ID",
                            wh_id = "WH_ID",
                            msg_id = "MSG_ID",
                            trandt = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                            SORT_COMP_SEG = new
                            {
                                LOAD_HDR_SEG = new
                                {
                                    LODNUM = order.wave,
                                    LOAD_DTL_SEG = new[]
                                    {
                                    new
                                    {
                                        subnum = order.codMastr,
                                        dtlnum = order.dtlNumber,
                                        qty = order.cantidadProcesada,
                                        stoloc = "POSTSORTER"
                                    }
                                }
                                }
                            }
                        }
                    };

                    // Enviar datos a la URL
                    var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_urlKN, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        logDetails.Add($"Order {order.id} sent successfully.");

                        // Guardar orden en la tabla ordenes
                        var cancelEntity = new Ordenes
                        {
                            Wave = order.wave,
                            WhId = "WH_ID",
                            MsgId = "MSG_ID",
                            Trandt = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                            Ordnum = order.numOrden,
                            Schbat = order.wave,
                            Cancod = order.codProducto,
                            Accion = "Confirmado"
                        };

                        _context.ordenes.Add(cancelEntity);
                    }
                    else
                    {
                        logDetails.Add($"Error order {order.id}: {response.StatusCode}");
                    }
                }

                await _context.SaveChangesAsync();
                return (true, string.Join(", ", logDetails));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        
        }
    }
}
