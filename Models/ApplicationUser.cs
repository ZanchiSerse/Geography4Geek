using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Geography4Geek_1.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [PersonalData]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [PersonalData]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [PersonalData]
        public UserRole Role { get; set; } = UserRole.FreeUser;

        // Proprietà aggiuntive per la visualizzazione
        public string FullName => $"{FirstName} {LastName}";

        // Aggiunta collezione per Quiz Attempts
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }

    public enum UserRole
    {
        Teacher,    // Docente
        Student,    // Studente
        FreeUser    // Utente libero
    }
}