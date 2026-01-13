using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ProductQuestionConfiguration : IEntityTypeConfiguration<ProductQuestion>
{
    public void Configure(EntityTypeBuilder<ProductQuestion> builder)
    {
        builder.ToTable("product_questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(q => q.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(q => q.UserId)
            .HasColumnName("user_id");

        builder.Property(q => q.QuestionText)
            .HasColumnName("question_text")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(q => q.AskerName)
            .HasColumnName("asker_name")
            .HasMaxLength(100);

        builder.Property(q => q.AskerEmail)
            .HasColumnName("asker_email")
            .HasMaxLength(255);

        builder.Property(q => q.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(QuestionStatus.Pending);

        builder.Property(q => q.HelpfulCount)
            .HasColumnName("helpful_count")
            .HasDefaultValue(0);

        builder.Property(q => q.AnsweredAt)
            .HasColumnName("answered_at");

        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(q => q.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(q => q.Product)
            .WithMany(p => p.Questions)
            .HasForeignKey(q => q.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.User)
            .WithMany()
            .HasForeignKey(q => q.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(q => q.ProductId);
        builder.HasIndex(q => q.UserId);
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.CreatedAt).IsDescending();
    }
}
