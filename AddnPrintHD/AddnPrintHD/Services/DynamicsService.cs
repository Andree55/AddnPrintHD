using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using AddnPrintHD.Commands;
using System.Windows.Input;
using AddnPrintHD.Models;

namespace AddnPrintHD.Services
{
    public class DynamicsService : INotifyPropertyChanged
    {
        public ICommand AddDeviceCommand { get; }
        public ICommand ClearFormCommand => new RelayCommand(ClearForm);

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly HttpClient _httpClient;

        private Device _newDevice = new Device();
        public Device NewDevice
        {
            get { return _newDevice; }
            set
            {
                _newDevice = value;
                NotifyPropertyChanged(nameof(NewDevice));
            }
        }
        public AvailabilityOption SelectedDeviceCategory { get; set; }
        public AvailabilityOption SelectedManufacturer { get; set; }
        public AvailabilityOption SelectedAvailability { get; set; }
        public ObservableCollection<AvailabilityOption> DeviceCategories { get; set; } = new ObservableCollection<AvailabilityOption>();
        public ObservableCollection<AvailabilityOption> Manufacturers { get; set; } = new ObservableCollection<AvailabilityOption>();
        public ObservableCollection<AvailabilityOption> AvailabilityOptions { get; set; } = new ObservableCollection<AvailabilityOption>();
        public ObservableCollection<AvailabilityOption> Device { get; set; } = new ObservableCollection<AvailabilityOption>();


        private ObservableCollection<DeviceModel> _deviceModels;
        public ObservableCollection<DeviceModel> DeviceModels
        {
            get { return _deviceModels; }
            set
            {
                _deviceModels = value;
                NotifyPropertyChanged(nameof(DeviceModels));
            }
        }
        private DeviceModel _selectedDeviceModel;
        public DeviceModel SelectedDeviceModel
        {
            get { return _selectedDeviceModel; }
            set
            {
                _selectedDeviceModel = value;
                NotifyPropertyChanged(nameof(SelectedDeviceModel));
            }
        }

        public DeviceModelService _deviceModelService;
        private string _objectValue;
        public string ObjectValue
        {
            get { return _objectValue; }
            set
            {
                _objectValue = value;
                NotifyPropertyChanged("ObjectValue");
            }
        }
        private async Task<ApiResponse> FetchDataFromApi(string url)
        {
            try
            {
                string accessToken = TokenService.GetCurrentToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Token is null or empty, retrieving new token...");
                    accessToken = await TokenService.GetAccessToken();

                    if (string.IsNullOrEmpty(accessToken))
                    {
                        Console.WriteLine("Failed to retrieve token");
                        return null;
                    }
                }

                Console.WriteLine($"Using access token: {accessToken.Substring(0, 20)}...");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await _httpClient.GetAsync(url);

                Console.WriteLine($"API response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API request failed: {response.StatusCode}\n{error}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Received JSON: {jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 200))}...");

                return JsonSerializer.Deserialize<ApiResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data from API: {ex.Message}");
                return null;
            }
        }
        private void ProcessAndLoadData(List<AvailabilityOption> source, string attributeName, ObservableCollection<AvailabilityOption> targetCollection)
        {
            try
            {
                if (source != null)
                {
                    targetCollection.Clear();

                    foreach (var item in source)
                    {
                        if (item.AttributeName == attributeName)
                        {
                            Console.WriteLine($"Adding: {item.Value} ({item.AttributeValue})");
                            Console.WriteLine($"Type of AttributeValue: {item.AttributeValue.GetType()}");

                            targetCollection.Add(new AvailabilityOption
                            {
                                AttributeValue = item.AttributeValue,
                                Value = item.Value
                            });
                        }
                    }
                }

                Console.WriteLine($"Data loaded successfully for {attributeName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing data: {ex.Message}");
            }
        }

        private async Task LoadDeviceCategories()
        {
            var response = await FetchDataFromApi(
                "https://fristadev.crm4.dynamics.com/api/data/v9.2/stringmaps?$filter=objecttypecode eq 'cra87_is_inventory'"
            );

            ProcessAndLoadData(response?.Value, "cra87_device_category", DeviceCategories);
        }


