using Microsoft.EntityFrameworkCore;

namespace Server.Data;
public class PasswordManagerDbContext : DbContext
{
    public PasswordManagerDbContext(
        DbContextOptions<PasswordManagerDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<SessionCredential> Sessions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionCredential>()
        .HasKey(s => s.CredentialHash);

        modelBuilder.Entity<User>()
            .HasIndex(s => s.Username)
            .IsUnique();
    }
}