// Models/QuizAnswer.cs
using System.ComponentModel.DataAnnotations;

namespace Geography4Geek_1.Models
{
    public class QuizAnswer
    {
        public int Id { get; set; }

        public int QuizAttemptId { get; set; }

        public QuizAttempt? QuizAttempt { get; set; }

        public int QuestionIndex { get; set; }

        public int SelectedAnswerIndex { get; set; }

        public bool IsCorrect { get; set; }

        // Proprietà opzionale per associare alle domande
        public int? QuestionId { get; set; }

        // Riferimento alla domanda - può essere null
        public QuizQuestion? Question { get; set; }
    }
}