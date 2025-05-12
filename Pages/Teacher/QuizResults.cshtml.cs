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
    public class QuizResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuizResultsModel> _logger;

        public QuizResultsModel(ApplicationDbContext context, ILogger<QuizResultsModel> logger)
        {
            _context = context;
            _logger = logger;
            QuizAttempts = new List<QuizAttempt>();

            // Inizializza i filtri per evitare errori nullable
            SortBy = "Date";
            SortOrder = "desc";
            StudentFilter = string.Empty;
            CountryFilter = string.Empty;
        }

        public List<QuizAttempt> QuizAttempts { get; set; }

        // Parametri di filtro/ordinamento
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StudentFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CountryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading quiz results at {time}", DateTime.UtcNow);

                // Query base - MODIFICA: Usa QuizAttempts invece di QuizResults
                var query = _context.QuizAttempts
                    .Include(a => a.User)
                    .Include(a => a.Quiz)
                    .AsQueryable();

                // Applicazione filtri
                if (!string.IsNullOrEmpty(StudentFilter))
                {
                    query = query.Where(a => a.User != null &&
                        (a.User.UserName != null && a.User.UserName.Contains(StudentFilter) ||
                         a.User.Email != null && a.User.Email.Contains(StudentFilter)));
                }

                if (!string.IsNullOrEmpty(CountryFilter))
                {
                    query = query.Where(a => a.CountryName.Contains(CountryFilter));
                }

                if (DateFrom.HasValue)
                {
                    query = query.Where(a => a.CompletedAt >= DateFrom.Value);
                }

                if (DateTo.HasValue)
                {
                    query = query.Where(a => a.CompletedAt <= DateTo.Value);
                }

                // Ordinamento
                query = SortBy.ToLower() switch
                {
                    "student" => SortOrder == "asc"
                        ? query.OrderBy(a => a.User != null ? a.User.UserName : string.Empty)
                        : query.OrderByDescending(a => a.User != null ? a.User.UserName : string.Empty),
                    "country" => SortOrder == "asc"
                        ? query.OrderBy(a => a.CountryName)
                        : query.OrderByDescending(a => a.CountryName),
                    "score" => SortOrder == "asc"
                        ? query.OrderBy(a => a.ScorePercentage)
                        : query.OrderByDescending(a => a.ScorePercentage),
                    _ => SortOrder == "asc"
                        ? query.OrderBy(a => a.CompletedAt)
                        : query.OrderByDescending(a => a.CompletedAt)
                };

                // Esecuzione query
                QuizAttempts = await query.ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quiz results");
                return StatusCode(500);
            }
        }
    }
}