using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Geography4Geek_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;

        public DebugController(ILogger<DebugController> logger)
        {
            _logger = logger;
        }

        [HttpGet("session")]
        public IActionResult CheckSession()
        {
            var sessQuizData = HttpContext.Session.GetString("QuizData");
            var sessQuizProgress = HttpContext.Session.GetString("QuizProgress");

            return Ok(new
            {
                hasQuizData = !string.IsNullOrEmpty(sessQuizData),
                hasQuizProgress = !string.IsNullOrEmpty(sessQuizProgress),
                sessionId = HttpContext.Session.Id
            });
        }
    }
}