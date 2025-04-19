using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AddnPrintHD.ViewModels
{
    /// <summary>
    /// Interaction logic for AddDeviceModelWindow.xaml
    /// </summary>
    public partial class AddDeviceModelWindow : Window
    {
        public string ModelName { get; private set; }
        public AddDeviceModelWindow()
        {
            InitializeComponent();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ModelName = ModelNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ModelName))
            {
                MessageBox.Show("Please enter a model name.");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
