using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Geography4Geek_1.Pages.Teacher
{
    [Authorize(Roles = "Admin,Teacher")]
    public class StudentDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StudentDetailsModel> _logger;

        public StudentDetailsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<StudentDetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;

            // Inizializza le collezioni e proprietà nullable
            Attempts = new List<QuizAttempt>();
            CountryChartData = new List<CountryChartItem>();
            MostFrequentMistakes = new List<FrequentMistake>();

            // Inizializza lo Student per evitare errori nullable
            Student = new ApplicationUser();
        }

        public ApplicationUser Student { get; set; }
        public List<QuizAttempt> Attempts { get; set; }
        public double AvgScore { get; set; }
        public double BestScore { get; set; }
        public DateTime? LastAttemptDate { get; set; }
        public List<CountryChartItem> CountryChartData { get; set; }
        public List<FrequentMistake> MostFrequentMistakes { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                _logger.LogInformation("Loading student details for user ID {id}", id);

                // Carica lo studente
                var studentUser = await _userManager.FindByIdAsync(id);
                if (studentUser == null)
                {
                    _logger.LogWarning("Student with ID {id} not found", id);
                    return NotFound("Studente non trovato");
                }

                // Assegna lo studente trovato
                Student = studentUser;

                // Carica i tentativi di quiz - MODIFICA: Usa QuizAttempts invece di QuizResults
                Attempts = await _context.QuizAttempts
                    .Include(a => a.Quiz)
                    .Where(a => a.UserId == id && a.CompletedAt != null)
                    .OrderByDescending(a => a.CompletedAt)
                    .ToListAsync();

                // Calcola statistiche
                if (Attempts.Any())
                {
                    AvgScore = Attempts.Average(a => a.ScorePercentage);
                    BestScore = Attempts.Max(a => a.ScorePercentage);
                    LastAttemptDate = Attempts.Max(a => a.CompletedAt);

                    // Prepara dati per grafico per paese
                    CountryChartData = Attempts
                        .GroupBy(a => a.CountryName)
                        .Select(g => new CountryChartItem
                        {
                            Country = g.Key,
                            Count = g.Count(),
                            AvgScore = g.Average(a => a.ScorePercentage)
                        })
                        .OrderByDescending(c => c.Count)
                        .ToList();

                    // Analizza gli errori più frequenti
                    await AnalyzeFrequentMistakes(id);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student details for user ID {id}", id);
                return StatusCode(500);
            }
        }

        private async Task AnalyzeFrequentMistakes(string userId)
        {
            try
            {
                // Ottieni tutte le risposte errate - MODIFICA: Usa QuizAnswers invece di QuizResults.Answers
                var wrongAnswers = await _context.QuizAnswers
                    .Include(a => a.Question)
                    .Include(a => a.QuizAttempt)
                    .Where(a => a.QuizAttempt != null && a.QuizAttempt.UserId == userId && !a.IsCorrect)
                    .ToListAsync();

                if (!wrongAnswers.Any())
                    return;

                // Raggruppa per categoria e conta gli errori
                var mistakesByCategory = wrongAnswers
                    .Where(a => a.Question != null)
                    .GroupBy(a => a.Question?.Category ?? "Generale")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        Questions = g.Select(a => a.Question).Where(q => q != null).ToList()
                    })
                    .OrderByDescending(g => g.Count)
                    .Take(3)
                    .ToList();

                // Crea l'elenco degli errori frequenti
                foreach (var category in mistakesByCategory)
                {
                    var relatedTopics = category.Questions
                        .Where(q => q != null)
                        .Select(q => q!.Topic ?? q.Category ?? "Generale")
                        .Distinct()
                        .Take(5)
                        .ToList();

                    MostFrequentMistakes.Add(new FrequentMistake
                    {
                        CategoryName = category.Category,
                        ErrorCount = category.Count,
                        Description = GetCategoryDescription(category.Category),
                        RelatedTopics = relatedTopics
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing frequent mistakes for user {userId}", userId);
            }
        }

        private string GetCategoryDescription(string category)
        {
            return category.ToLower() switch
            {
                "geografia" => "Lo studente ha difficoltà con domande relative alla geografia fisica e politica.",
                "storia" => "Lo studente mostra incertezze sugli eventi storici e le date significative.",
                "cultura" => "Ci sono lacune nelle conoscenze relative agli aspetti culturali e tradizioni.",
                "economia" => "Lo studente ha difficoltà con domande relative all'economia e commercio.",
                "politica" => "Ci sono incertezze sui sistemi politici e governativi.",
                "lingue" => "Lo studente mostra difficoltà con le domande relative alle lingue e gruppi etnici.",
                _ => $"Lo studente ha commesso diversi errori nella categoria '{category}'."
            };
        }

        public class CountryChartItem
        {
            public string Country { get; set; } = string.Empty;
            public int Count { get; set; }
            public double AvgScore { get; set; }
        }

        public class FrequentMistake
        {
            public string CategoryName { get; set; } = string.Empty;
            public int ErrorCount { get; set; }
            public string Description { get; set; } = string.Empty;
            public List<string> RelatedTopics { get; set; } = new List<string>();
        }
    }
}