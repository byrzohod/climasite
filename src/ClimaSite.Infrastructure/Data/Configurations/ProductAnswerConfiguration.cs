using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ProductAnswerConfiguration : IEntityTypeConfiguration<ProductAnswer>
{
    public void Configure(EntityTypeBuilder<ProductAnswer> builder)
    {
        builder.ToTable("product_answers");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id");

        builder.Property(a => a.AnswerText)
            .HasColumnName("answer_text")
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(a => a.AnswererName)
            .HasColumnName("answerer_name")
            .HasMaxLength(100);

        builder.Property(a => a.IsOfficial)
            .HasColumnName("is_official")
            .HasDefaultValue(false);

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AnswerStatus.Pending);

        builder.Property(a => a.HelpfulCount)
            .HasColumnName("helpful_count")
            .HasDefaultValue(0);

        builder.Property(a => a.UnhelpfulCount)
            .HasColumnName("unhelpful_count")
            .HasDefaultValue(0);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(a => a.QuestionId);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.IsOfficial);
        builder.HasIndex(a => a.CreatedAt).IsDescending();
    }
}
