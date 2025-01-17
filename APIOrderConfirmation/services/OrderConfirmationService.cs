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
        //QA
        //private readonly string _urlKN = "https://scl02i1.int.kn:8010/ws/mconductor/inb/CLPUD01/Senad/SORT_COMPLETE";
        //variable para crear el subnum
        private static int _numSubNum = 1;
        //Prod
        private readonly string _urlKN = " https://scl02p1.int.kn:8010/ws/mconductor/inb/CLPUD01/Senad/SORT_COMPLETE";
       
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
                    .Where(o => o.estado == false)
                    .ToListAsync();

                var logDetails = new List<string>();

                foreach (var order in ordersToProcess)
                {
                    // dar formato 00000000 
                    string subnumcompleto = _numSubNum.ToString("D9");
                    _numSubNum++;
                    // Crear el JSON
                    var payload = new
                    {
                        SORT_COMPLETE = new
                        {
                            wcs_id = "WCS_ID",
                            wh_id = "CLPUD01", //valor fijo
                            msg_id = "123456", //averiguar si es fijo
                            trandt = "20241125090909", //averiguar si es fijo 
                            SORT_COMP_SEG = new
                            {
                                LOAD_HDR_SEG = new
                                {
                                    LODNUM = order.wave,
                                    LOAD_DTL_SEG = new[]
                                    {
                                    new
                                    {
                                        subnum = subnumcompleto,
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
                            WhId = "CLPUD01",//validar si es fijo si no es fijo cambiar 
                            MsgId = "123456",//validar si es fijo si no es fijo cambiar 
                            Trandt = "20241125090909", //averiguar si es fijo 
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
