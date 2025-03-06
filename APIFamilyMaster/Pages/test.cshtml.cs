using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;
using APIFamilyMaster.data;

namespace APIFamilyMaster.Pages
{
    public class TestModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TestModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public List<FamilyMaster> FamilyMasters { get; set; }

        public async Task OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            SetAuthorizationHeader(httpClient);

            ///
            var response = await httpClient.GetAsync("http://apifamilymaster:8080/api/FamilyMaster/all");
            ///


            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                // testtt
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                ////
                ///
                FamilyMasters = JsonSerializer.Deserialize<List<FamilyMaster>>(content, options);
            }
            else
            {
                FamilyMasters = new List<FamilyMaster>();
            }
        }

        private void SetAuthorizationHeader(HttpClient client)
        {
            var username = "senad";
            var password = "S3nad";
            var credentials = $"{username}:{password}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }
    }
}