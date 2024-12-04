namespace APIOrderConfirmation.services
{
    public interface IOrderConfirmationService
    {
        Task<(bool Success, string Detalles)> ProcesoOrdersAsync();
    }
}
