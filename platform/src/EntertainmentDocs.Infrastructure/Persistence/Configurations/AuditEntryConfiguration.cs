using EntertainmentDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntertainmentDocs.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(120).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UserId);
    }
}
