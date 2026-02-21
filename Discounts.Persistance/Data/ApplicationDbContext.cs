using Discounts.Domain.Entities.Business;
using Discounts.Domain.Entities.Configuration;
using Discounts.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Persistance.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.CompanyDescription).HasMaxLength(1000);
        });

        // Category configuration
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IconClass).HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Discount configuration
        builder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ImageUrl).HasMaxLength(2000);
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountedPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Discounts)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Merchant)
                .WithMany()
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ApprovedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
        });

        // Coupon configuration
        builder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();

            entity.HasOne(e => e.Discount)
                .WithMany(d => d.Coupons)
                .HasForeignKey(e => e.DiscountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Coupons)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Order configuration
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Discount)
                .WithMany()
                .HasForeignKey(e => e.DiscountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // SystemSettings configuration
        builder.Entity<SystemSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Value).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }
}
