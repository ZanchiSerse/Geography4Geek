// Pages/CreateTest.cshtml.cs
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Geography4Geek_1.Pages
{
    public class CreateTestModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateTestModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string Message { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // 1. Verifica se l'utente ZanchiSerse esiste
                var user = await _userManager.FindByNameAsync("ZanchiSerse");
                if (user == null)
                {
                    Message = "Errore: Utente ZanchiSerse non trovato";
                    return Page();
                }

                // 2. Verifica se abbiamo già un quiz di test
                var quiz = await _context.Quizzes.FirstOrDefaultAsync();

                // 3. Se non c'è nessun quiz, creane uno semplice
                if (quiz == null)
                {
                    quiz = new QuizModel  // Usa QuizModel invece di Quiz
                    {
                        Title = "Quiz di Prova",
                        Description = "Quiz creato per testare la dashboard",
                        CountryName = "Italia",
                        CountryCode = "IT",
                        Difficulty = "Facile",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Quizzes.Add(quiz);
                    await _context.SaveChangesAsync();
                }

                // 4. Crea un tentativo di quiz
                var attempt = new QuizAttempt
                {
                    UserId = user.Id,
                    QuizId = quiz.Id,
                    CountryName = "Italia",
                    Difficulty = "Facile",
                    Score = 7,
                    TotalQuestions = 10,
                    StartedAt = DateTime.UtcNow.AddMinutes(-15),
                    CompletedAt = DateTime.UtcNow
                };

                _context.QuizAttempts.Add(attempt);
                await _context.SaveChangesAsync();

                // Aggiungi alcune risposte di esempio
                var answers = new[]
                {
                    new QuizAnswer
                    {
                        QuizAttemptId = attempt.Id,
                        QuestionIndex = 0,
                        SelectedAnswerIndex = 2,
                        IsCorrect = true
                    },
                    new QuizAnswer
                    {
                        QuizAttemptId = attempt.Id,
                        QuestionIndex = 1,
                        SelectedAnswerIndex = 1,
                        IsCorrect = false
                    }
                };

                _context.QuizAnswers.AddRange(answers);
                await _context.SaveChangesAsync();

                Message = $"Test creato con successo! ID: {attempt.Id}";
                return Page();
            }
            catch (Exception ex)
            {
                Message = $"Errore: {ex.Message}";
                if (ex.InnerException != null)
                {
                    Message += $"\nDettagli: {ex.InnerException.Message}";
                }
                return Page();
            }
        }
    }
}