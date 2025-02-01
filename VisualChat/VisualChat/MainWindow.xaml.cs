using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;

namespace VisualChat
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient();
        private HubConnection _connection;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ConnectToSignalR();
        }

        private async void OnRequestApi(object sender, RoutedEventArgs e)
        {
            string response = string.Empty;

            try
            {
                const string url = "http://localhost:5028/api/Ollama/pull/1?model=phi3";
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
                    Trace.WriteLine($"{response}");
                });
            });

            // 接続開始
            try
            {
                await _connection.StartAsync();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"SignalR connection failed: {e.Message}");
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
    }
}
