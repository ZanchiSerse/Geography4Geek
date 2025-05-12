using System;
using System.Threading.Tasks;
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Geography4Geek_1.Pages
{
    public class FixRoleModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public string UserEmail { get; set; }
        public string CurrentRole { get; set; }

        public FixRoleModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _signInManager = signInManager;
        }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                UserEmail = user.Email;
                CurrentRole = user.Role.ToString();
            }
            else
            {
                UserEmail = "Non autenticato";
                CurrentRole = "Nessuno";
            }
        }

        public async Task<IActionResult> OnPostAsync(string selectedRole)
        {
            if (string.IsNullOrEmpty(selectedRole))
            {
                ModelState.AddModelError(string.Empty, "Seleziona un ruolo valido");
                return Page();
            }

            // Ottieni l'utente corrente
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Utente non trovato");
            }

            // Assicurati che i ruoli esistano
            string[] roles = { "Teacher", "Student", "Administrator" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Rimuovi l'utente da tutti i ruoli attuali
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Aggiungi l'utente al nuovo ruolo
            await _userManager.AddToRoleAsync(user, selectedRole);

            // Aggiorna anche la proprietà Role nell'oggetto ApplicationUser
            if (Enum.TryParse<UserRole>(selectedRole, out var userRole))
            {
                user.Role = userRole;
                await _userManager.UpdateAsync(user);
            }

            // Aggiorna il database direttamente (backup)
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Aggiorna i claims
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, selectedRole));

            // Effettua un nuovo login per aggiornare i cookies
            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, isPersistent: true);

            ViewData["Message"] = $"Il ruolo è stato aggiornato a {selectedRole}. Sei stato riloggato con i nuovi permessi.";
            UserEmail = user.Email;
            CurrentRole = selectedRole;

            return Page();
        }
    }
}