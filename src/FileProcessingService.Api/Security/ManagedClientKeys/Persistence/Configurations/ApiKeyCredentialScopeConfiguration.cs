using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence.Configurations;


public sealed class ApiKeyCredentialScopeConfiguration
    : IEntityTypeConfiguration<ApiKeyCredentialScope>
{
    public void Configure(EntityTypeBuilder<ApiKeyCredentialScope> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("api_key_credential_scopes", table =>
        {
            table.HasCheckConstraint(
                "ck_api_key_credential_scopes_scope_not_blank",
                "char_length(btrim(scope)) > 0");
        });

        builder.HasKey(
            credentialScope => new
            {
                credentialScope.ApiKeyCredentialId,
                credentialScope.Scope
            });

        builder.Property(credentialScope => credentialScope.ApiKeyCredentialId)
            .HasColumnName("api_key_credential_id")
            .HasColumnType(PostgreSqlColumnTypes.Uuid)
            .IsRequired();

        builder.Property(credentialScope => credentialScope.Scope)
            .HasColumnName("scope")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(credentialScope => credentialScope.ApiKeyCredential)
            .WithMany(credential => credential.Scopes)
            .HasForeignKey(credentialScope => credentialScope.ApiKeyCredentialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
