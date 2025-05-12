// Controllers/QuizResultController.cs - versione corretta per qualsiasi utente
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Geography4Geek_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizResultController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizResultController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveQuizResult([FromBody] QuizResultRequest request)
        {
            try
            {
                Console.WriteLine($"API chiamata - Dati ricevuti: {System.Text.Json.JsonSerializer.Serialize(request)}");

                // 1. Ottieni l'ID dell'utente corrente
                string userId;

                if (User.Identity.IsAuthenticated)
                {
                    // Usa l'utente autenticato
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    Console.WriteLine($"Utente autenticato: {User.Identity.Name}, ID: {userId}");
                }
                else
                {
                    // In caso di utente non autenticato, usa un ID anonimo basato sul timestamp
                    // Utile per test e per consentire ai visitatori di fare quiz senza account
                    userId = $"anon-{DateTime.UtcNow.Ticks}";
                    Console.WriteLine($"Nessun utente autenticato, usando ID temporaneo: {userId}");
                }

                // 2. Trova o crea un quiz
                int quizId = request.QuizId;
                var quiz = await _context.Quizzes.FindAsync(quizId);

                if (quiz == null && quizId > 0)
                {
                    // Se è stato fornito un ID quiz ma non è stato trovato
                    Console.WriteLine($"Quiz con ID {quizId} non trovato");
                    return BadRequest(new { success = false, message = $"Quiz con ID {quizId} non trovato" });
                }
                else if (quiz == null)
                {
                    // Crea un quiz temporaneo solo se non è stato specificato un ID
                    // MODIFICA: Rimosso CountryCode dalla creazione del quiz
                    quiz = new QuizModel
                    {
                        Title = $"Quiz su {request.CountryName}",
                        Description = $"Quiz generato automaticamente",
                        CountryName = request.CountryName ?? "Sconosciuto",
                        // Aggiungi questa riga:
                        CountryCode = request.CountryCode ?? ((request.CountryName?.Length >= 2) ?
                  request.CountryName.Substring(0, 2).ToUpper() : "XX"),
                        Difficulty = request.Difficulty ?? "Facile",
                        CreatedAt = DateTime.UtcNow
                    };


                    _context.Quizzes.Add(quiz);
                    await _context.SaveChangesAsync();

                    quizId = quiz.Id;
                    Console.WriteLine($"Creato quiz temporaneo con ID {quizId}");
                }

                // 3. Crea il tentativo con i dati ricevuti
                var attempt = new QuizAttempt
                {
                    UserId = userId,
                    QuizId = quizId,
                    CountryName = request.CountryName ?? "Sconosciuto",
                    Difficulty = request.Difficulty ?? "Facile",
                    Score = request.Score,
                    TotalQuestions = request.TotalQuestions,
                    StartedAt = request.StartedAt ?? DateTime.UtcNow.AddMinutes(-10),
                    CompletedAt = DateTime.UtcNow
                };

                _context.QuizAttempts.Add(attempt);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Quiz salvato con successo, ID: {attempt.Id}");
                return Ok(new
                {
                    success = true,
                    message = "Risultato quiz salvato con successo",
                    attemptId = attempt.Id,
                    userId = userId,
                    isAuthenticated = User.Identity.IsAuthenticated
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'API: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Classe per la richiesta API
        public class QuizResultRequest
        {
            public int QuizId { get; set; }
            public string? CountryName { get; set; }
            public string? CountryCode { get; set; }  // Aggiungi questa riga
            public string? Difficulty { get; set; }
            public int Score { get; set; }
            public int TotalQuestions { get; set; }
            public DateTime? StartedAt { get; set; }
        }

    }
}