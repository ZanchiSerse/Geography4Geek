using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using System.Linq;
using System.Threading.Tasks;
using Geography4Geek_1.ViewModels;
using System.Collections.Generic;

namespace Geography4Geek_1.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Statistiche di base per la dashboard
            var viewModel = new DashboardViewModel
            {
                TotalStudents = await _context.Users.CountAsync(),
                TotalQuizzes = await _context.Quizzes.CountAsync(),
                TotalAttempts = await _context.QuizAttempts.CountAsync(),
                RecentAttempts = await _context.QuizAttempts
                    .Include(a => a.User)
                    .Include(a => a.Quiz)
                    .OrderByDescending(a => a.CompletedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> StudentResults()
        {
            var quizAttempts = await _context.QuizAttempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            return View(quizAttempts);
        }

        public async Task<IActionResult> AttemptDetails(int id)
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

        [HttpGet]
        public async Task<IActionResult> StudentQuizHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("ID studente richiesto");
            }

            var student = await _context.Users.FindAsync(userId);
            if (student == null)
            {
                return NotFound("Studente non trovato");
            }

            var attempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.Answers)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            var viewModel = new StudentQuizHistoryViewModel
            {
                Student = student,
                Attempts = attempts
            };

            return View(viewModel);
        }
    }
}