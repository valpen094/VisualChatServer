using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Net.Sockets;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhisperController(RAGService ragService) : ControllerBase
    {
        private readonly RAGService _ragService = ragService;

        /// <summary>
        /// Open the faster-whisper_server.
        /// </summary>
        /// <returns></returns>
        [HttpGet("open/{userId}")]
        public IActionResult Open()
        {
            string message = string.Empty;

            try
            {
                _ragService._whisperClient = new TcpClient(_ragService.WhisperUri.Item1, _ragService.WhisperUri.Item2);
                _ragService._stream = _ragService._whisperClient.GetStream();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error: {e.Message}");
            }

            return Ok(new { result = "Accept", content = message });
        }

        /// <summary>
        /// Close the faster-whisper_server.
        /// </summary>
        /// <returns></returns>
        [HttpGet("close/{userId}")]
        public IActionResult Close()
        {
            string message = string.Empty;

            try
            {
                // Send a message to the server.
                byte[] data = Encoding.UTF8.GetBytes("close");
                _ragService._stream.Write(data, 0, data.Length);
                _ragService._whisperClient.Close();
                _ragService._stream.Close();
            }
            catch (ObjectDisposedException)
            {

            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error: {e.Message}");
            }

            return Ok(new { result = "Accept", content = string.Empty });
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
                    // Send a message to the server.
                    byte[] data = Encoding.UTF8.GetBytes("transcribe");
                    _ragService._stream.Write(data, 0, data.Length);

                    // Receive the response from the server.
                    byte[] buffer = new byte[1024];
                    int bytesRead = _ragService._stream.Read(buffer, 0, buffer.Length);
                    message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Trace.WriteLine("POST Response: " + message);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error: " + ex.Message);
                }

            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }
    }
}
