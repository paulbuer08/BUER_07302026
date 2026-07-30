using FileProcessingService.Api.Configuration;
using FileProcessingService.Api.Features.Files.Processing;
using FileProcessingService.Api.Features.Files.Tracking;
using FileProcessingService.Api.Security;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
const string swaggerDocumentName = "v1";
const string apiKeySchemeName = "ApiKey";


var builder = WebApplication.CreateBuilder(args);
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:IsEnabled");

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
    IFileProcessor,
    CsvFileProcessor>();

builder.Services.AddSingleton<
    IFileProcessor,
    JsonFileProcessor>();

builder.Services.AddSingleton<
    IFileProcessingTracker,
    InMemoryFileProcessingTracker>();

builder.Services.AddSingleton<
    IValidateOptions<ApiKeyAuthenticationOptions>,
    ApiKeyAuthenticationOptionsValidator>();

builder.Services
    .AddOptions<ApiKeyAuthenticationOptions>()
    .BindConfiguration(ApiKeyAuthenticationOptions.SectionName)
    .ValidateOnStart();


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
        apiKeySchemeName,
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationDefaults.HeaderName,
            Description =
                $"Enter the API key sent through the " +
                $"{ApiKeyAuthenticationDefaults.HeaderName} request header."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                apiKeySchemeName,
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

app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.MapControllers();


// Start the app
await app.RunAsync();
