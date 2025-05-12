using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Geography4Geek_1.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReturnUrl { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "Email non valida")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Il nome è obbligatorio")]
            [Display(Name = "Nome")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Il cognome è obbligatorio")]
            [Display(Name = "Cognome")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Il ruolo è obbligatorio")]
            [Display(Name = "Ruolo")]
            public UserRole Role { get; set; }

            [Required(ErrorMessage = "La password è obbligatoria")]
            [StringLength(100, ErrorMessage = "La {0} deve essere lunga almeno {2} caratteri.", MinimumLength = 4)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Conferma password")]
            [Compare("Password", ErrorMessage = "Le password non corrispondono.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                try
                {
                    // Crea un nuovo utente con i dati inseriti
                    var user = new ApplicationUser
                    {
                        UserName = Input.Email,
                        Email = Input.Email,
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        Role = Input.Role,
                        RegistrationDate = DateTime.UtcNow
                    };

                    // Log per debug
                    _logger.LogInformation("Tentativo registrazione utente: {Email}", Input.Email);

                    // Crea l'utente nel database
                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Utente creato con successo.");

                        // Login automatico dell'utente
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }

                    // Se la creazione dell'utente fallisce, mostra gli errori
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Errore registrazione: {Error}", error.Description);
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante la registrazione");
                    ModelState.AddModelError(string.Empty, $"Errore: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError(string.Empty, $"Dettaglio: {ex.InnerException.Message}");
                    }
                }
            }

            // Se arrivati qui, qualcosa è fallito, ridisplaya il form
            return Page();
        }
    }
}