using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using HearLoveen.Infrastructure.Persistence;
using HearLoveen.Domain.Entities;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Database
var conn = builder.Configuration.GetConnectionString("Postgres") 
    ?? "Host=localhost;Database=hearloveen;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));

// Azure Blob Storage
var blobConnectionString = builder.Configuration.GetConnectionString("AzureStorage") 
    ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

// Azure Service Bus
var serviceBusConnection = builder.Configuration.GetConnectionString("ServiceBus") 
    ?? "Endpoint=sb://localhost";
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnection));

// HTTP Client for ML API
var mlApiUrl = builder.Configuration["MLApi:BaseUrl"] ?? "http://localhost:8000";
builder.Services.AddHttpClient("MLApi", client =>
{
    client.BaseAddress = new Uri(mlApiUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    },
    options => builder.Configuration.Bind("AzureAdB2C", options));

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "database")
    .AddAzureBlobStorage(blobConnectionString, name: "blob-storage")
    .AddUrlGroup(new Uri($"{mlApiUrl}/health"), name: "ml-api");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("AnalysisService"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HearLoveen Analysis Service", 
        Version = "v1",
        Description = "Service for analyzing audio recordings with ML"
    });
});

// Background Service for processing
builder.Services.AddHostedService<AudioProcessorBackgroundService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// Trigger manual analysis
app.MapPost("/api/v1/analysis/trigger/{submissionId:guid}", async (
    Guid submissionId,
    AppDbContext db,
    BlobServiceClient blobServiceClient,
    IHttpClientFactory httpClientFactory) =>
{
    var submission = await db.AudioSubmissions.FindAsync(submissionId);
    
    if (submission == null)
        return Results.NotFound();

    if (submission.ProcessedAt != null)
        return Results.BadRequest("Already processed");

    // Download audio from blob
    var blobUri = new Uri(submission.BlobUrl);
    var containerClient = blobServiceClient.GetBlobContainerClient("audio-recordings");
    var blobClient = containerClient.GetBlobClient(Path.GetFileName(blobUri.LocalPath));

    using var memoryStream = new MemoryStream();
    await blobClient.DownloadToAsync(memoryStream);
    memoryStream.Position = 0;

    // Send to ML API
    var httpClient = httpClientFactory.CreateClient("MLApi");
    using var content = new MultipartFormDataContent();
    content.Add(new StreamContent(memoryStream), "file", "audio.wav");

    var response = await httpClient.PostAsync("/api/transcribe", content);
    
    if (!response.IsSuccessStatusCode)
    {
        Log.Error("ML API error: {StatusCode}", response.StatusCode);
        return Results.Problem("ML API error");
    }

    var transcriptionResult = await response.Content.ReadFromJsonAsync<TranscriptionResponse>();
    
    if (transcriptionResult == null)
        return Results.Problem("Invalid ML API response");

    // Save feedback
    var feedback = new FeedbackReport
    {
        Id = Guid.NewGuid(),
        SubmissionId = submissionId,
        Transcript = transcriptionResult.Text,
        Score = transcriptionResult.Confidence * 100,
        Feedback = GenerateFeedback(transcriptionResult.Confidence)
    };

    db.FeedbackReports.Add(feedback);
    
    submission.ProcessedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();

    Log.Information("Analysis completed for submission {SubmissionId}", submissionId);

    return Results.Ok(new
    {
        submissionId,
        transcript = transcriptionResult.Text,
        confidence = transcriptionResult.Confidence,
        score = feedback.Score,
        feedback = feedback.Feedback,
        processedAt = submission.ProcessedAt
    });
})
.RequireAuthorization()
.WithName("TriggerAnalysis")
.WithTags("Analysis");

// Get analysis results
app.MapGet("/api/v1/analysis/{submissionId:guid}", async (
    Guid submissionId,
    AppDbContext db) =>
{
    var feedback = await db.FeedbackReports
        .FirstOrDefaultAsync(f => f.SubmissionId == submissionId);

    if (feedback == null)
        return Results.NotFound();

    return Results.Ok(new
    {
        submissionId,
        transcript = feedback.Transcript,
        score = feedback.Score,
        feedback = feedback.Feedback,
        createdAt = feedback.CreatedAt
    });
})
.RequireAuthorization()
.WithName("GetAnalysisResults")
.WithTags("Analysis");

app.Run();

// Helper method
static string GenerateFeedback(double confidence)
{
    return confidence switch
    {
        >= 0.9 => "Excellent pronunciation! Keep up the great work!",
        >= 0.8 => "Very good! Your pronunciation is clear and accurate.",
        >= 0.7 => "Good job! Continue practicing to improve further.",
        >= 0.6 => "Fair attempt. Try to pronounce more clearly.",
        _ => "Keep practicing! Focus on clarity and pronunciation."
    };
}

// Background Service for processing audio from Service Bus
public class AudioProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AudioProcessorBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public AudioProcessorBackgroundService(
        IServiceProvider services,
        ILogger<AudioProcessorBackgroundService> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audio Processor Background Service started");

        var serviceBusConnection = _configuration.GetConnectionString("ServiceBus") 
            ?? "Endpoint=sb://localhost";
        
        var client = new ServiceBusClient(serviceBusConnection);
        var processor = client.CreateProcessor("audio-submitted", new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false
        });

        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            await processor.StopProcessingAsync();
            await processor.DisposeAsync();
        }
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            var message = JsonSerializer.Deserialize<AudioSubmittedMessage>(body);

            if (message == null)
            {
                _logger.LogWarning("Invalid message format");
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            _logger.LogInformation("Processing audio submission: {SubmissionId}", message.SubmissionId);

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

            // Download audio
            var blobUri = new Uri(message.BlobUrl);
            var containerClient = blobServiceClient.GetBlobContainerClient("audio-recordings");
            var blobClient = containerClient.GetBlobClient(Path.GetFileName(blobUri.LocalPath));

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;

            // Send to ML API
            var httpClient = httpClientFactory.CreateClient("MLApi");
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(memoryStream), "file", "audio.wav");

            var response = await httpClient.PostAsync("/api/transcribe", content);
            var transcriptionResult = await response.Content.ReadFromJsonAsync<TranscriptionResponse>();

            if (transcriptionResult != null)
            {
                // Save results
                var feedback = new FeedbackReport
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = message.SubmissionId,
                    Transcript = transcriptionResult.Text,
                    Score = transcriptionResult.Confidence * 100,
                    Feedback = GenerateFeedback(transcriptionResult.Confidence)
                };

                db.FeedbackReports.Add(feedback);

                var submission = await db.AudioSubmissions.FindAsync(message.SubmissionId);
                if (submission != null)
                {
                    submission.ProcessedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();

                _logger.LogInformation("Analysis completed for {SubmissionId}", message.SubmissionId);
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await args.DeadLetterMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error in Service Bus processor");
        return Task.CompletedTask;
    }
    
    private static string GenerateFeedback(double confidence)
    {
        return confidence switch
        {
            >= 0.9 => "Excellent pronunciation! Keep up the great work!",
            >= 0.8 => "Very good! Your pronunciation is clear and accurate.",
            >= 0.7 => "Good job! Continue practicing to improve further.",
            >= 0.6 => "Fair attempt. Try to pronounce more clearly.",
            _ => "Keep practicing! Focus on clarity and pronunciation."
        };
    }
}

// DTOs
public record TranscriptionResponse(string Text, double Confidence, string Language, double Duration);
public record AudioSubmittedMessage(Guid SubmissionId, Guid ChildId, string BlobUrl, string? ExpectedText, DateTime UploadedAt);
