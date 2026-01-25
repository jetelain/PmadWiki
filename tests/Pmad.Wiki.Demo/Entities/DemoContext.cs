using Microsoft.EntityFrameworkCore;

namespace Pmad.Wiki.Demo.Entities;

public class DemoContext : DbContext
{
    public DemoContext(DbContextOptions<DemoContext> options)
        : base(options)
    {
    }

    public DbSet<DemoUser> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DemoUser>().ToTable("DemoUser");
    }
}
