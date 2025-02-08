using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Net.Sockets;
using System;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhisperController : ControllerBase
    {
        private readonly RAGService _ragService;

        public WhisperController(RAGService ragService)
        {
            _ragService = ragService;
        }

        /// <summary>
        /// Record the voice and send the result to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("record/{userId}")]
        public IActionResult RecordAsync(string userId)
        {
            string message = string.Empty;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                try
                {
                    const string url = "faster-whisper/api/record";

                    string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
                    var jsonData = new { filePath };
                    string jsonString = JsonSerializer.Serialize(jsonData);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // Send a message to the server.
                    HttpResponseMessage response = await _ragService._whisperClient.PostAsync(url, content);

                    var statusCode = response.StatusCode.ToString();
                    int statusCodeValue = (int)response.StatusCode;

                    // Receive the response from the server.
                    message = await response.Content.ReadAsStringAsync();
                    Trace.WriteLine($"{DateTime.Now.ToString()} POST Response: {message}");

                    await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/record", errorcode = statusCodeValue, status = statusCode, content = message });
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error: " + ex.Message);
                }

            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("transcribe/{userId}")]
        public IActionResult TranscribeAsync(string userId)
        {
            string message = string.Empty;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                try
                {
                    const string url = "faster-whisper/api/transcribe";

                    string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
                    var jsonData = new { filePath };
                    string jsonString = JsonSerializer.Serialize(jsonData);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // Send a message to the server.
                    HttpResponseMessage response = await _ragService._whisperClient.PostAsync(url, content);

                    var statusCode = response.StatusCode.ToString();
                    int statusCodeValue = (int)response.StatusCode;

                    // Receive the response from the server.
                    message = await response.Content.ReadAsStringAsync();
                    Trace.WriteLine($"{DateTime.Now.ToString()} POST Response: {message}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<TranscriptionResult>(message);
                        if (result != null)
                        {
                            foreach (var segment in result.segments)
                            {
                                Trace.WriteLine($"{DateTime.Now.ToString()} [{segment.start}s - {segment.end}s] {segment.text}");
                            }
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"{DateTime.Now.ToString()} Error: {response.StatusCode}");
                    }

                    await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/transcribe", errorcode = statusCodeValue, status = statusCode, content = message });
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error: " + ex.Message);
                }

            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Record and transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("whisper/{userId}")]
        public IActionResult WhisperAsync(string userId)
        {
            string message = string.Empty;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                try
                {
                    const string url = "faster-whisper/api/whisper";

                    string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
                    var jsonData = new { filePath };
                    string jsonString = JsonSerializer.Serialize(jsonData);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // Send a message to the server.
                    HttpResponseMessage response = await _ragService._whisperClient.PostAsync(url, content);

                    var statusCode = response.StatusCode.ToString();
                    int statusCodeValue = (int)response.StatusCode;

                    // Receive the response from the server.
                    message = await response.Content.ReadAsStringAsync();
                    Trace.WriteLine($"{DateTime.Now.ToString()} POST Response: {message}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<TranscriptionResult>(message);
                        if (result != null)
                        {
                            foreach (var segment in result.segments)
                            {
                                Trace.WriteLine($"{DateTime.Now.ToString()} [{segment.start}s - {segment.end}s] {segment.text}");
                            }
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"{DateTime.Now.ToString()} Error: {response.StatusCode}");
                    }

                    await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/whisper", errorcode = statusCodeValue, status = statusCode, content = message });
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error: " + ex.Message);
                }

            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        public class TranscriptionResult
        {
            public List<Segment> segments { get; set; }
        }

        public class Segment
        {
            public float start { get; set; }
            public float end { get; set; }
            public string text { get; set; }
        }
    }
}
