using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ProductAnswerVoteConfiguration : IEntityTypeConfiguration<ProductAnswerVote>
{
    public void Configure(EntityTypeBuilder<ProductAnswerVote> builder)
    {
        builder.ToTable("product_answer_votes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(v => v.AnswerId)
            .HasColumnName("answer_id")
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
        builder.HasOne(v => v.Answer)
            .WithMany()
            .HasForeignKey(v => v.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - one vote per answer per user
        builder.HasIndex(v => new { v.AnswerId, v.UserId }).IsUnique();

        // Indexes
        builder.HasIndex(v => v.AnswerId);
        builder.HasIndex(v => v.UserId);
    }
}
