using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.ToTable("review_votes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(v => v.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(v => v.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(v => v.IsHelpful)
            .HasColumnName("is_helpful")
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(v => v.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(v => v.Review)
            .WithMany()
            .HasForeignKey(v => v.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - one vote per review per user
        builder.HasIndex(v => new { v.ReviewId, v.UserId }).IsUnique();

        // Indexes
        builder.HasIndex(v => v.ReviewId);
        builder.HasIndex(v => v.UserId);
    }
}
