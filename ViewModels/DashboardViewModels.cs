// ViewModels/DashboardViewModels.cs
using Geography4Geek_1.Models;
using System.Collections.Generic;

namespace Geography4Geek_1.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalQuizzes { get; set; }
        public int TotalAttempts { get; set; }
        public List<QuizAttempt> RecentAttempts { get; set; }
    }

    public class StudentQuizHistoryViewModel
    {
        public ApplicationUser Student { get; set; }
        public List<QuizAttempt> Attempts { get; set; }
    }
}