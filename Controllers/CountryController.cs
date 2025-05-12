using Geography4Geek_1.Models;
using Geography4Geek_1.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Geography4Geek_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly IGeminiService _geminiService;

        public CountryController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpGet("{countryName}")]
        public async Task<IActionResult> GetCountryInfo(string countryName)
        {
            var info = await _geminiService.GetCountryInfoAsync(countryName);
            return Ok(new { info });
        }

        [HttpGet("{countryName}/quiz")]
        public async Task<IActionResult> GenerateQuiz(string countryName, [FromQuery] int difficulty = 2, [FromQuery] int questions = 5)
        {
            var quiz = await _geminiService.GenerateCountryQuizAsync(countryName, difficulty, questions);
            return Ok(quiz);
        }
    }
}