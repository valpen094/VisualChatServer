using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNetCore.SignalR.Client;

namespace VisualChat
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> Apis { get; set; } = [];

        private readonly HttpClient _client = new HttpClient();
        private HubConnection? _connection;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApiComboBox.Items.Clear();
            CategoryComboBox.SelectedIndex = 0;

            await ConnectToSignalR();
        }

        private async void OnRequestApi(object sender, RoutedEventArgs e)
        {
            string response = string.Empty;
            string category = CategoryComboBox.Text;
            string api = ApiComboBox.Text;

            try
            {
                string url = $"http://localhost:5028/api/{category}/{api}/1";
                response = await _client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                response = "Error: " + ex.Message;
            }
            finally
            {
                ChatTextBox.Text = response;
            }
        }

        /// <summary>
        /// Connect to SignalR.
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToSignalR()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5028/ragService", options =>
                {
                    options.Headers["userId"] = "1";
                })
                .Build();

            // Receive a recorded file
            _connection.On<string>("ReceiveResult", (response) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ChatTextBox.Text += $"{response}\n";
                    Debug.WriteLine($"{response}");
                });
            });

            // 接続開始
            try
            {
                await _connection.StartAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"SignalR connection failed: {e.Message}");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _client.Dispose();
                _connection.Remove("ReceiveResult");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedValue = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            if (selectedValue == null)
            {
                return;
            }

            List<string> categories = selectedValue switch
            {
                "Ollama" => ["pull", "chat", "generate", "embed", "select"],
                "Chroma" => ["query"],
                "Whisper" => ["record", "transcribe", "whisper"],
                "General" => ["alive"],
                _ => ["dummy"],
            };

            Apis.Clear();

            foreach (var api in categories)
            {
                Apis.Add(api);
            }

            ApiComboBox.ItemsSource = Apis;
            ApiComboBox.SelectedIndex = 0;
        }
    }
}
