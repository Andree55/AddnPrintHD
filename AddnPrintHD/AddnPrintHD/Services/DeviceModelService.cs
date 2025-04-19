using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AddnPrintHD.Models;

namespace AddnPrintHD.Services
{
    public class DeviceModelService
    {
        private readonly HttpClient _httpClient;

        public DeviceModelService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ObservableCollection<DeviceModel>> GetDeviceModelsAsync()
        {
            string url = "https://fristadev.crm4.dynamics.com/api/data/v9.2/cra87_is_inventory_device_models";
            ObservableCollection<DeviceModel> deviceModels = new ObservableCollection<DeviceModel>();

            try
            {
                string accessToken = await TokenService.GetAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                    throw new Exception("Access token is empty!");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error fetching data: {response.StatusCode}");

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("API Response: ");
                Console.WriteLine(jsonResponse);

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                if (apiResponse?.Devices != null)
                {
                    foreach (var device in apiResponse.Devices)
                    {
                        deviceModels.Add(new DeviceModel
                        {
                            DeviceModelId = device.DeviceModelId,
                            Cra87Id = device.Cra87Id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine(deviceModels);
            return deviceModels;
        }

        public async Task<Guid> CreateDeviceModelAsync(string modelName)
        {
            string url = "https://fristadev.crm4.dynamics.com/api/data/v9.2/cra87_is_inventory_device_models";

            var payload = new
            {
                cra87_id = modelName
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            string token = await TokenService.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to create DeviceModel: {response.StatusCode}");

            string entityUri = response.Headers.Location.ToString();
            string id = entityUri.Split('(')[1].TrimEnd(')');
            return Guid.Parse(id);
        }
    }
}
