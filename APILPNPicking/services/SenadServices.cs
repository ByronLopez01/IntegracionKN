using APILPNPicking.models;

namespace APILPNPicking.services
{
    public class SenadServices
    {
        private readonly HttpClient _httpClient;

        public SenadServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SenadResponse> SendPackageDataAsync(SenadRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("http://100.100.244.80:5000/api/PkgWeight", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SenadResponse>();
        }

    }
}
