using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.EntityFrameworkCore;

namespace Geography4Geek_1.Services
{
    public class QuizService
    {
        private readonly ApplicationDbContext _context;

        public QuizService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Ottieni tutti i quiz
        public async Task<List<QuizModel>> GetQuizzesAsync()
        {
            return await _context.Quizzes.ToListAsync();
        }

        // Ottieni un quiz specifico per ID
        public async Task<QuizModel?> GetQuizAsync(int id)
        {
            return await _context.Quizzes.FindAsync(id);
        }

        // Salva un risultato del quiz
        public async Task SaveQuizResultAsync(QuizAttempt attempt)
        {
            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();
        }

        // Ottieni i risultati del quiz per un utente
        public async Task<List<QuizAttempt>> GetUserResultsAsync(string userId)
        {
            return await _context.QuizAttempts
                .Include(r => r.Quiz)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();
        }

        // Ottieni i risultati per un quiz specifico
        public async Task<List<QuizAttempt>> GetQuizResultsAsync(int quizId)
        {
            return await _context.QuizAttempts
                .Include(r => r.User)
                .Where(r => r.QuizId == quizId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();
        }

        // Ottieni la classifica di tutti gli utenti
        public async Task<List<QuizAttempt>> GetLeaderboardAsync()
        {
            return await _context.QuizAttempts
                .Include(r => r.User)
                .Include(r => r.Quiz)
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.TotalQuestions)
                .ToListAsync();
        }
    }
}