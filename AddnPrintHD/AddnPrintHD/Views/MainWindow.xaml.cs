using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AddnPrintHD.Models;
using AddnPrintHD.Services;
using AddnPrintHD.ViewModels;
using static AddnPrintHD.ViewModels.MainViewModel;

namespace AddnPrintHD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DeviceModelService _deviceModelService;
        public ObservableCollection<DeviceModel> DeviceModels { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _deviceModelService = new DeviceModelService();
            this.DataContext = new MainViewModel();
            DeviceModels = new ObservableCollection<DeviceModel>();
            DeviceModelComboBox.ItemsSource = DeviceModels; // Bind ComboBox to DeviceModels
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Asynchronously fetch the device models and bind them to the ComboBox
                await LoadDeviceModels();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading device models: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDeviceModels()
        {
            try
            {
                // Fetch device models asynchronously from the service
                var deviceModels = await _deviceModelService.GetDeviceModelsAsync();

                if (deviceModels != null && deviceModels.Count > 0)
                {
                    // Clear current items and add the new ones
                    DeviceModels.Clear();
                    foreach (var device in deviceModels)
                    {
                        DeviceModels.Add(device);
                    }
                }
                else
                {
                    MessageBox.Show("No devices found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading device models: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddNewModel_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddDeviceModelWindow();
            if (addWindow.ShowDialog() == true)
            {
                try
                {
                    string newModelName = addWindow.ModelName;

                    Guid newModelId = await _deviceModelService.CreateDeviceModelAsync(newModelName);

                    MessageBox.Show("New device model added!");

                    await LoadDeviceModels();

                    var added = DeviceModels.FirstOrDefault(m => m.DeviceModelId == newModelId);
                    if (added != null)
                        DeviceModelComboBox.SelectedItem = added;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding model: {ex.Message}");
                }
            }
        }
    }
}

