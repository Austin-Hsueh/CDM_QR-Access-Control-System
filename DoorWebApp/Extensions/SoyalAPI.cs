using DoorWebApp.Models;
using DoorWebApp.Models.DTO;
using Newtonsoft.Json;
using SoyalQRGen.Entities;
using System.Security.Cryptography;
using System.Text;

namespace DoorWebApp.Extensions
{
    public static class SoyalAPI
    {
        public static async Task<APIResponse<List<UserQRCode>>> SendUserAccessProfilesAsync(List<UserAccessProfile> profiles)
        {
            using var client = new HttpClient();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:1029/api/v1/UserAccessProfile");

                var json = JsonConvert.SerializeObject(new { profiles });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<UserQRCode>(responseString);
                APIResponse<List<UserQRCode>> parsedApiResponse = JsonConvert.DeserializeObject<APIResponse<List<UserQRCode>>>(responseString);
                return parsedApiResponse;
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine("HTTP request error: " + httpEx.Message);
                if (httpEx.InnerException != null)
                {
                    Console.WriteLine("Inner exception: " + httpEx.InnerException.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("General error: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner exception: " + ex.InnerException.Message);
                }
                throw;
            }
        }
    }
}
