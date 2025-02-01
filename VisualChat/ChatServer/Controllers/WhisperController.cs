using Microsoft.AspNetCore.Mvc;
using Whisper.net.Logger;
using Whisper.net;
using NAudio.Wave;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using OllamaSharp.Models.Chat;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhisperController(RAGService ragService) : ControllerBase
    {
        private readonly RAGService _ragService = ragService;

        /// <summary>
        /// Record the voice and send the result to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("record/{userId}")]
        public IActionResult Record(string userId)
        {
            const string wavFileName = "voice.wav";
            string modelFileName = $"{Directory.GetCurrentDirectory()}\\ggml-base.bin";
            string messsage = string.Empty;

            // Optional logging from the native library
            using var whisperLogger = LogProvider.AddConsoleLogging(WhisperLogLevel.None);

            // This section creates the whisperFactory object which is used to create the processor object.
            using var whisperFactory = WhisperFactory.FromPath("ggml-base.bin");

            // This section creates the processor object which is used to process the audio file, it uses language `auto` to detect the language of the audio file.
            // It also sets the segment event handler, which is called every time a new segment is detected.
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .WithSegmentEventHandler(async (segment) =>
                {
                    // Get a text
                    messsage = segment.Text;

                    try
                    {
                        // Delete the audio file
                        System.IO.File.Delete(wavFileName);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e.Message);
                    }
                    finally
                    {
                        await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/record", errorcode = 200, status = "Completed", details = messsage });
                    }
                })
                .Build();

            using (var waveIn = new WaveInEvent())
            {
                waveIn.WaveFormat = new WaveFormat(16000, 1);

                using var waveWriter = new WaveFileWriter(wavFileName, waveIn.WaveFormat);
                waveIn.DataAvailable += (sender, e) =>
                {
                    waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                };

                // Start recording
                waveIn.StartRecording();

                Trace.WriteLine("Please speak into the microphone.\r\n* recording... [Enter]");
                Console.ReadLine();

                // Stop recording
                waveIn.StopRecording();
            }

            try
            {
                // This section processes the audio file and prints the results (start time, end time and text) to the console.
                using var fileStream = System.IO.File.OpenRead(wavFileName);
                processor.Process(fileStream);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return StatusCode(500, new { result = "Failed", details = e.Message });   // 500 Internal Server Error
            }

            return Ok(new { result = "Accept", details = string.Empty });
        }
    }
}
