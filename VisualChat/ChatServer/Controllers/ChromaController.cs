using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChromaController(RAGService ragService) : ControllerBase
    {
        private readonly RAGService _ragService = ragService;

        /// <summary>
        /// Query the ChromaDB.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("query/{userId}")]
        public IActionResult QueryAsync(string userId)
        {            
            _ = Task.Run(async () =>
            {
                string message = string.Empty;

                try
                {
                    // Query the database
                    Debug.WriteLine($"{DateTime.Now} Start query.");
                    var queryData = await _ragService.ChromaCollectionClient.Query([new(_ragService.QueryEmbedding)]);
                    Debug.WriteLine($"{DateTime.Now} End query.");

                    foreach (var item in queryData)
                    {
                        foreach (var entry in item)
                        {
                            message += $"{entry.Document}\r\n";
                        }
                    }
                }
                catch (Exception e)
                {
                    message = $"Error: {e.Message}";
                }
                finally
                {
                    try
                    {
                        await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "chroma/query", errorcode = 200, status = "Completed", content = message });
                        Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                    }
                    catch (Exception ex)
                    {
                        // If an error occurs when sending to the Hub.
                        Debug.WriteLine($"{DateTime.Now} Error sending to client: {ex.Message}");
                    }
                }

            }).ConfigureAwait(false);
            
            return Ok(new { result = "Accept", content = string.Empty });
        }
    }
}