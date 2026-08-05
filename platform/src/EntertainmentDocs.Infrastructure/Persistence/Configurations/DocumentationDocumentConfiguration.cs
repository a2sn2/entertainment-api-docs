using EntertainmentDocs.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntertainmentDocs.Infrastructure.Persistence.Configurations;

public sealed class DocumentationDocumentConfiguration : IEntityTypeConfiguration<DocumentationDocument>
{
    public void Configure(EntityTypeBuilder<DocumentationDocument> builder)
    {
        builder.ToTable("documentation_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reference).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(240).IsRequired();
        builder.HasIndex(x => x.Reference).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasMany(x => x.Versions).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("documentation_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.DocumentId, x.Version }).IsUnique();
    }
}
