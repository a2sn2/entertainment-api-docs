using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Athar.Infrastructure.Migrations;

[DbContext(typeof(AtharDbContext))]
public sealed class AtharDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("Athar.Infrastructure.AuditEntry", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<string>("Action").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)");
            entity.Property<DateTimeOffset>("CreatedUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("Details").IsRequired().HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            entity.Property<Guid>("EntityId").HasColumnType("uniqueidentifier");
            entity.Property<string>("EntityType").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)");
            entity.Property<Guid?>("UserId").HasColumnType("uniqueidentifier");
            entity.HasKey("Id");
            entity.HasIndex("EntityType", "EntityId", "CreatedUtc");
            entity.ToTable("AuditEntries", "athar");
        });

        modelBuilder.Entity("Athar.Infrastructure.AtharUser", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<int>("AccessFailedCount").HasColumnType("int");
            entity.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("nvarchar(max)");
            entity.Property<DateTimeOffset>("CreatedUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("DisplayName").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)");
            entity.Property<string>("Email").HasMaxLength(256).HasColumnType("nvarchar(256)");
            entity.Property<bool>("EmailConfirmed").HasColumnType("bit");
            entity.Property<bool>("LockoutEnabled").HasColumnType("bit");
            entity.Property<DateTimeOffset?>("LockoutEnd").HasColumnType("datetimeoffset");
            entity.Property<string>("NormalizedEmail").HasMaxLength(256).HasColumnType("nvarchar(256)");
            entity.Property<string>("NormalizedUserName").HasMaxLength(256).HasColumnType("nvarchar(256)");
            entity.Property<string>("PasswordHash").HasColumnType("nvarchar(max)");
            entity.Property<string>("PhoneNumber").HasColumnType("nvarchar(max)");
            entity.Property<bool>("PhoneNumberConfirmed").HasColumnType("bit");
            entity.Property<string>("SecurityStamp").HasColumnType("nvarchar(max)");
            entity.Property<bool>("TwoFactorEnabled").HasColumnType("bit");
            entity.Property<string>("UserName").HasMaxLength(256).HasColumnType("nvarchar(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedEmail").HasDatabaseName("EmailIndex");
            entity.HasIndex("NormalizedUserName").IsUnique().HasDatabaseName("UserNameIndex").HasFilter("[NormalizedUserName] IS NOT NULL");
            entity.ToTable("Users", "identity");
        });

        modelBuilder.Entity("Athar.Domain.Initiative", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<string>("Category").IsRequired().HasMaxLength(80).HasColumnType("nvarchar(80)");
            entity.Property<string>("City").IsRequired().HasMaxLength(80).HasColumnType("nvarchar(80)");
            entity.Property<Guid>("ClientRequestId").HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("CreatedUtc").HasColumnType("datetimeoffset");
            entity.Property<Guid>("OwnerUserId").HasColumnType("uniqueidentifier");
            entity.Property<decimal>("RequestedBudget").HasPrecision(18, 2).HasColumnType("decimal(18,2)");
            entity.Property<byte[]>("RowVersion").IsConcurrencyToken().IsRequired().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            entity.Property<string>("Status").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            entity.Property<string>("Summary").IsRequired().HasMaxLength(1800).HasColumnType("nvarchar(1800)");
            entity.Property<int>("TargetBeneficiaries").HasColumnType("int");
            entity.Property<string>("Title").IsRequired().HasMaxLength(140).HasColumnType("nvarchar(140)");
            entity.Property<DateTimeOffset>("UpdatedUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("OwnerUserId", "ClientRequestId").IsUnique();
            entity.HasIndex("Status", "CreatedUtc");
            entity.ToTable("Initiatives", "athar");
        });

        modelBuilder.Entity("Athar.Domain.InitiativeReview", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<string>("Decision").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            entity.Property<Guid>("InitiativeId").HasColumnType("uniqueidentifier");
            entity.Property<string>("Notes").IsRequired().HasMaxLength(1200).HasColumnType("nvarchar(1200)");
            entity.Property<DateTimeOffset>("ReviewedUtc").HasColumnType("datetimeoffset");
            entity.Property<Guid>("ReviewerUserId").HasColumnType("uniqueidentifier");
            entity.HasKey("Id");
            entity.HasIndex("InitiativeId", "ReviewedUtc");
            entity.HasIndex("ReviewerUserId");
            entity.ToTable("InitiativeReviews", "athar");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("nvarchar(max)");
            entity.Property<string>("Name").HasMaxLength(256).HasColumnType("nvarchar(256)");
            entity.Property<string>("NormalizedName").HasMaxLength(256).HasColumnType("nvarchar(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedName").IsUnique().HasDatabaseName("RoleNameIndex").HasFilter("[NormalizedName] IS NOT NULL");
            entity.ToTable("Roles", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            entity.Property<string>("ClaimType").HasColumnType("nvarchar(max)");
            entity.Property<string>("ClaimValue").HasColumnType("nvarchar(max)");
            entity.Property<Guid>("RoleId").HasColumnType("uniqueidentifier");
            entity.HasKey("Id");
            entity.HasIndex("RoleId");
            entity.ToTable("RoleClaims", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            entity.Property<string>("ClaimType").HasColumnType("nvarchar(max)");
            entity.Property<string>("ClaimValue").HasColumnType("nvarchar(max)");
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.HasKey("Id");
            entity.HasIndex("UserId");
            entity.ToTable("UserClaims", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", entity =>
        {
            entity.Property<string>("LoginProvider").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<string>("ProviderKey").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<string>("ProviderDisplayName").HasColumnType("nvarchar(max)");
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.HasKey("LoginProvider", "ProviderKey");
            entity.HasIndex("UserId");
            entity.ToTable("UserLogins", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", entity =>
        {
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("RoleId").HasColumnType("uniqueidentifier");
            entity.HasKey("UserId", "RoleId");
            entity.HasIndex("RoleId");
            entity.ToTable("UserRoles", "identity");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", entity =>
        {
            entity.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
            entity.Property<string>("LoginProvider").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<string>("Name").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<string>("Value").HasColumnType("nvarchar(max)");
            entity.HasKey("UserId", "LoginProvider", "Name");
            entity.ToTable("UserTokens", "identity");
        });

        modelBuilder.Entity("Athar.Domain.Initiative", entity =>
        {
            entity.HasOne("Athar.Infrastructure.AtharUser", null)
                .WithMany()
                .HasForeignKey("OwnerUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Athar.Domain.InitiativeReview", entity =>
        {
            entity.HasOne("Athar.Domain.Initiative", null)
                .WithMany()
                .HasForeignKey("InitiativeId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne("Athar.Infrastructure.AtharUser", null)
                .WithMany()
                .HasForeignKey("ReviewerUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", entity =>
        {
            entity.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", entity =>
        {
            entity.HasOne("Athar.Infrastructure.AtharUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", entity =>
        {
            entity.HasOne("Athar.Infrastructure.AtharUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", entity =>
        {
            entity.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne("Athar.Infrastructure.AtharUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", entity =>
        {
            entity.HasOne("Athar.Infrastructure.AtharUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
#pragma warning restore 612, 618
    }
}
