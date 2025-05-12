using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Geography4Geek_1.Pages.Teacher
{
    [Authorize(Roles = "Admin,Teacher")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ApplicationDbContext context, ILogger<DashboardModel> logger)
        {
            _context = context;
            _logger = logger;

            // Inizializza le liste per evitare null
            RecentAttempts = new List<QuizAttempt>();
            CountryData = new List<CountryDataPoint>();
            DifficultyData = new List<DifficultyDataPoint>();
        }

        public string Message { get; set; } = "Dashboard docente";
        public string CurrentDate { get; set; } = "2025-05-12 17:21:48";
        public string CurrentUser { get; set; } = "ZanchiSerse";

        // Statistiche
        public int TotalStudents { get; set; }
        public int TotalQuizzes { get; set; }
        public int TotalAttempts { get; set; }
        public double AvgScore { get; set; }

        // Dati per tabelle e grafici
        public List<QuizAttempt> RecentAttempts { get; set; }
        public List<CountryDataPoint> CountryData { get; set; }
        public List<DifficultyDataPoint> DifficultyData { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Info base
                CurrentUser = User.Identity?.Name ?? "ZanchiSerse";
                CurrentDate = "2025-05-12 17:21:48";
                Message = "Dashboard docente caricata con successo!";

                // Statistiche generali
                TotalStudents = await _context.Users.CountAsync();
                TotalQuizzes = await _context.Quizzes.CountAsync();
                TotalAttempts = await _context.QuizAttempts.CountAsync();

                // Punteggio medio (con gestione caso vuoto)
                if (await _context.QuizAttempts.AnyAsync())
                {
                    var avgScores = await _context.QuizAttempts
                        .Where(a => a.CompletedAt != null)
                        .Select(a => a.ScorePercentage)
                        .ToListAsync();

                    AvgScore = avgScores.Any() ? avgScores.Average() : 0;
                }

                // Tentativi recenti
                RecentAttempts = await _context.QuizAttempts
                    .Include(a => a.User)
                    .OrderByDescending(a => a.CompletedAt)
                    .Take(5)
                    .ToListAsync();

                // Dati per grafici (semplificati)
                await LoadChartData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore caricamento dashboard");
                Message = $"Errore: {ex.Message}";
            }
        }

        private async Task LoadChartData()
        {
            // Dati per paesi
            var countryGroups = await _context.QuizAttempts
                .GroupBy(a => a.CountryName)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            CountryData = countryGroups.Select(g => new CountryDataPoint
            {
                Country = g.Country,
                Count = g.Count
            }).ToList();

            // Dati per difficoltà
            var diffGroups = await _context.QuizAttempts
                .GroupBy(a => a.Difficulty)
                .Select(g => new { Difficulty = g.Key, Count = g.Count() })
                .ToListAsync();

            DifficultyData = diffGroups.Select(g => new DifficultyDataPoint
            {
                Difficulty = g.Difficulty,
                Count = g.Count
            }).ToList();
        }

        public class CountryDataPoint
        {
            public string Country { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class DifficultyDataPoint
        {
            public string Difficulty { get; set; } = string.Empty;
            public int Count { get; set; }
        }
    }
}