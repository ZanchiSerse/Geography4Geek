using Geography4Geek_1.Data;
using Geography4Geek_1.Models;
using Geography4Geek_1.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ======= CONFIGURAZIONE PERCORSO DATABASE =======
// Usa il desktop dell'utente per il database - posizione con sicuri permessi di scrittura
string databasePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    "Geography4Geek_DB.sqlite");

// Imposta il percorso di |DataDirectory| alla directory corrente
AppDomain.CurrentDomain.SetData("DataDirectory",
    Path.GetDirectoryName(databasePath));

// Usa una stringa di connessione esplicita con il percorso completo
string connectionString = $"Data Source={databasePath}";

// Output per debug
Console.WriteLine($"Percorso database: {databasePath}");
Console.WriteLine($"Esiste già il file? {File.Exists(databasePath)}");
Console.WriteLine($"Utente corrente: {Environment.UserName}");

// ======= CONFIGURAZIONE SERVIZI =======
// Database SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity con impostazioni semplificate
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredLength = 4; // Semplificato per test
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configurazione cookie semplificata
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

builder.Services.AddMemoryCache(); 
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
// bool created = context.Database.EnsureCreated();

// Aggiungi questo dopo builder.Services.AddRazorPages()
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TeacherOnly", policy => policy.RequireRole(UserRole.Teacher.ToString()));
    // Aggiungi altre policy se necessario
    options.AddPolicy("StudentOnly", policy => policy.RequireRole(UserRole.Student.ToString()));
    options.AddPolicy("FreeUserOnly", policy => policy.RequireRole(UserRole.FreeUser.ToString()));
});
// Configura le Razor Pages per utilizzare l'autorizzazione
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Teacher", "TeacherOnly");
});
var app = builder.Build();

// ======= CONFIGURAZIONE PIPELINE =======
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
// Nel metodo Configure, aggiungi questa linea dopo app.UseAuthentication():
//app.UseMiddleware<Geography4Geek_1.Middleware.UserActivityMiddleware>();

//// Se stai usando .NET 6+ con la configurazione minimale, aggiungi:
//// PRIMA di app.MapRazorPages() o app.UseEndpoints()
//app.UseMiddleware<Geography4Geek_1.Middleware.UserActivityMiddleware>();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();
app.UseMiddleware<Geography4Geek_1.Middleware.RoleSyncMiddleware>();

// Middleware per ridirezionare a login se non autenticato
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && (!context.User.Identity?.IsAuthenticated ?? true))
    {
        context.Response.Redirect("/Account/Login");
        return;
    }
    await next();
});

// ======= INIZIALIZZAZIONE DATABASE =======
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Assicurati che i ruoli esistano
        string[] roleNames = Enum.GetNames(typeof(UserRole));
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                Console.WriteLine($"Ruolo {roleName} creato con successo");
            }
        }

        // Crea un utente admin se non esiste già
        if (!context.Users.Any())
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@example.com",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRole.Teacher,
                EmailConfirmed = true
            };

            // Password semplificata per test
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                // Assegna il ruolo all'utente
                await userManager.AddToRoleAsync(adminUser, UserRole.Teacher.ToString());
                Console.WriteLine("Utente admin creato con successo e ruolo assegnato");
            }
            else
            {
                Console.WriteLine($"Errore creazione admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRORE DATABASE: {ex.Message}");
        Console.WriteLine($"DETTAGLIO: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"INNER: {ex.InnerException.Message}");
        }
    }
}

app.Run();