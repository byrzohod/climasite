using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class InstallationRequestConfiguration : IEntityTypeConfiguration<InstallationRequest>
{
    public void Configure(EntityTypeBuilder<InstallationRequest> builder)
    {
        builder.ToTable("installation_requests");

        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ir => ir.UserId)
            .HasColumnName("user_id");

        builder.Property(ir => ir.OrderId)
            .HasColumnName("order_id");

        builder.Property(ir => ir.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(ir => ir.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ir => ir.InstallationType)
            .HasColumnName("installation_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ir => ir.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(InstallationRequestStatus.Pending);

        // Customer information
        builder.Property(ir => ir.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ir => ir.CustomerEmail)
            .HasColumnName("customer_email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ir => ir.CustomerPhone)
            .HasColumnName("customer_phone")
            .HasMaxLength(50)
            .IsRequired();

        // Address
        builder.Property(ir => ir.AddressLine1)
            .HasColumnName("address_line1")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ir => ir.AddressLine2)
            .HasColumnName("address_line2")
            .HasMaxLength(255);

        builder.Property(ir => ir.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ir => ir.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ir => ir.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired();

        // Scheduling
        builder.Property(ir => ir.PreferredDate)
            .HasColumnName("preferred_date");

        builder.Property(ir => ir.PreferredTimeSlot)
            .HasColumnName("preferred_time_slot")
            .HasMaxLength(50);

        builder.Property(ir => ir.ScheduledDate)
            .HasColumnName("scheduled_date");

        builder.Property(ir => ir.CompletedAt)
            .HasColumnName("completed_at");

        // Notes
        builder.Property(ir => ir.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(ir => ir.TechnicianNotes)
            .HasColumnName("technician_notes")
            .HasMaxLength(2000);

        // Pricing
        builder.Property(ir => ir.EstimatedPrice)
            .HasColumnName("estimated_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ir => ir.FinalPrice)
            .HasColumnName("final_price")
            .HasPrecision(18, 2);

        builder.Property(ir => ir.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(ir => ir.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(ir => ir.User)
            .WithMany()
            .HasForeignKey(ir => ir.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ir => ir.Order)
            .WithMany()
            .HasForeignKey(ir => ir.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ir => ir.Product)
            .WithMany()
            .HasForeignKey(ir => ir.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ir => ir.UserId);
        builder.HasIndex(ir => ir.OrderId);
        builder.HasIndex(ir => ir.ProductId);
        builder.HasIndex(ir => ir.Status);
        builder.HasIndex(ir => ir.ScheduledDate);
        builder.HasIndex(ir => ir.CreatedAt).IsDescending();
    }
}
