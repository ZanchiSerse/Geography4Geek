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
    public class AttemptDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttemptDetailsModel> _logger;

        public AttemptDetailsModel(ApplicationDbContext context, ILogger<AttemptDetailsModel> logger)
        {
            _context = context;
            _logger = logger;
            Answers = new List<QuizAnswer>();
        }

        public QuizAttempt? Attempt { get; set; }
        public List<QuizAnswer> Answers { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                _logger.LogInformation("Loading quiz attempt details for ID {id}", id);

                // Carica il tentativo con tutte le informazioni correlate
                Attempt = await _context.QuizAttempts
                    .Include(a => a.User)
                    .Include(a => a.Quiz)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (Attempt == null)
                {
                    _logger.LogWarning("Quiz attempt with ID {id} not found", id);
                    return NotFound("Tentativo di quiz non trovato");
                }

                // Carica le risposte con le relative domande
                Answers = await _context.QuizAnswers
                    .Include(a => a.Question)
                    .Where(a => a.QuizAttemptId == id)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quiz attempt details for ID {id}", id);
                return StatusCode(500);
            }
        }
    }
}