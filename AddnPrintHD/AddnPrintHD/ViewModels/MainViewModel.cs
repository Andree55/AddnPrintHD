using DymoSDK.Implementations;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System;
using AddnPrintHD.Models;
using AddnPrintHD.Commands;
using System.Windows;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AddnPrintHD.Services;

namespace AddnPrintHD.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Commands
        private ICommand _openFileCommand;
        public ICommand OpenFileCommand
        {
            get { return _openFileCommand = _openFileCommand ?? new CommandHandler(() => OpenFileAction(), true); }
        }

        private ICommand _printLabelCommand;
        public ICommand PrintLabelCommand
        {
            get { return _printLabelCommand = _printLabelCommand ?? new CommandHandler(() => PrintLabelAction(), true); }
        }

        private ICommand _updateLabelCommand;
        public ICommand UpdateLabelCommand
        {
            get { return _updateLabelCommand = _updateLabelCommand ?? new CommandHandler(() => UpdateValueAction(), true); }
        }

        private ICommand _updatePreviewCommand;
        public ICommand UpdatePreviewCommand
        {
            get { return _updatePreviewCommand = _updatePreviewCommand ?? new CommandHandler(() => UpdatePreviewAction(), true); }
        }



        #endregion

        #region Props
        IEnumerable<DymoSDK.Interfaces.IPrinter> _printers;

        public ICommand AddDeviceCommand { get; }
        public ICommand ClearFormCommand => new RelayCommand(ClearForm);

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
                OnPropertyChanged(nameof(DeviceModels));
            }
        }
        private DeviceModel _selectedDeviceModel;
        public DeviceModel SelectedDeviceModel
        {
            get { return _selectedDeviceModel; }
            set
            {
                _selectedDeviceModel = value;
                OnPropertyChanged(nameof(SelectedDeviceModel));
            }
        }

        public DeviceModelService _deviceModelService;
        #endregion

        #region Dynamics
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
                "https://org689d1ab9.crm4.dynamics.com/api/data/v9.2/stringmaps?$filter=objecttypecode eq 'cra87_is_inventory'"
            );

            ProcessAndLoadData(response?.Value, "cra87_device_category", DeviceCategories);
        }
        private async Task LoadManufacturers()
        {
            var response = await FetchDataFromApi(
                "https://org689d1ab9.crm4.dynamics.com/api/data/v9.2/stringmaps?$filter=objecttypecode eq 'cra87_is_inventory'"
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
                "https://org689d1ab9.crm4.dynamics.com/api/data/v9.2/stringmaps?$filter=objecttypecode eq 'cra87_is_inventory'"
            );

            ProcessAndLoadData(response?.Value, "cra87_availability", AvailabilityOptions);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    "https://org689d1ab9.api.crm4.dynamics.com/api/data/v9.2/cra87_is_inventories",
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
                                        $"Record added successfully!\n" +
                                        $"Please verify the accuracy of the data associated with Device ID: {deviceId} in Inventory.",
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
        #endregion

        public MainViewModel()
        {
            DymoSDK.App.Init();
            dymoSDKLabel = DymoLabel.Instance;
            TwinTurboRolls = new List<string>() { "Auto", "Left", "Right" };
            LoadPrinters();

            AddDeviceCommand = new RelayCommand(AddDevice);

            _httpClient = new HttpClient();

            AvailabilityOptions = new ObservableCollection<AvailabilityOption>();

            _deviceModelService = new DeviceModelService();
            DeviceModels = new ObservableCollection<DeviceModel>();

            _ = LoadAvailabilityOptions();
            _ = LoadDeviceCategories();
            _ = LoadManufacturers();
            _ = LoadDeviceModels();
        }
        #region Dymo
        public IEnumerable<DymoSDK.Interfaces.IPrinter> Printers
        {
            get
            {
                if (_printers == null)
                    _printers = new List<DymoSDK.Interfaces.IPrinter>();
                return _printers;
            }
            set
            {
                _printers = value;
                NotifyPropertyChanged("Printers");
            }
        }

        public int PrintersFound
        {
            get { return Printers.Count(); }
        }

        string _fileName;
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    return "No file selected";

                return _fileName;
            }
            set
            {
                _fileName = value;
                NotifyPropertyChanged("FileName");
            }
        }

        private BitmapImage _imageSourcePreview;

        public BitmapImage ImageSourcePreview
        {
            get { return _imageSourcePreview; }
            set
            {
                _imageSourcePreview = value;
                NotifyPropertyChanged("ImageSourcePreview");
            }
        }


        List<DymoSDK.Interfaces.ILabelObject> _labelObjects;
        public List<DymoSDK.Interfaces.ILabelObject> LabelObjects
        {
            get
            {
                if (_labelObjects == null)
                    _labelObjects = new List<DymoSDK.Interfaces.ILabelObject>();
                return _labelObjects;
            }
            set
            {
                _labelObjects = value;
                NotifyPropertyChanged("LabelObjects");
            }
        }

        private DymoSDK.Interfaces.ILabelObject _selectedLabelObject;
        public DymoSDK.Interfaces.ILabelObject SelectedLabelObject
        {
            get { return _selectedLabelObject; }
            set
            {
                _selectedLabelObject = value;
                NotifyPropertyChanged("SelectedLabelObject");
            }
        }

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


        DymoSDK.Interfaces.IPrinter _selectedPrinter;
        public DymoSDK.Interfaces.IPrinter SelectedPrinter
        {
            get { return _selectedPrinter; }
            set
            {
                _selectedPrinter = value;
                NotifyPropertyChanged("SelectedPrinter");
                DisplayConsumableInformation();
            }
        }

        List<string> _twinTurboRolls;
        public List<string> TwinTurboRolls
        {
            get
            {
                if (_twinTurboRolls == null)
                    _twinTurboRolls = new List<string>();
                return _twinTurboRolls;
            }
            set
            {
                _twinTurboRolls = value;
                NotifyPropertyChanged("TwinTurboRolls");
            }
        }

        private string _selectedRoll;
        public string SelectedRoll
        {
            get { return _selectedRoll; }
            set
            {
                _selectedRoll = value;
                NotifyPropertyChanged("SelectedRoll");
            }
        }

        private string _consumableInfoText;
        public string ConsumableInfoText
        {
            get { return _consumableInfoText; }
            set
            {
                _consumableInfoText = value;
                NotifyPropertyChanged("ConsumableInfoText");
            }
        }

        private bool _showConsumableInfo;
        public bool ShowConsumableInfo
        {
            get { return _showConsumableInfo; }
            set
            {
                _showConsumableInfo = value;
                NotifyPropertyChanged("ShowConsumableInfo");
            }
        }
        

        DymoSDK.Interfaces.IDymoLabel dymoSDKLabel;


        

        /// <summary>
        /// Load Printers
        /// </summary>
        private async void LoadPrinters()
        {
            Printers = await DymoPrinter.Instance.GetPrinters();
        }

        /// <summary>
        /// Open a Dymo label file and load the content in the instance of the class
        /// Get the preview image of the label
        /// Get the list of object names
        /// </summary>
        private void OpenFileAction()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DYMO files |*.label;*.dymo|All files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                FileName = openFileDialog.FileName;
                // DymoSDK.App.Init();
                //Load label from file path
                dymoSDKLabel.LoadLabelFromFilePath(FileName);
                //Get image preview of the label
                dymoSDKLabel.GetPreviewLabel();
                //Load image preview in the control 
                ImageSourcePreview = LoadImage(dymoSDKLabel.Preview);
                //Get object names list
                LabelObjects = dymoSDKLabel.GetLabelObjects().ToList();
            }
        }
        /// <summary>
        /// Print the current loaded label using the selected printer name
        /// </summary>
        private void PrintLabelAction()
        {
            int copies = 1;
            if (SelectedPrinter != null)
            {
                //Send to print
                if (SelectedPrinter.Name.Contains("Twin Turbo"))
                {
                    int rollSel = SelectedRoll == "Auto" ? 0 : SelectedRoll == "Left" ? 1 : 2;
                    DymoPrinter.Instance.PrintLabel(dymoSDKLabel, SelectedPrinter.Name, copies, rollSelected: rollSel);
                }
                else
                    DymoPrinter.Instance.PrintLabel(dymoSDKLabel, SelectedPrinter.Name, copies);

                //If the label contains counter objects
                //Update counter object and preview to show the incresead value of the counter
                var counterObjs = dymoSDKLabel.GetLabelObjects().Where(w => w.Type == DymoSDK.Interfaces.TypeObject.CounterObject).ToList();
                if (counterObjs.Count > 0)
                {
                    foreach (var obj in counterObjs)
                        dymoSDKLabel.UpdateLabelObject(obj, copies.ToString());
                    UpdatePreviewAction();
                }
            }
        }
        /// <summary>
        /// Update the object value using the object name selected
        /// </summary>
        private void UpdateValueAction()
        {
            if (SelectedLabelObject != null)
            {
                //Update label object value
                dymoSDKLabel.UpdateLabelObject(SelectedLabelObject, ObjectValue);
                UpdatePreviewAction();
            }
        }
        /// <summary>
        /// Update the preview image of the label
        /// </summary>
        private void UpdatePreviewAction()
        {
            dymoSDKLabel.GetPreviewLabel();
            if (dymoSDKLabel != null)
                ImageSourcePreview = LoadImage(dymoSDKLabel.Preview);
        }
        /// <summary>
        /// Load the preview image label in the  image control
        /// </summary>
        /// <param name="array">Preview image content</param>
        /// <returns>Bitmap of the content</returns>
        private BitmapImage LoadImage(byte[] array)
        {
            using (var ms = new System.IO.MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }
        private async Task DisplayConsumableInformation()
        {
            ConsumableInfoText = string.Empty;
            if (SelectedPrinter != null)
            {
                if (await DymoPrinter.Instance.IsRollStatusSupported(SelectedPrinter.DriverName))
                {
                    //IMPORTANT: Get consumable information may return NULL when printer is connected to the machine
                    // we recommend wait a few seconds to establish connection with printer.
                    var rollStatusInPrinter = await DymoPrinter.Instance.GetRollStatusInPrinter(SelectedPrinter.DriverName);
                    if (rollStatusInPrinter != null)
                    {
                        ConsumableInfoText = $"Status: {rollStatusInPrinter.RollStatus} \nConsumable: {rollStatusInPrinter.Name} \nLabels remaining: {rollStatusInPrinter.LabelsRemaining}";
                    }
                }
            }
        }
        #endregion
    }
}
