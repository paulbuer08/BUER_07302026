using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence.Configurations;


public sealed class OrganizationConfiguration
    : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("organizations", table =>
        {
            table.HasCheckConstraint(
                "ck_organizations_code_not_blank",
                "char_length(btrim(code)) > 0");

            table.HasCheckConstraint(
                "ck_organizations_normalized_code_not_blank",
                "char_length(btrim(normalized_code)) > 0");

            table.HasCheckConstraint(
                "ck_organizations_display_name_not_blank",
                "char_length(btrim(display_name)) > 0");

            table.HasCheckConstraint(
                "ck_organizations_status",
                "status IN ('PendingApproval', 'Active', 'Suspended', 'Closed')");
        });

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Id)
            .HasColumnName("id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .ValueGeneratedNever();

        builder.Property(organization => organization.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(organization => organization.NormalizedCode)
            .HasColumnName("normalized_code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(organization => organization.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(organization => organization.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(organization => organization.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone)
            .IsRequired();

        builder.Property(organization => organization.ApprovedAtUtc)
            .HasColumnName("approved_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.Property(organization => organization.SuspendedAtUtc)
            .HasColumnName("suspended_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.Property(organization => organization.ClosedAtUtc)
            .HasColumnName("closed_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.HasIndex(organization => organization.NormalizedCode)
            .IsUnique()
            .HasDatabaseName("ux_organizations_normalized_code");

        builder.HasIndex(organization => organization.Status)
            .HasDatabaseName("ix_organizations_status");
    }
}
