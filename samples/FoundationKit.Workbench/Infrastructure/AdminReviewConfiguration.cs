using FoundationKit.Workbench.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class AdminReviewConfiguration : IEntityTypeConfiguration<AdminReview>
{
    public void Configure(EntityTypeBuilder<AdminReview> builder)
    {
        builder.ToTable("AdminReviews");
        builder.HasKey(review => review.Id);
        builder.Property(review => review.Decision)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(review => review.ReviewedBy).HasMaxLength(120).IsRequired();
        builder.Property(review => review.Notes).HasMaxLength(1200).IsRequired();
        builder.Property(review => review.ReviewedUtc).IsRequired();
        builder.Ignore(review => review.DomainEvents);
        builder.HasIndex(review => review.BuildBriefId);
        builder.HasIndex(review => review.ReviewedUtc);
        builder.HasOne<BuildBrief>()
            .WithMany()
            .HasForeignKey(review => review.BuildBriefId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
