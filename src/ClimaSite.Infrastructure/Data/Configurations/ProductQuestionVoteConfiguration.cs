using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ProductQuestionVoteConfiguration : IEntityTypeConfiguration<ProductQuestionVote>
{
    public void Configure(EntityTypeBuilder<ProductQuestionVote> builder)
    {
        builder.ToTable("product_question_votes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(v => v.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(v => v.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(v => v.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(v => v.Question)
            .WithMany()
            .HasForeignKey(v => v.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - one helpful vote per question per user
        builder.HasIndex(v => new { v.QuestionId, v.UserId }).IsUnique();

        // Indexes
        builder.HasIndex(v => v.QuestionId);
        builder.HasIndex(v => v.UserId);
    }
}
