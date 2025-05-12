using System.ComponentModel.DataAnnotations;

namespace Geography4Geek_1.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;  // Inizializzazione

        [Required(ErrorMessage = "Password obbligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;  // Inizializzazione

        [Display(Name = "Ricordami")]
        public bool RememberMe { get; set; }
    }
}