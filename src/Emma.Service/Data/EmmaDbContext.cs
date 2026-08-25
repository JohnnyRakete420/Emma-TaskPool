using Emma.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Service.Data;

public class EmmaDbContext(DbContextOptions<EmmaDbContext> options) : DbContext(options)
{
    public DbSet<Prozess> Prozesse => Set<Prozess>();
    public DbSet<Aufgabe> Aufgaben => Set<Aufgabe>();
    public DbSet<WiederkehrenderPlan> WiederkehrendePlaene => Set<WiederkehrenderPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aufgabe>()
            .HasOne(a => a.Prozess)
            .WithMany()
            .HasForeignKey(a => a.ProzessId);

        modelBuilder.Entity<WiederkehrenderPlan>()
            .HasOne(p => p.Prozess)
            .WithMany()
            .HasForeignKey(p => p.ProzessId);
    }
}
