using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Geography4Geek_1.Models
{
    public class QuizGenerationRequest
    {
        [JsonPropertyName("countryName")]
        public string CountryName { get; set; } = string.Empty;

        [JsonPropertyName("questionCount")]
        public int QuestionCount { get; set; } = 10;

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "medium";
    }



    public class QuizData
    {
        public string CountryName { get; set; } = string.Empty;
        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }
}