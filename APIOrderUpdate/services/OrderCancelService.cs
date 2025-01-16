using System.Linq;
using System.Threading.Tasks;
using APIOrderUpdate.data;
using APIOrderUpdate.enums;
using APIOrderUpdate.models;
using Microsoft.EntityFrameworkCore;

namespace APIOrderUpdate.services
{
    public class OrderCancelService : IOrderCancelService
    {
        private readonly OrderUpdateContext _context;

        public OrderCancelService(OrderUpdateContext context)
        {
            _context = context;
        }

        public async Task<(OrderCancelResult, List<string>)> HandleOrderCancelAsync(OrderCancelKN orderCancelKn)
        {
            if (orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG is not IEnumerable<OrderCancelSeg> orderCancelSegs)
            {
                return (OrderCancelResult.NotFound, new List<string>()); //Ordenes no encontradas
            }

            bool anyOrderCancelled = false;
            bool anyOrderNotCancelled = false;
            bool allOrdersInProcess = true;
            var ordersNotCancelled = new List<string>();

            foreach (var orderCancelSeg in orderCancelSegs)
            {
                var waveId = orderCancelSeg.schbat;
                var ordnum = orderCancelSeg.ordnum;

                var existingOrderInProcess = await _context.OrdenEnProceso
                    .Where(o => o.numOrden == ordnum)
                    .FirstOrDefaultAsync();

                if (existingOrderInProcess != null && existingOrderInProcess.estado)
                {
                    // Si la orden está en proceso, no se puede cancelar
                    anyOrderNotCancelled = true;
                    ordersNotCancelled.Add(ordnum);
                    continue;
                }

                allOrdersInProcess = false; // Al menos una orden no está en proceso

                var existingWaveRelease = await _context.WaveReleases
                    .Where(w => w.Wave == waveId && w.NumOrden == ordnum)
                    .ToListAsync();

                if (existingWaveRelease != null)
                {
                    _context.WaveReleases.RemoveRange(existingWaveRelease);

                    if (existingOrderInProcess != null)
                    {
                        _context.OrdenEnProceso.Remove(existingOrderInProcess);
                    }

                    var newOrderCancel = new OrderCancelEntity
                    {
                        Wave = waveId,
                        WhId = orderCancelKn.ORDER_CANCEL.wh_id,
                        MsgId = orderCancelKn.ORDER_CANCEL.msg_id,
                        Trandt = orderCancelKn.ORDER_CANCEL.trandt,
                        Ordnum = ordnum,
                        Schbat = orderCancelSeg.schbat,
                        Cancod = orderCancelSeg.cancod,
                        Accion = "Cancelación"
                    };

                    _context.ordenes.Add(newOrderCancel);
                    anyOrderCancelled = true;
                }
                else
                {
                    // La orden no se encontró o no está en la Wave especificada
                    anyOrderNotCancelled = true;
                    ordersNotCancelled.Add(ordnum);

                    // Buscar si la orden existe en la base de datos en otras Waves
                    var anyOrderExists = await _context.WaveReleases
                        .Where(w => w.NumOrden == ordnum)
                        .AnyAsync();

                    var newOrderCancel = new OrderCancelEntity
                    {
                        Wave = waveId,
                        WhId = orderCancelKn.ORDER_CANCEL.wh_id,
                        MsgId = orderCancelKn.ORDER_CANCEL.msg_id,
                        Trandt = orderCancelKn.ORDER_CANCEL.trandt,
                        Ordnum = ordnum,
                        Schbat = orderCancelSeg.schbat,
                        Cancod = orderCancelSeg.cancod,
                        Accion = anyOrderExists ? "Orden no encontrada en la Wave" : "Orden no encontrada"
                    };

                    _context.ordenes.Add(newOrderCancel);
                }
            }

            await _context.SaveChangesAsync();

            // Si todas las ordenes están en proceso, retornar AllOrdersInProcess
            if (allOrdersInProcess)
            {
                return (OrderCancelResult.AllOrdersInProcess, ordersNotCancelled);
            }

            // Si alguna orden fue cancelada, retornar Cancelled, de lo contrario NotFound
            if (anyOrderCancelled) 
            {
                return (anyOrderNotCancelled ? OrderCancelResult.PartiallyCancelled : OrderCancelResult.Cancelled, ordersNotCancelled);
            }
            else
            {
                return (OrderCancelResult.NotFound, ordersNotCancelled);
            }
        }
    }
}
