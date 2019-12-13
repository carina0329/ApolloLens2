using ApolloLensLibrary.Signaller;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Markup;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Newtonsoft.Json;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Reflection;
using Newtonsoft.Json.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private UserConfig Config { get; } = UserConfig.Instance;

        public SettingsPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.SignallerAddressInput.Text = (string)Config.GetProperty("Signaller.Address");
                this.SignallerPortInput.Text = (string)Config.GetProperty("Signaller.Port");
                this.SecureSignallerSwitch.IsOn = (bool)Config.GetProperty("Signaller.Secure");
                string selectedColor = (string)Config.GetProperty("Cursor.Color");

                this.CursorColorComboBox.Items.Clear();
                int index = 0;
                foreach (var prop in typeof(Colors).GetProperties())
                {
                    this.CursorColorComboBox.Items.Add(prop.Name);
                    if (prop.Name.ToLower() == selectedColor.ToLower())
                    {
                        this.CursorColorComboBox.SelectedItem = this.CursorColorComboBox.Items[index];
                    }
                    index++;
                }
            });
        }

        #region FunctionalUIHandlers        

        private void CancelSaveButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this.Config.SaveProperty("Signaller.Address", this.SignallerAddressInput.Text);
            this.Config.SaveProperty("Signaller.Port", this.SignallerPortInput.Text);
            this.Config.SaveProperty("Signaller.Secure", this.SecureSignallerSwitch.IsOn);
            this.Config.SaveProperty("Cursor.Color", this.CursorColorComboBox.SelectedValue.ToString());
            this.Config.SaveSettings();

            this.Frame.Navigate(typeof(MainPage));
        }
    }

    #endregion
}
