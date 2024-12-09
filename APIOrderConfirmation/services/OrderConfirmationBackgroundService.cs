using APIOrderConfirmation.data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace APIOrderConfirmation.services
{
    public class OrderConfirmationBackgroundService : BackgroundService
    {
        private readonly OrderConfirmationContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _urlKN = "https://scl02i1.int.kn:8010/ws/mconductor/inb/CLPUD01/Senad/SORT_COMPLETE";
        private static int _numSubNum = 1;

        public OrderConfirmationBackgroundService(OrderConfirmationContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Llamar al método para procesar las órdenes
                await ProcesoOrdersAsync();

                // Esperar un tiempo antes de verificar nuevamente (por ejemplo, 1 minuto)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        public async Task ProcesoOrdersAsync()
        {
            try
            {
                // Filtrar órdenes con estado 0 (desactivadas)
                var ordersToProcess = await _context.ordenesEnProceso
                    .Where(o => o.estado == false)
                    .ToListAsync();

                var logDetails = new List<string>();

                foreach (var order in ordersToProcess)
                {
                    // Generar el subnum con un formato de 9 dígitos
                    string subnumcompleto = _numSubNum.ToString("D9");
                    _numSubNum++;

                    // Crear el JSON
                    var payload = new
                    {
                        SORT_COMPLETE = new
                        {
                            wcs_id = "WCS_ID",
                            wh_id = "CLPUD01", // valor fijo
                            msg_id = "123456", // Valor fijo o dinámico según corresponda
                            trandt = DateTime.UtcNow.ToString("yyyyMMddHHmmss"), // Fecha y hora actual
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

                    // Enviar los datos a la URL externa
                    var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_urlKN, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        logDetails.Add($"Order {order.id} sent successfully.");

                        // Guardar la orden en la tabla de ordenes
                        var cancelEntity = new Ordenes
                        {
                            Wave = order.wave,
                            WhId = "CLPUD01", // valor fijo
                            MsgId = "123456", // valor fijo
                            Trandt = DateTime.UtcNow.ToString("yyyyMMddHHmmss"), // Fecha y hora actual
                            Ordnum = order.numOrden,
                            Schbat = order.wave,
                            Cancod = order.codProducto,
                            Accion = "Confirmado"
                        };

                        _context.ordenes.Add(cancelEntity);

                        // Actualizar el estado de la orden a procesado (ya no está en proceso)
                        order.estado = true;
                        _context.ordenesEnProceso.Update(order);
                    }
                    else
                    {
                        logDetails.Add($"Error order {order.id}: {response.StatusCode}");
                    }
                }

                // Guardar cambios en la base de datos
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Manejo de errores

                Console.WriteLine($"Error processing orders: {ex.Message}");
            }
        }
    }
}