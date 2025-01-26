using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VisualChat
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void OnRequestApi(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = "http://localhost:5028/hello"; // APIのエンドポイント
                string response = await _httpClient.GetStringAsync(url);
                ResponseText.Text = response;
            }
            catch (Exception ex)
            {
                ResponseText.Text = "エラー: " + ex.Message;
            }
        }
    }
}
