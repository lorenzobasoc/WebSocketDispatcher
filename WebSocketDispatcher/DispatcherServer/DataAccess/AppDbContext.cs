using DispatcherServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatcherServer.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<LogEntity> Logs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<LogEntity>().HasKey(l => l.Id);
    }
}
