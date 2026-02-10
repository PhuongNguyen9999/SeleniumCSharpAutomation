using System;
using System.Threading.Tasks;
using RestSharp;
using UnsplashAutomation.Utilities;

namespace UnsplashAutomation.Services
{
    public class ApiService // Dịch vụ gọi API của Unsplash
    {
        private readonly RestClient client;

        public ApiService()
        {
            // Khởi tạo RestClient với URL gốc từ TestData
            client = new RestClient(TestData.ApiBaseUrl);
        }

        public async Task<bool> DeleteCollection(string collectionId)
        {
            Console.WriteLine($"API: Attempting to delete collection {collectionId}...");
            
            // Tạo request DELETE đến endpoint /collections/{id}
            var request = new RestRequest($"/collections/{collectionId}", Method.Delete);
            
            // Thêm Header Authorization
            // Ưu tiên dùng Bearer Token nếu có (để có quyền xóa), nếu không thì dùng Access Key (chỉ đọc)
            if (!string.IsNullOrEmpty(TestData.ApiBearerToken))
            {
                Console.WriteLine("API: Using Bearer Token for authorization.");
                request.AddHeader("Authorization", $"Bearer {TestData.ApiBearerToken}");
            }
            else
            {
                Console.WriteLine("API Warning: No Bearer Token found in TestData. Falling back to Access Key (Client-ID). Deletion will likely fail.");
                request.AddHeader("Authorization", $"Client-ID {TestData.ApiAccessKey}");
            }

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                Console.WriteLine($"API: Collection {collectionId} deleted successfully (Status: {response.StatusCode}).");
                return true;
            }
            else
            {
                Console.WriteLine($"API Warning: Failed to delete collection. Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");
                return false;
            }
        }
    }
}
