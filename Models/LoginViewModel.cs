using System.ComponentModel.DataAnnotations;

namespace Geography4Geek_1_Iniziologin.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Il nome utente è obbligatorio")]
        [Display(Name = "Nome utente")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ricordami")]
        public bool RememberMe { get; set; }
    }
}