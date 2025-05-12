using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geography4Geek_1.Models
{
    public class Question
    {
        public int Id { get; set; }

        public int QuizId { get; set; }
        public QuizModel? Quiz { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string Options { get; set; } = "[]"; // JSON array

        public int CorrectAnswerIndex { get; set; }

        public string? Explanation { get; set; }

        public string? Category { get; set; }

        public string? Topic { get; set; }

        public int QuestionNumber { get; set; }
    }
}