// Crea questo file in /Middleware/RoleSyncMiddleware.cs
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Geography4Geek_1.Middleware
{
    public class RoleSyncMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleSyncMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    // Verifica se l'utente ha il ruolo corrispondente alla sua proprietà Role
                    var roleName = user.Role.ToString();
                    if (!await userManager.IsInRoleAsync(user, roleName))
                    {
                        // Rimuovi l'utente da tutti i ruoli correnti
                        var currentRoles = await userManager.GetRolesAsync(user);
                        if (currentRoles.Any())
                        {
                            await userManager.RemoveFromRolesAsync(user, currentRoles);
                        }

                        // Aggiungi l'utente al ruolo corretto
                        await userManager.AddToRoleAsync(user, roleName);

                        // Aggiorna il sign-in
                        await signInManager.RefreshSignInAsync(user);
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method per facilitare l'uso
    public static class RoleSyncMiddlewareExtensions
    {
        public static IApplicationBuilder UseRoleSync(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RoleSyncMiddleware>();
        }
    }
}