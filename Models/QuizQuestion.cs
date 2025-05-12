using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Geography4Geek_1.Models
{

    // Models/QuizQuestion.cs
 
        public class QuizQuestion
        {
            public int Id { get; set; }

            public int QuizId { get; set; }

            public QuizModel? Quiz { get; set; }

            [Required]
            public string Question { get; set; } = string.Empty;

            // Proprietà aggiunta per compatibilità con le viste
            public string Text => Question;

            // Proprietà aggiunta per compatibilità con StudentDetailsModel
            public string? Topic { get; set; }

            // Memorizzato come JSON
            public string OptionsJson { get; set; } = "[]";

            public int CorrectAnswerIndex { get; set; }

            public List<string> Options
            {
                get
                {
                    try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(OptionsJson) ?? new List<string>(); }
                    catch { return new List<string>(); }
                }
                set
                {
                    OptionsJson = System.Text.Json.JsonSerializer.Serialize(value);
                }
            }

            public string? Explanation { get; set; }

            public string Category { get; set; } = "Geografia";
        }
    


    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        ShortAnswer
    }
}