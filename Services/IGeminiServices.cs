using Geography4Geek_1.Models;
using System.Threading.Tasks;

namespace Geography4Geek_1.Services
{
    public interface IGeminiService
    {
        Task<QuizModel> GenerateCountryQuizAsync(string countryName, int difficultyLevel = 2, int questionCount = 5);
        Task<string> GetCountryInfoAsync(string countryName);
    }
}