using OllamaSharp;
using ChromaDB.Client;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace ChatServer
{
    public class RAGService : Hub
    {
        private const string OllamaUri = "http://localhost:11434";
        private const string ChromaUri = "http://localhost:8000/api/v1";
        private const string WhisperUri = "http://localhost:5023";
        private const string OllamaProcessName = "ollama";
        private const string ChromaProcessName = "chroma";
        private const string WhisperProcessName = "faster-whisper";

        public OllamaApiClient _ollamaClient { get; private set; }
        public ChromaClient _chromaClient { get; private set; }
        public HttpClient _whisperClient { get; private set; }

        public RAGService()
        {
            bool isRunnning;

            // Start Olaama process
            try
            {
                Ollama();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error: {e.Message}");

                // Check if the Ollama process is running
                isRunnning = Process.GetProcessesByName(OllamaProcessName).Length != 0;
                if (!isRunnning)
                {
                    Trace.WriteLine("There is not Ollama process.");
                }

                // Start the Ollama process
            }

            // Start ChromaDB process
            try
            {
                Chroma();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error: {e.Message}");

                // Check if the ChromaDB process is running
                isRunnning = Process.GetProcessesByName(ChromaProcessName).Length != 0;
                if (!isRunnning)
                {
                    Trace.WriteLine("There is not ChromaDB process.");
                }

                // Start the ChromaDB process
            }

            // Start faster-whisper process
            try
            {
                _whisperClient = new HttpClient { BaseAddress = new Uri(WhisperUri) };
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error: {e.Message}");

                // Check if the faster-whisper process is running
                /*
                isRunnning = Process.GetProcessesByName(WhisperProcessName).Length != 0;
                if (!isRunnning)
                {
                    Trace.WriteLine("There is not faster-whisper process.");
                }
                */
                // Start the faster-whisper process
            }
        }

        /// <summary>
        /// Init Ollama.
        /// </summary>
        private async void Ollama()
        {
            // Initialize Ollama
            _ollamaClient = new OllamaApiClient(new Uri(OllamaUri));

            // Pull and select models.
            string response = string.Empty;
            _ollamaClient.SelectedModel = "phi3";

            try
            {
                await foreach (var status in _ollamaClient.PullModelAsync(_ollamaClient.SelectedModel))
                {
                    response += $"{status.Percent}% {status.Status}\r\n";
                    Trace.WriteLine($"{status.Percent}% {status.Status}");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error: {e.Message}");
            }
        }

        /// <summary>
        /// Init ChromaDB.
        /// </summary>
        private void Chroma()
        {
            // Initialize ChromaDB
            var options = new ChromaConfigurationOptions(ChromaUri);
            using var httpClient = new HttpClient();
            _chromaClient = new ChromaClient(options, httpClient);
        }

        /// <summary>
        /// Once the client connects, give the connection ID to the user.
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            await base.OnConnectedAsync();
            Trace.WriteLine($"[Server] Client Connected: {connectionId}");
        }

        /// <summary>
        /// Once the client disconnects, remove the connection ID from the user.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
         {
            string connectionId = Context.ConnectionId;
            await base.OnDisconnectedAsync(exception);
            Trace.WriteLine($"[Server] Client Disconnected: {connectionId}");
        }
    }
}
