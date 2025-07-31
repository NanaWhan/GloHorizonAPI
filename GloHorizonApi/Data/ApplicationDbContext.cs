using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
    }

    // Domain Models
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<BookingRequest> BookingRequests { get; set; }
    public DbSet<BookingStatusHistory> BookingStatusHistories { get; set; }
    public DbSet<BookingDocument> BookingDocuments { get; set; }
    public DbSet<TravelPackage> TravelPackages { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<OtpVerification> OtpVerifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<BookingRequest>()
            .HasOne(b => b.User)
            .WithMany(u => u.BookingRequests)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookingStatusHistory>()
            .HasOne(h => h.BookingRequest)
            .WithMany(b => b.StatusHistory)
            .HasForeignKey(h => h.BookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookingDocument>()
            .HasOne(d => d.BookingRequest)
            .WithMany(b => b.Documents)
            .HasForeignKey(d => d.BookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();

        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.Email)
            .IsUnique();

        modelBuilder.Entity<BookingRequest>()
            .HasIndex(b => b.ReferenceNumber)
            .IsUnique();

        modelBuilder.Entity<BookingRequest>()
            .HasIndex(b => new { b.UserId, b.CreatedAt });

        modelBuilder.Entity<BookingRequest>()
            .HasIndex(b => b.ServiceType);

        modelBuilder.Entity<BookingRequest>()
            .HasIndex(b => b.Status);

        modelBuilder.Entity<BookingDocument>()
            .HasIndex(d => new { d.BookingRequestId, d.DocumentType });

        modelBuilder.Entity<TravelPackage>()
            .HasIndex(p => new { p.IsActive, p.IsFeatured, p.DisplayOrder });

        modelBuilder.Entity<OtpVerification>()
            .HasIndex(o => new { o.PhoneNumber, o.CreatedAt });

        modelBuilder.Entity<OtpVerification>()
            .HasIndex(o => o.ExpiresAt);

        // Configure decimal precision
        modelBuilder.Entity<BookingRequest>()
            .Property(b => b.QuotedAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<BookingRequest>()
            .Property(b => b.FinalAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<TravelPackage>()
            .Property(p => p.BasePrice)
            .HasPrecision(10, 2);
    }
} 