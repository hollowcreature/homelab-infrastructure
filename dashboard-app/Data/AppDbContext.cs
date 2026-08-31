using Microsoft.EntityFrameworkCore;
using DashboardApi.Models;

namespace DashboardApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service> Services { get; set; }
    public DbSet<StatusCheck> StatusChecks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatusCheck>()
            .HasIndex(s => new { s.ServiceId, s.CheckedAt });
    }
}