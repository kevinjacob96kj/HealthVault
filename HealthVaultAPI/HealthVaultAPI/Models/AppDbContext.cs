using Microsoft.EntityFrameworkCore;

namespace HealthVaultAPI.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
