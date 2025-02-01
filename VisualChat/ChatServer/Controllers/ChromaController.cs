using Microsoft.AspNetCore.Mvc;
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
        /// <param name="prompt"></param>
        /// <returns></returns>
        [HttpGet("query/{userId}")]
        public IActionResult Query(string userId, string prompt)
        {
            return Ok(new { result = "Accept", details = string.Empty });
        }
    }
}