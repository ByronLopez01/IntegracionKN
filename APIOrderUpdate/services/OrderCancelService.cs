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

        public async Task<OrderCancelResult> HandleOrderCancelAsync(OrderCancelKN orderCancelKn)
        {
            if (orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG is not IEnumerable<OrderCancelSeg> orderCancelSegs)
            {
                return OrderCancelResult.NotFound; //Ordenes no encontradas
            }

            foreach (var orderCancelSeg in orderCancelSegs)
            {
                var waveId = orderCancelSeg.schbat;
                var ordnum = orderCancelSeg.ordnum;

                var existingWaveRelease = await _context.WaveReleases
                    .Where(w => w.Wave == waveId && w.NumOrden == ordnum)
                    .FirstOrDefaultAsync();

                if (existingWaveRelease != null)
                {
                    _context.WaveReleases.Remove(existingWaveRelease);

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
                }
                else
                {
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

            return OrderCancelResult.Cancelled; //Ordenes canceladas
        }
    }
}
