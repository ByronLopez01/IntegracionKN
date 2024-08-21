using APILPNPicking.models;

namespace APILPNPicking.services
{
    public interface ISenadServices
    {
        Task<SenadResponse> SendPackageDataAsync(SenadRequest request);
    }
}
