using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence.Configurations;


public sealed class ApiKeyCredentialConfiguration
    : IEntityTypeConfiguration<ApiKeyCredential>
{
    public void Configure(EntityTypeBuilder<ApiKeyCredential> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("api_key_credentials", table =>
        {
            table.HasCheckConstraint(
                "ck_api_key_credentials_key_id_not_blank",
                "char_length(btrim(key_id)) > 0");

            table.HasCheckConstraint(
                "ck_api_key_credentials_hash_version",
                "hash_algorithm_version >= 1");

            table.HasCheckConstraint(
                "ck_api_key_credentials_version_one_hash_length",
                "hash_algorithm_version <> 1 OR octet_length(secret_hash) = 32");

            table.HasCheckConstraint(
                "ck_api_key_credentials_not_before",
                "not_before_utc >= issued_at_utc");

            table.HasCheckConstraint(
                "ck_api_key_credentials_expiration",
                "expires_at_utc IS NULL OR expires_at_utc > not_before_utc");

            table.HasCheckConstraint(
                "ck_api_key_credentials_revocation",
                "(revoked_at_utc IS NULL AND revocation_reason IS NULL) OR " +
                "(revoked_at_utc IS NOT NULL AND " +
                "revocation_reason IS NOT NULL AND " +
                "char_length(btrim(revocation_reason)) > 0)");

            table.HasCheckConstraint(
                "ck_api_key_credentials_replacement",
                "replaces_credential_id IS NULL OR replaces_credential_id <> id");
        });

        builder.HasKey(credential => credential.Id);

        builder.Property(credential => credential.Id)
            .HasColumnName("id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .ValueGeneratedNever();

        builder.Property(credential => credential.ClientApplicationId)
            .HasColumnName("client_application_id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .IsRequired();

        builder.Property(credential => credential.KeyId)
            .HasColumnName("key_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(credential => credential.SecretHash)
            .HasColumnName("secret_hash")
            .HasColumnType(PostgreSqlColumnTypes.BinaryData)
            .IsRequired();

        builder.Property(credential => credential.HashAlgorithmVersion)
            .HasColumnName("hash_algorithm_version")
            .IsRequired();

        builder.Property(credential => credential.IssuedAtUtc)
            .HasColumnName("issued_at_utc")
            .HasColumnType(
                PostgreSqlColumnTypes.TimestampWithTimeZone)
            .IsRequired();

        builder.Property(credential => credential.NotBeforeUtc)
            .HasColumnName("not_before_utc")
            .HasColumnType(
                PostgreSqlColumnTypes.TimestampWithTimeZone)
            .IsRequired();

        builder.Property(credential => credential.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.Property(credential => credential.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone);

        builder.Property(credential => credential.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(500);

        builder.Property(credential => credential.ReplacesCredentialId)
            .HasColumnName("replaces_credential_id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid);

        builder.HasIndex(credential => credential.KeyId)
            .IsUnique()
            .HasDatabaseName("ux_api_key_credentials_key_id");

        builder.HasIndex(
                credential => new
                {
                    credential.ClientApplicationId,
                    credential.RevokedAtUtc
                })
            .HasDatabaseName("ix_api_key_credentials_client_revoked");

        builder.HasIndex(credential => credential.ExpiresAtUtc)
            .HasDatabaseName("ix_api_key_credentials_expires_at");

        builder.HasOne(credential => credential.ClientApplication)
            .WithMany(client => client.ApiKeyCredentials)
            .HasForeignKey(credential => credential.ClientApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(credential => credential.ReplacesCredential)
            .WithOne(credential => credential.ReplacementCredential)
            .HasForeignKey<ApiKeyCredential>(credential => credential.ReplacesCredentialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
