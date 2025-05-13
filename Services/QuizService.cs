using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.EntityFrameworkCore;

namespace Geography4Geek_1.Services
{
    public class QuizResultService
    {
        private readonly ApplicationDbContext _context;

        public QuizResultService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Ottiene tutti i risultati dei quiz con informazioni su utenti
        public async Task<List<QuizResult>> GetAllResultsAsync()
        {
            return await _context.QuizResults
                .Include(r => r.User)
                .Include(r => r.Quiz)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();
        }

        // Ottiene i risultati per uno studente specifico
        public async Task<List<QuizResult>> GetResultsByStudentIdAsync(string studentId)
        {
            return await _context.QuizResults
                .Include(r => r.Quiz)
                .Where(r => r.UserId == studentId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();
        }

        // Ottiene statistiche sui risultati degli studenti
        public async Task<Dictionary<string, double>> GetTopPerformingStudentsAsync(int count = 5)
        {
            return await _context.QuizResults
                .Include(r => r.User)
                .GroupBy(r => r.UserId)
                .Select(g => new {
                    UserName = g.First().User.FirstName + " " + g.First().User.LastName,
                    AverageScore = g.Average(r => r.ScorePercentage)
                })
                .OrderByDescending(x => x.AverageScore)
                .Take(count)
                .ToDictionaryAsync(x => x.UserName, x => Math.Round(x.AverageScore, 1));
        }

        // Ottiene statistiche sui quiz più completati
        public async Task<Dictionary<string, int>> GetMostCompletedQuizzesAsync(int count = 5)
        {
            return await _context.QuizResults
                .GroupBy(r => r.QuizId)
                .Select(g => new {
                    QuizId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToDictionaryAsync(x => $"Quiz #{x.QuizId}", x => x.Count);
        }
    }
}