using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Geography4Geek_1.Models
{
    public class QuizModel
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string CountryName { get; set; } = string.Empty;

        // Rimosso [Required] per permettere valori null nei record esistenti
        public string? CountryCode { get; set; }

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        // Proprietà aggiunta per compatibilità con GeminiService
        public int DifficultyLevel => Difficulty switch
        {
            "Facile" => 1,
            "Media" => 2,
            "Difficile" => 3,
            _ => 1 // Valore predefinito se non corrisponde a nessuno dei casi
        };

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Proprietà di navigazione
        public virtual ICollection<QuizQuestion>? Questions { get; set; }

        // Metodo helper per impostare le domande (per GeminiService)
        public void SetQuestions(List<QuizQuestion> questions)
        {
            Questions = questions;
        }
    }
}