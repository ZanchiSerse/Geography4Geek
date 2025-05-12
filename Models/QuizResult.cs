using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geography4Geek_1.Models
{
    public class QuizResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public int QuizId { get; set; }

        [ForeignKey("QuizId")]
        public virtual QuizModel Quiz { get; set; } = null!;

        [Required]
        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        [NotMapped] // Questa proprietà viene calcolata e non salvata nel database
        public double ScorePercentage => TotalQuestions > 0 ? (double)Score / TotalQuestions * 100 : 0;
    }
}