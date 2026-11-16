using Microsoft.EntityFrameworkCore;

namespace Server.Data;
public class PasswordManagerDbContext : DbContext
{
    public PasswordManagerDbContext(
        DbContextOptions<PasswordManagerDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<SessionCredential> Sessions { get; set; }
    public DbSet<VaultEntry> VaultEntries { get; set; }
    public DbSet<TrustedDeviceCredential> TrustedDevices { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionCredential>()
        .HasKey(s => s.CredentialHash);

        modelBuilder.Entity<User>()
            .HasIndex(s => s.Username)
            .IsUnique();
        
        modelBuilder.Entity<VaultEntry>()
            .HasKey(v => v.EntryId);

        modelBuilder.Entity<VaultEntry>()
            .HasOne(v => v.User)
            .WithMany(u => u.VaultEntries)
            .HasForeignKey(v => v.UserId);

        modelBuilder.Entity<TrustedDeviceCredential>()
            .HasKey(device => device.DeviceID);

        modelBuilder.Entity<TrustedDeviceCredential>()
            .HasIndex(device => device.TokenHash)
            .IsUnique();

        modelBuilder.Entity<TrustedDeviceCredential>()
            .HasOne<User>()
            .WithMany(user => user.TrustedDevices)
            .HasForeignKey(device => device.UserID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}