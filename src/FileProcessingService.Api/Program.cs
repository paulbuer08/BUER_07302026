using FileProcessingService.Api.Configuration;
using FileProcessingService.Api.Features.Files.Processing;
using FileProcessingService.Api.Features.Files.Services;
using FileProcessingService.Api.Features.Files.Tracking;
using FileProcessingService.Api.Security;
using FileProcessingService.Api.Security.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

const string swaggerDocumentName = "v1";


var builder = WebApplication.CreateBuilder(args);

var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:IsEnabled");
var authenticationSection = builder.Configuration.GetRequiredSection(AuthenticationOptions.SectionName);
var authenticationMode = authenticationSection.GetValue<AuthenticationMode>(nameof(AuthenticationOptions.Mode));
var authenticationHeaderName = authenticationSection[nameof(AuthenticationOptions.HeaderName)] ?? ApiKeyAuthenticationDefaults.HeaderName;

// Register application services
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddOptions<FileProcessingOptions>()
    .BindConfiguration(FileProcessingOptions.SectionName)
    .ValidateOnStart();

builder.Services.AddSingleton<
    IValidateOptions<FileProcessingOptions>,
    FileProcessingOptionsValidator>();

builder.Services.AddSingleton<
    IConfigureOptions<FormOptions>,
    FileUploadFormOptionsSetup>();

builder.Services.AddSingleton<
    IFileProcessor,
    CsvFileProcessor>();

builder.Services.AddSingleton<
    IFileProcessor,
    JsonFileProcessor>();

builder.Services.AddScoped<
    IFileProcessingOrchestrator,
    FileProcessingOrchestrator>();

builder.Services.AddSingleton<
    IFileProcessingTracker,
    InMemoryFileProcessingTracker>();

builder.Services.AddSingleton<
    IValidateOptions<AuthenticationOptions>,
    AuthenticationOptionsValidator>();

builder.Services
    .AddOptions<AuthenticationOptions>()
    .BindConfiguration(AuthenticationOptions.SectionName)
    .ValidateOnStart();

switch (authenticationMode)
{
    case AuthenticationMode.SharedKey:
        builder.Services.AddSingleton<
            IApiKeyCredentialValidator,
            SharedKeyCredentialValidator>();
        break;
    case AuthenticationMode.ManagedClientKeys:
        throw new InvalidOperationException(
            "ManagedClientKeys authentication requires its PostgreSQL credential store.");
    default:
        throw new InvalidOperationException(
            "The configured authentication mode is unsupported.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyAuthenticationDefaults.SchemeName;
        options.DefaultChallengeScheme = ApiKeyAuthenticationDefaults.SchemeName;
        options.DefaultForbidScheme = ApiKeyAuthenticationDefaults.SchemeName;
    })
    .AddScheme<
        ApiKeyAuthenticationSchemeOptions,
        ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationDefaults.SchemeName,
            options => options.HeaderName = authenticationHeaderName);

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(
        ApiKeyAuthorizationDefaults.ProcessFilesPolicy,
        policy =>
        {
            policy.AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.SchemeName);
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(
                ApiKeyAuthorizationDefaults.ScopeClaimType,
                ApiKeyAuthorizationDefaults.ProcessFilesScope);
        })
    .AddPolicy(
        ApiKeyAuthorizationDefaults.ReadReportsPolicy,
        policy =>
        {
            policy.AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.SchemeName);
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(
                ApiKeyAuthorizationDefaults.ScopeClaimType,
                ApiKeyAuthorizationDefaults.ReadReportsScope);
        });


builder.Services
    .AddFileProcessingRateLimiting(builder.Configuration);


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        swaggerDocumentName,
        new OpenApiInfo
        {
            Title = "File Processing Service API",
            Version = "v1",
            Description = "A secure REST API for processing files and reporting processing activity."
        });

    options.AddSecurityDefinition(
        ApiKeyAuthenticationDefaults.SchemeName,
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = authenticationHeaderName,
            Description =
                $"Enter the API key sent through the {authenticationHeaderName} request header."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                ApiKeyAuthenticationDefaults.SchemeName,
                document)] = []
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "File Processing Service API v1");

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "File Processing Service API";
        options.DisplayRequestDuration();
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers()
    .RequireRateLimiting(RateLimitingOptions.PolicyName);


// Start the app
await app.RunAsync();
