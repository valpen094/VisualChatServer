﻿using OllamaSharp;
using ChromaDB.Client;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace ChatServer
{
    public class RAGService : Hub
    {
        private Tuple<string, string> OllamaUri { get; set; } = new("localhost", "11434");
        private Tuple<string, string> ChromaUri { get; set; } = new("localhost", "8000");
        private Tuple<string, string> WhisperUri { get; set; } = new("localhost", "5023");

        private const string OllamaProcessName = "ollama";
        private const string ChromaProcessName = "chroma";
        private const string WhisperProcessName = "faster-whisper";
        
        private const string ChromaCollectionName = "docs";

        public OllamaApiClient? OllamaClient { get; private set; }
        public ChromaClient? ChromaClient { get; private set; }
        private HttpClient? ChromaHttpClient { get; set; }
        public ChromaCollectionClient? ChromaCollectionClient { get; private set; }
        public HttpClient? WhisperClient { get; private set; }

        /// <summary>
        /// Numeric Vector Data
        /// </summary>
        public float[]? QueryEmbedding { get; set; } = [0,0f, 1.1f, 2.2f, 3.3f, 4.4f,5.5f, 6.6f, 7.7f, 8.8f, 9.9f];

        public RAGService()
        {
            Ollama();
            Chroma(); 
            Whisper();
        }

        /// <summary>
        /// Start the service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Task> StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop the service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            void CloseServer(string processName) => Process.GetProcessesByName(processName).ToList().ForEach(process =>
            {
                Debug.WriteLine($"{DateTime.Now} {processName} is existed.");

                try
                {
                    process.Kill();
                    process.WaitForExit();
                    Debug.WriteLine($"{DateTime.Now} Process {processName} terminated.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{DateTime.Now} Error: {ex.Message}");
                }
            });

            CloseServer(OllamaProcessName);   // Kill the Ollama process.
            CloseServer(ChromaProcessName);   // Kill the ChromaDB process.
            CloseServer(WhisperProcessName);  // Kill the ChromaDB process.

            return Task.CompletedTask;
        }

        /// <summary>
        /// Init Ollama.
        /// </summary>
        private async void Ollama()
        {
            // Check if the Ollama process is running
            bool isRunning = Process.GetProcessesByName(OllamaProcessName).Length != 0;
            if (!isRunning)
            {
                Debug.WriteLine($"{DateTime.Now} There is not Ollama process.");

                // Start the Ollama process
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c ollama serve",
                        CreateNoWindow = true, // true: not display window, false: display window
                        UseShellExecute = false
                    };

                    Process.Start(psi);
                    Debug.WriteLine($"{DateTime.Now} Start the ollama server.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{DateTime.Now} Error: {ex.Message}");
                }
            }

            try
            {
                // Initialize Ollama
                string url = $"http://{OllamaUri.Item1}:{OllamaUri.Item2}";

                Debug.WriteLine($"{DateTime.Now} Connecting to {url}...");
                OllamaClient = new OllamaApiClient(new Uri(url));
                Debug.WriteLine($"{DateTime.Now} Connected to {url}");

                // Pull and select models.
                string response = string.Empty;
                OllamaClient.SelectedModel = "phi3";

                await foreach (var status in OllamaClient.PullModelAsync(OllamaClient.SelectedModel))
                {
                    response += $"{status.Percent}% {status.Status}\r\n";
                    Debug.WriteLine($"{DateTime.Now} {status.Percent}% {status.Status}");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{DateTime.Now} Error: {e.Message}");
            }
        }

        /// <summary>
        /// Init ChromaDB.
        /// </summary>
        private async void Chroma()
        {
            // Check if the ChromaDB process is running
            bool isRunning = Process.GetProcessesByName(ChromaProcessName).Length != 0;
            if (!isRunning)
            {
                Debug.WriteLine($"{DateTime.Now} There is not ChromaDB process.");

                // Start the ChromaDB process
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c chroma.exe run --path \"..\\chromadb\" --host {ChromaUri.Item1} --port {ChromaUri.Item2}",
                        CreateNoWindow = true, // true: not display window, false: display window
                        UseShellExecute = false
                    };

                    Process.Start(psi);
                    Debug.WriteLine($"{DateTime.Now} Start the ChromaDB server.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{DateTime.Now} Error: {ex.Message}");
                }
            }

            try
            {
                // Initialize ChromaDB
                string url = $"http://{ChromaUri.Item1}:{ChromaUri.Item2}/api/v1/";
                var options = new ChromaConfigurationOptions(url);
                ChromaHttpClient = new HttpClient();

                Debug.WriteLine($"{DateTime.Now} Connecting to {url}...");
                ChromaClient = new ChromaClient(options, ChromaHttpClient);
                Debug.WriteLine($"{DateTime.Now} Connected to {url}");

                // Create or Get a collection
                var collection = await ChromaClient.GetOrCreateCollection(ChromaCollectionName);
                ChromaCollectionClient = new ChromaCollectionClient(collection, options, ChromaHttpClient);
                Debug.WriteLine($"{DateTime.Now} Collection obtained.");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{DateTime.Now} Error: {e.Message}");
            }
        }

        /// <summary>
        /// Init faster-whisper.
        /// </summary>
        private void Whisper()
        {
            // Check if the faster-whisper process is running
            bool isRunning = Process.GetProcessesByName(WhisperProcessName).Length != 0;
            if (!isRunning)
            {
                /*
                Debug.WriteLine($"{DateTime.Now} There is not faster-whisper process.");

                // Start the faster-whisper process
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "",
                        Arguments = "",
                        CreateNoWindow = false, // true: not display window, false: display window
                        UseShellExecute = false
                    };

                    Process.Start(psi);
                    Debug.WriteLine($"{DateTime.Now} Start the faster-whisper server.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{DateTime.Now} Error: {ex.Message}");
                }
                */
            }

            try
            {
                string url = $"http://{WhisperUri.Item1}:{WhisperUri.Item2}";

                Debug.WriteLine($"{DateTime.Now} Connecting to {url}...");
                WhisperClient = new HttpClient { BaseAddress = new Uri(url) };
                Debug.WriteLine($"{DateTime.Now} Connected to {url}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{DateTime.Now} Error: {e.Message}");
            }
        }

        /// <summary>
        /// Once the client connects, give the connection ID to the user.
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            await base.OnConnectedAsync();
            Debug.WriteLine($"{DateTime.Now} Client Connected: {connectionId}");
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
            Debug.WriteLine($"{DateTime.Now} Client Disconnected: {connectionId}");
        }
    }
}