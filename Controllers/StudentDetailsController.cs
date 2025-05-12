using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Geography4Geek_1.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class StudentResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StudentResultsController> _logger;

        public StudentResultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<StudentResultsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Dashboard principale con statistiche
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Statistiche di base
                var totalStudents = await _context.Users.CountAsync();
                var totalQuizzes = await _context.Quizzes.CountAsync();
                var totalAttempts = await _context.QuizAttempts.CountAsync();

                // Quiz recenti
                var recentAttempts = await _context.QuizAttempts
                    .Include(a => a.User)
                    .Include(a => a.Quiz)
                    .OrderByDescending(a => a.CompletedAt)
                    .Take(10)
                    .ToListAsync();

                // Punteggio medio
                var avgScore = 0.0;
                if (await _context.QuizAttempts.AnyAsync(a => a.CompletedAt != null))
                {
                    avgScore = await _context.QuizAttempts
                        .Where(a => a.CompletedAt != null)
                        .Select(a => a.ScorePercentage)
                        .AverageAsync();
                }

                // Dati per grafici
                var countryData = await _context.QuizAttempts
                    .Where(a => a.CompletedAt != null)
                    .GroupBy(a => a.CountryName)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                var difficultyData = await _context.QuizAttempts
                    .Where(a => a.CompletedAt != null)
                    .GroupBy(a => a.Difficulty)
                    .Select(g => new { Difficulty = g.Key, Count = g.Count() })
                    .ToListAsync();

                ViewBag.TotalStudents = totalStudents;
                ViewBag.TotalQuizzes = totalQuizzes;
                ViewBag.TotalAttempts = totalAttempts;
                ViewBag.AvgScore = avgScore;
                ViewBag.CountryData = countryData;
                ViewBag.DifficultyData = difficultyData;
                ViewBag.CurrentUser = User.Identity?.Name ?? "ZanchiSerse";
                ViewBag.CurrentDateTime = DateTime.Parse("2025-05-12 17:03:06"); // Data statica

                return View(recentAttempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il caricamento della dashboard");
                return View("Error", new ErrorViewModel { RequestId = ex.Message });
            }
        }

        // Elenco di tutti i risultati dei quiz
        public async Task<IActionResult> Index()
        {
            var quizAttempts = await _context.QuizAttempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            return View(quizAttempts);
        }

        // Dettaglio di un tentativo di quiz
        public async Task<IActionResult> Details(int id)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .Include(a => a.Answers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attempt == null)
            {
                return NotFound();
            }

            return View(attempt);
        }

        // Storico quiz per uno studente specifico
        public async Task<IActionResult> StudentHistory(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var attempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == id && a.CompletedAt != null)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            // Calcola statistiche
            var viewModel = new
            {
                Student = student,
                Attempts = attempts,
                AvgScore = attempts.Any() ? attempts.Average(a => a.ScorePercentage) : 0,
                BestScore = attempts.Any() ? attempts.Max(a => a.ScorePercentage) : 0,
                LastAttemptDate = attempts.Any() ? attempts.Max(a => a.CompletedAt) : null
            };

            ViewBag.Student = student;
            ViewBag.AvgScore = viewModel.AvgScore;
            ViewBag.BestScore = viewModel.BestScore;
            ViewBag.LastAttemptDate = viewModel.LastAttemptDate;

            return View(attempts);
        }
    }

    // Classe di supporto per gli errori
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}