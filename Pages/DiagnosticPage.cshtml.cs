using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using IOFile = System.IO.File;

namespace Geography4Geek_1.Pages
{
    [Authorize(Roles = "Teacher,Administrator")]
    public class DiagnosticPageModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public string DatabasePath { get; set; } = string.Empty;
        public bool DatabaseExists { get; set; }
        public bool ConnectionSuccess { get; set; }
        public int UserCount { get; set; }
        public List<ApplicationUser> UserList { get; set; } = new List<ApplicationUser>();
        public string ErrorMessage { get; set; } = string.Empty;

        public DiagnosticPageModel(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Ottieni il percorso del database
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (connectionString?.Contains("Data Source=") == true)
                {
                    var dataSource = connectionString.Split('=')[1].Trim();

                    if (dataSource.StartsWith("|DataDirectory|"))
                    {
                        var dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
                        dataSource = dataSource.Replace("|DataDirectory|", dataDirectory ?? "");
                    }

                    DatabasePath = dataSource;
                }
                else
                {
                    // Percorso predefinito sul desktop
                    DatabasePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "Geography4Geek_DB.sqlite");
                }

                // Verifica se il file esiste
                DatabaseExists = IOFile.Exists(DatabasePath);

                // Testa la connessione al database
                await _context.Database.OpenConnectionAsync();
                ConnectionSuccess = true;

                // Conta gli utenti
                UserCount = await _context.Users.CountAsync();

                // Ottieni la lista degli utenti - rimosso riferimento a LastActivityDate
                UserList = await _context.Users
                    .OrderByDescending(u => u.RegistrationDate)
                    .ToListAsync();

                // Chiudi la connessione
                await _context.Database.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                ConnectionSuccess = false;
                ErrorMessage = $"Errore: {ex.Message}\n{ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    ErrorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
            }
        }
    }
}