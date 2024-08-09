using APIOrderUpdate.enums;
using APIOrderUpdate.models;

namespace APIOrderUpdate.services
{
    public interface IOrderCancelService
    {
        Task<OrderCancelResult> HandleOrderCancelAsync(OrderCancelKN orderCancelKn);
    }
}
