using FoundationKit.Workbench.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class BuildBriefConfiguration : IEntityTypeConfiguration<BuildBrief>
{
    public void Configure(EntityTypeBuilder<BuildBrief> builder)
    {
        builder.ToTable("BuildBriefs");
        builder.HasKey(brief => brief.Id);
        builder.Property(brief => brief.ProjectName).HasMaxLength(160).IsRequired();
        builder.Property(brief => brief.ProjectType).HasMaxLength(80).IsRequired();
        builder.Property(brief => brief.Audience).HasMaxLength(300).IsRequired();
        builder.Property(brief => brief.Goal).HasMaxLength(1000).IsRequired();
        builder.Property(brief => brief.SelectedCapabilityIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(brief => brief.Priorities).HasMaxLength(800).IsRequired();
        builder.Property(brief => brief.Notes).HasMaxLength(2000).IsRequired();
        builder.Property(brief => brief.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(brief => brief.CreatedUtc).IsRequired();
        builder.Property(brief => brief.UpdatedUtc).IsRequired();
        builder.Ignore(brief => brief.SelectedCapabilityIds);
        builder.Ignore(brief => brief.DomainEvents);
        builder.HasIndex(brief => brief.CreatedUtc);
        builder.HasIndex(brief => brief.Status);
    }
}
