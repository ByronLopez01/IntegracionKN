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
            var wcsId = orderCancelKn.ORDER_CANCEL.wcs_id;
            var ordnum = orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.ordnum;

            var existingWaveRelease = await _context.WaveReleases
                .Where(w => w.Wave == wcsId && w.NumOrden == ordnum)
                .FirstOrDefaultAsync();

            if (existingWaveRelease != null)
            { 
                _context.WaveReleases.Remove(existingWaveRelease);

               var newOrderCancel = new OrderCancelEntity
                {
                    Wave = wcsId,
                    WhId = orderCancelKn.ORDER_CANCEL.wh_id,
                    MsgId = orderCancelKn.ORDER_CANCEL.msg_id,
                    Trandt = orderCancelKn.ORDER_CANCEL.trandt,
                    Ordnum = ordnum,
                    Schbat = orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.schbat,
                    Cancod = orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.cancod,
                    Accion = "Cancelación"
                };

                _context.ordenes.Add(newOrderCancel);
                await _context.SaveChangesAsync();

                return OrderCancelResult.Cancelled;
            }
            else
            {
                // Buscar si la orden existe en la base de datos en otras Waves
                var anyOrderExists = await _context.WaveReleases
                    .Where(w => w.NumOrden == ordnum)
                    .AnyAsync();

                var newOrderCancel = new OrderCancelEntity
                {
                    Wave = wcsId,
                    WhId = orderCancelKn.ORDER_CANCEL.wh_id,
                    MsgId = orderCancelKn.ORDER_CANCEL.msg_id,
                    Trandt = orderCancelKn.ORDER_CANCEL.trandt,
                    Ordnum = ordnum,
                    Schbat = orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.schbat,
                    Cancod = orderCancelKn.ORDER_CANCEL.ORDER_CANCEL_SEG.cancod,
                    Accion = anyOrderExists ? "Orden no encontrada en la Wave" : "Orden no encontrada"
                };

                _context.ordenes.Add(newOrderCancel);
                await _context.SaveChangesAsync();

                return anyOrderExists ? OrderCancelResult.NotFound : OrderCancelResult.NotFound;
            }
        }

    }
}
