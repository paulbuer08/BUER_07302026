using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence.Configurations;


public sealed class ClientApplicationConfiguration
    : IEntityTypeConfiguration<ClientApplication>
{
    public void Configure(EntityTypeBuilder<ClientApplication> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("client_applications", table =>
        {
            table.HasCheckConstraint(
                "ck_client_applications_identifier_not_blank",
                "char_length(btrim(client_identifier)) > 0");

            table.HasCheckConstraint(
                "ck_client_applications_display_name_not_blank",
                "char_length(btrim(display_name)) > 0");

            table.HasCheckConstraint(
                "ck_client_applications_status",
                "status IN ('PendingActivation', 'Active', 'Suspended', 'Disabled')");
        });

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .HasColumnName("id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .ValueGeneratedNever();

        builder.Property(client => client.OrganizationId)
            .HasColumnName("organization_id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .IsRequired();

        builder.Property(client => client.ClientIdentifier)
            .HasColumnName("client_identifier")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(client => client.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(client => client.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(client => client.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone)
            .IsRequired();

        builder.Property(client => client.ActivatedAtUtc)
            .HasColumnName("activated_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.Property(client => client.SuspendedAtUtc)
            .HasColumnName("suspended_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.Property(client => client.DisabledAtUtc)
            .HasColumnName("disabled_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.HasIndex(client => client.ClientIdentifier)
            .IsUnique()
            .HasDatabaseName(
                "ux_client_applications_client_identifier");

        builder.HasIndex(
                client => new
                {
                    client.OrganizationId,
                    client.Status
                })
            .HasDatabaseName(
                "ix_client_applications_organization_status");

        builder.HasOne(client => client.Organization)
            .WithMany(organization => organization.ClientApplications)
            .HasForeignKey(client => client.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
