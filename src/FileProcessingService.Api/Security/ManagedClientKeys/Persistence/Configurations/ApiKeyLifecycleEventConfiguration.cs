using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence.Configurations;


public sealed class ApiKeyLifecycleEventConfiguration
    : IEntityTypeConfiguration<ApiKeyLifecycleEvent>
{
    public void Configure(
        EntityTypeBuilder<ApiKeyLifecycleEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("api_key_lifecycle_events", table =>
        {
            table.HasCheckConstraint(
                "ck_api_key_lifecycle_events_event_type",
                "event_type IN ('Issued', 'Activated', 'RotationStarted', 'Revoked')");

            table.HasCheckConstraint(
                "ck_api_key_lifecycle_events_actor_not_blank",
                "char_length(btrim(actor_identifier)) > 0");

            table.HasCheckConstraint(
                "ck_api_key_lifecycle_events_reason_not_blank",
                "reason IS NULL OR char_length(btrim(reason)) > 0");
        });

        builder.HasKey(lifecycleEvent => lifecycleEvent.Id);

        builder.Property(lifecycleEvent => lifecycleEvent.Id)
            .HasColumnName("id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .ValueGeneratedNever();

        builder.Property(lifecycleEvent => lifecycleEvent.ApiKeyCredentialId)
            .HasColumnName("api_key_credential_id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .IsRequired();

        builder.Property(lifecycleEvent => lifecycleEvent.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(lifecycleEvent => lifecycleEvent.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType(PostgreSqlColumnTypes.TimestampWithTimeZone)
            .IsRequired();

        builder.Property(lifecycleEvent => lifecycleEvent.ActorIdentifier)
            .HasColumnName("actor_identifier")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lifecycleEvent => lifecycleEvent.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500);

        builder.Property(lifecycleEvent => lifecycleEvent.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType(PostgreSqlColumnTypes.Json);

        builder.HasIndex(
                lifecycleEvent => new
                {
                    lifecycleEvent.ApiKeyCredentialId,
                    lifecycleEvent.OccurredAtUtc
                })
            .HasDatabaseName("ix_api_key_lifecycle_events_credential_time");

        builder.HasOne(lifecycleEvent => lifecycleEvent.ApiKeyCredential)
            .WithMany(credential => credential.LifecycleEvents)
            .HasForeignKey(lifecycleEvent => lifecycleEvent.ApiKeyCredentialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
