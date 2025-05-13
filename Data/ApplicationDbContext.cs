using Geography4Geek_1.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Geography4Geek_1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Data/ApplicationDbContext.cs - aggiungi o aggiorna queste entità
        public DbSet<QuizModel> Quizzes { get; set; } = null!;
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;
        public DbSet<QuizAnswer> QuizAnswers { get; set; } = null!;
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
        public DbSet<QuizResult> QuizResults { get; set; } = null!; // Aggiungiamo QuizResults

        // Nel metodo OnModelCreating
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurazione speciale per SQLite - risolve il problema con nvarchar(max)
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // Configura tutte le proprietà di tipo stringa come TEXT
                foreach (var entityType in builder.Model.GetEntityTypes())
                {
                    foreach (var property in entityType.GetProperties())
                    {
                        // Se la proprietà è di tipo string, configurala come TEXT
                        if (property.ClrType == typeof(string))
                        {
                            property.SetColumnType("TEXT");
                        }
                    }
                }
            }

            // Configura le relazioni
            builder.Entity<QuizAttempt>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);  // Permette utenti anonimi

            // Assicura che QuizModel abbia una proprietà CountryCode
            builder.Entity<QuizModel>()
                .Property(q => q.CountryCode)
                .HasColumnType("TEXT")
                .IsRequired(false);  // Reso opzionale per compatibilità con dati esistenti

            // Configura la relazione per QuizResult
            builder.Entity<QuizResult>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizResult>()
                .HasOne(r => r.Quiz)
                .WithMany()
                .HasForeignKey(r => r.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}