using Microsoft.EntityFrameworkCore;

namespace Server.Data;

public class PasswordManagerDbContext : DbContext
{
    public PasswordManagerDbContext(
        DbContextOptions<PasswordManagerDbContext> options)
        : base(options)
    {
    }
}