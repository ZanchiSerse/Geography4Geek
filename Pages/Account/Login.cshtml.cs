using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Geography4Geek_1.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReturnUrl { get; set; } = string.Empty;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "Email non valida")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "La password è obbligatoria")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Il ruolo è obbligatorio")]
            public UserRole Role { get; set; }

            [Display(Name = "Ricordami")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                try
                {
                    // Trova l'utente per controllare il ruolo
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null && user.Role != Input.Role)
                    {
                        ModelState.AddModelError(string.Empty, "Il ruolo selezionato non corrisponde al ruolo dell'utente.");
                        return Page();
                    }

                    // Tenta il login
                    var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Utente loggato con successo.");
                        return LocalRedirect(returnUrl);
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Account bloccato.");
                        return RedirectToPage("./Lockout");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Tentativo di accesso non valido.");
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante il login");
                    ModelState.AddModelError(string.Empty, $"Errore: {ex.Message}");
                    return Page();
                }
            }

            // Se arrivati qui, qualcosa è fallito, ridisplaya il form
            return Page();
        }
    }
}