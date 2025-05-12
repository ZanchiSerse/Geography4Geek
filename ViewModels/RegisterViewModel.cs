using Geography4Geek_1.Models;
using System.ComponentModel.DataAnnotations;

namespace Geography4Geek_1.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Nome obbligatorio")]
        [StringLength(50, ErrorMessage = "Il {0} deve essere tra {2} e {1} caratteri.", MinimumLength = 2)]
        [Display(Name = "Nome")]
        public string FirstName { get; set; } = string.Empty;  // Inizializzazione

        [Required(ErrorMessage = "Cognome obbligatorio")]
        [StringLength(50, ErrorMessage = "Il {0} deve essere tra {2} e {1} caratteri.", MinimumLength = 2)]
        [Display(Name = "Cognome")]
        public string LastName { get; set; } = string.Empty;  // Inizializzazione

        [Required(ErrorMessage = "Email obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;  // Inizializzazione

        [Required(ErrorMessage = "Password obbligatoria")]
        [StringLength(100, ErrorMessage = "La {0} deve essere lunga almeno {2} caratteri.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;  // Inizializzazione

        [DataType(DataType.Password)]
        [Display(Name = "Conferma password")]
        [Compare("Password", ErrorMessage = "Le password non corrispondono.")]
        public string ConfirmPassword { get; set; } = string.Empty;  // Inizializzazione

        [Required(ErrorMessage = "Ruolo obbligatorio")]
        [Display(Name = "Ruolo")]
        public UserRole Role { get; set; }
    }
}