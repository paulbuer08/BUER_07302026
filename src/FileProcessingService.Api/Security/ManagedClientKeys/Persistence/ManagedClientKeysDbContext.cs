using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using FileProcessingService.Api.Security.ManagedClientKeys.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence;


public sealed class ManagedClientKeysDbContext(
    DbContextOptions<ManagedClientKeysDbContext> options)
    : DbContext(options)
{
    public const string SchemaName = "security";
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();
    public DbSet<ApiKeyCredential> ApiKeyCredentials => Set<ApiKeyCredential>();
    public DbSet<ApiKeyCredentialScope> ApiKeyCredentialScopes => Set<ApiKeyCredentialScope>();
    public DbSet<ApiKeyLifecycleEvent> ApiKeyLifecycleEvents => Set<ApiKeyLifecycleEvent>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new ClientApplicationConfiguration());

        modelBuilder.ApplyConfiguration(new ApiKeyCredentialConfiguration());
        modelBuilder.ApplyConfiguration(new ApiKeyCredentialScopeConfiguration());
        modelBuilder.ApplyConfiguration(new ApiKeyLifecycleEventConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
