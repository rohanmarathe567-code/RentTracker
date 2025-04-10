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
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships and delete behaviors
        modelBuilder.Entity<RentalPayment>()
            .HasOne<RentalProperty>()
            .WithMany()
            .HasForeignKey(p => p.RentalPropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RentalPayment>()
            .HasOne(rp => rp.PaymentMethod)
            .WithMany()
            .HasForeignKey(rp => rp.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attachment>()
            .Property(a => a.RentalPropertyId)
            .IsRequired(false);

        modelBuilder.Entity<Attachment>()
            .Property(a => a.RentalPaymentId)
            .IsRequired(false);
    }
}