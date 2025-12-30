using System.Linq;
using System.Windows;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.windows
{
    public partial class SettingWindow : Window
    {
        private readonly Setting _setting;

        public SettingWindow(Setting setting)
        {
            InitializeComponent();
            _setting = setting;

            EnableTtsCheckBox.IsChecked = _setting.EnableTts;
            TtsVoiceTextBox.Text = _setting.TtsVoice;

            var devices = AudioDeviceService.GetRenderDevices();
            devices.Insert(0, new AudioDeviceInfo { Id = "", Name = "(System default)" });
            TtsOutputDeviceComboBox.ItemsSource = devices;
            TtsOutputDeviceComboBox.SelectedValue = _setting.TtsOutputDeviceId ?? "";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _setting.EnableTts = EnableTtsCheckBox.IsChecked == true;
            _setting.TtsVoice = TtsVoiceTextBox.Text.Trim();
            var selected = (string?)TtsOutputDeviceComboBox.SelectedValue;
            _setting.TtsOutputDeviceId = string.IsNullOrWhiteSpace(selected) ? null : selected;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
