using Microsoft.EntityFrameworkCore;

namespace APIOrderConfirmation.data
{
    public class OrderConfirmationContext : DbContext
    {
        public OrderConfirmationContext(DbContextOptions<OrderConfirmationContext> options)
            : base(options)
        {
        }


    }
}
