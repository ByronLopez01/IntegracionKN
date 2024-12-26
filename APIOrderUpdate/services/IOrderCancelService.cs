using APIOrderUpdate.enums;
using APIOrderUpdate.models;

namespace APIOrderUpdate.services
{
    public interface IOrderCancelService
    {
        Task<(OrderCancelResult, List<string>)> HandleOrderCancelAsync(OrderCancelKN orderCancelKn);
    }
}
