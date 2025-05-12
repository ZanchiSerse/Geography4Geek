using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Identity;

namespace Geography4Geek_1.Data
{
    public static class IdentityDataInitializer
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Creazione dei ruoli predefiniti se non esistono
            string[] roleNames = {
                UserRole.Teacher.ToString(),
                UserRole.Student.ToString(),
                UserRole.FreeUser.ToString()
            };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}