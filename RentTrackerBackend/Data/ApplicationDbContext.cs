using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RentalProperty> RentalProperties { get; set; } = null!;
    public DbSet<RentalPayment> RentalPayments { get; set; } = null!;
    public DbSet<Attachment> Attachments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<RentalPayment>()
            .HasOne(p => p.RentalProperty)
            .WithMany(r => r.RentalPayments)
            .HasForeignKey(p => p.RentalPropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.RentalProperty)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.RentalPropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.RentalPayment)
            .WithMany(p => p.Attachments)
            .HasForeignKey(a => a.RentalPaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}