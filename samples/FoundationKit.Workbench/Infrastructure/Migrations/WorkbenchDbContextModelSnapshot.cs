using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FoundationKit.Workbench.Infrastructure.Migrations;

[DbContext(typeof(WorkbenchDbContext))]
public partial class WorkbenchDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("FoundationKit.Workbench.Domain.BuildBrief", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");

            entity.Property<string>("Audience")
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnType("nvarchar(300)");

            entity.Property<DateTimeOffset>("CreatedUtc")
                .HasColumnType("datetimeoffset");

            entity.Property<string>("Goal")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("nvarchar(1000)");

            entity.Property<string>("Notes")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("nvarchar(2000)");

            entity.Property<string>("Priorities")
                .IsRequired()
                .HasMaxLength(800)
                .HasColumnType("nvarchar(800)");

            entity.Property<string>("ProjectName")
                .IsRequired()
                .HasMaxLength(160)
                .HasColumnType("nvarchar(160)");

            entity.Property<string>("ProjectType")
                .IsRequired()
                .HasMaxLength(80)
                .HasColumnType("nvarchar(80)");

            entity.Property<string>("SelectedCapabilityIdsJson")
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.HasKey("Id");
            entity.HasIndex("CreatedUtc");
            entity.ToTable("BuildBriefs");
        });
#pragma warning restore 612, 618
    }
}
