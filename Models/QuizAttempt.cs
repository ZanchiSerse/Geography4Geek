// Models/QuizAttempt.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geography4Geek_1.Models
{
    // Models/QuizAttempt.cs - Modifica per supportare utenti anonimi
    public class QuizAttempt
    {
        public QuizAttempt()
        {
            Answers = new List<QuizAnswer>();
        }

        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Relazione opzionale con l'utente (nullable)
        public ApplicationUser? User { get; set; }

        public int QuizId { get; set; }

        public QuizModel? Quiz { get; set; }

        [Required]
        public string CountryName { get; set; } = string.Empty;

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        [NotMapped]
        public double ScorePercentage => TotalQuestions > 0 ? (Score * 100.0) / TotalQuestions : 0;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public virtual ICollection<QuizAnswer> Answers { get; set; }

        // Proprietà per verificare se l'utente è anonimo
        [NotMapped]
        public bool IsAnonymous => UserId.StartsWith("anon-");
    }
}