        private async Task LoadManufacturers()
        {
            var response = await FetchDataFromApi(
                "https://fristadev.crm4.dynamics.com/api/data/v9.2/stringmaps?$filter=objecttypecode eq 'cra87_is_inventory'"
            );

            ProcessAndLoadData(response?.Value, "cra87_manufacturer", Manufacturers);
        }


        public async Task LoadDeviceModels()
        {
            if (_deviceModelService != null)
            {
                var deviceModels = await _deviceModelService.GetDeviceModelsAsync();
                if (deviceModels != null && deviceModels.Count > 0)
                {
                    DeviceModels.Clear();
                    foreach (var device in deviceModels)
                    {
                        DeviceModels.Add(device);
                    }
                }
            }
            else
            {
                Console.WriteLine("_deviceModelService is not initialized.");
            }
        }

        private async Task LoadAvailabilityOptions()
        {
            var response = await FetchDataFromApi(
                "https://fristadev.crm4.dynamics.com/api/data/v9.2/stringmaps?$filter=objecttypecode eq 'cra87_is_inventory'"
            );

            ProcessAndLoadData(response?.Value, "cra87_availability", AvailabilityOptions);
        }

        public async void AddDevice()
        {
            try
            {
                string accessToken = TokenService.GetCurrentToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = await TokenService.GetAccessToken();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        MessageBox.Show("Access token is missing. Record cannot be added.");
                        return;
                    }
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (!_httpClient.DefaultRequestHeaders.Contains("OData-MaxVersion"))
                {
                    _httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                    _httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }

                var payload = new Dictionary<string, object>();

                if (!string.IsNullOrWhiteSpace(NewDevice.DeviceName))
                    payload.Add("cra87_device_name", NewDevice.DeviceName);

                if (!string.IsNullOrWhiteSpace(NewDevice.SerialNumber))
                    payload.Add("cra87_serial_number", NewDevice.SerialNumber);

                if (SelectedDeviceCategory != null)
                    payload.Add("cra87_device_category", SelectedDeviceCategory.AttributeValue);

                if (SelectedManufacturer != null)
                    payload.Add("cra87_manufacturer", SelectedManufacturer.AttributeValue);

                if (SelectedAvailability != null)
                    payload.Add("cra87_availability", SelectedAvailability.AttributeValue);

                if (SelectedDeviceModel != null)
                {
                    string modelBind = $"/cra87_is_inventory_device_models({SelectedDeviceModel.DeviceModelId})";
                    payload.Add("cra87_Device_model@odata.bind", modelBind);
                }


                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Payload:\n" + json);

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "https://fristadev.api.crm4.dynamics.com/api/data/v9.2/cra87_is_inventories",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var locationHeader = response.Headers.Location;
                    string createdEntityUri = locationHeader?.ToString();

                    string accessTokenFinal = TokenService.GetCurrentToken();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenFinal);


                    if (!string.IsNullOrEmpty(createdEntityUri))
                    {
                        HttpResponseMessage retrieveResponse = await _httpClient.GetAsync(createdEntityUri);

                        if (retrieveResponse.IsSuccessStatusCode)
                        {
                            string parsedJson = await retrieveResponse.Content.ReadAsStringAsync();

                            using (JsonDocument doc = JsonDocument.Parse(parsedJson))
                            {
                                if (doc.RootElement.TryGetProperty("cra87_id", out JsonElement idElement))
                                {
                                    string deviceId = idElement.GetString();
                                    ObjectValue = deviceId;

                                    MessageBox.Show(
                                        $"Record added successfully.\nDevice ID: {deviceId}",
                                        "Success",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information
                                    );

                                    ClearForm();
                                    return;
                                }
                            }
                        }
                    }


                    MessageBox.Show("Record added, but device ID could not be retrieved.", "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);


                    ClearForm();
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error while creating record:\n{response.StatusCode}\n{errorMsg}", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception:\n{ex.Message}", "Exception");
            }
        }
        private void ClearForm()
        {
            NewDevice = new Device();
        }
    }
    }

