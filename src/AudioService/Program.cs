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
using System.Security.Claims;

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

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        options.TokenValidationParameters.NameClaimType = "name";
    },
    options => builder.Configuration.Bind("AzureAdB2C", options));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Parent", policy => policy.RequireRole("Parent"));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "database")
    .AddAzureBlobStorage(blobConnectionString, name: "blob-storage");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("AudioService"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HearLoveen Audio Service", 
        Version = "v1",
        Description = "Service for uploading and managing audio recordings"
    });
});

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

// Upload audio file
app.MapPost("/api/v1/audio/upload", async (
    HttpRequest request,
    AppDbContext db,
    BlobServiceClient blobServiceClient,
    ServiceBusClient serviceBusClient,
    ClaimsPrincipal user) =>
{
    try
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("Request must be multipart/form-data");

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("audio");
        
        if (file == null || file.Length == 0)
            return Results.BadRequest("No audio file provided");

        // Validate file type
        var allowedExtensions = new[] { ".wav", ".mp3", ".m4a", ".ogg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return Results.BadRequest("Invalid file type. Allowed: " + string.Join(", ", allowedExtensions));

        // Get childId from form
        if (!form.TryGetValue("childId", out var childIdStr) || !Guid.TryParse(childIdStr, out var childId))
            return Results.BadRequest("Valid childId required");

        // Get expected text (optional)
        form.TryGetValue("expectedText", out var expectedText);

        // Generate unique blob name
        var blobName = $"{childId}/{Guid.NewGuid()}{extension}";
        var containerClient = blobServiceClient.GetBlobContainerClient("audio-recordings");
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);

        // Upload to blob storage
        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, overwrite: false);
        }

        var blobUrl = blobClient.Uri.ToString();

        // Save to database
        var submission = new AudioSubmission
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            BlobUrl = blobUrl,
            UploadedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        db.AudioSubmissions.Add(submission);
        await db.SaveChangesAsync();

        // Send message to Service Bus for processing
        var sender = serviceBusClient.CreateSender("audio-submitted");
        var message = new ServiceBusMessage(System.Text.Json.JsonSerializer.Serialize(new
        {
            submissionId = submission.Id,
            childId = childId,
            blobUrl = blobUrl,
            expectedText = expectedText?.ToString(),
            uploadedAt = submission.UploadedAt
        }));
        
        await sender.SendMessageAsync(message);

        Log.Information("Audio uploaded successfully: {SubmissionId} for child {ChildId}", 
            submission.Id, childId);

        return Results.Created($"/api/v1/audio/{submission.Id}", new
        {
            submissionId = submission.Id,
            blobUrl = blobUrl,
            uploadedAt = submission.UploadedAt,
            status = "processing"
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error uploading audio");
        return Results.Problem("Error uploading audio: " + ex.Message);
    }
})
.RequireAuthorization("Parent")
.WithName("UploadAudio")
.WithTags("Audio")
.DisableAntiforgery();

// Get audio submission status
app.MapGet("/api/v1/audio/{submissionId:guid}", async (
    Guid submissionId,
    AppDbContext db) =>
{
    var submission = await db.AudioSubmissions
        .Include(s => s.FeedbackReports)
        .FirstOrDefaultAsync(s => s.Id == submissionId);

    if (submission == null)
        return Results.NotFound();

    return Results.Ok(new
    {
        submissionId = submission.Id,
        childId = submission.ChildId,
        uploadedAt = submission.UploadedAt,
        processedAt = submission.ProcessedAt,
        status = submission.ProcessedAt.HasValue ? "completed" : "processing",
        hasFeedback = submission.FeedbackReports.Any()
    });
})
.RequireAuthorization()
.WithName("GetAudioStatus")
.WithTags("Audio");

// List audio submissions for a child
app.MapGet("/api/v1/audio/child/{childId:guid}", async (
    Guid childId,
    AppDbContext db,
    int skip = 0,
    int take = 20) =>
{
    var submissions = await db.AudioSubmissions
        .Where(s => s.ChildId == childId)
        .OrderByDescending(s => s.UploadedAt)
        .Skip(skip)
        .Take(take)
        .Select(s => new
        {
            submissionId = s.Id,
            uploadedAt = s.UploadedAt,
            processedAt = s.ProcessedAt,
            status = s.ProcessedAt.HasValue ? "completed" : "processing"
        })
        .ToListAsync();

    return Results.Ok(submissions);
})
.RequireAuthorization()
.WithName("ListAudioSubmissions")
.WithTags("Audio");

// Delete audio submission
app.MapDelete("/api/v1/audio/{submissionId:guid}", async (
    Guid submissionId,
    AppDbContext db,
    BlobServiceClient blobServiceClient) =>
{
    var submission = await db.AudioSubmissions.FindAsync(submissionId);
    
    if (submission == null)
        return Results.NotFound();

    // Delete from blob storage
    try
    {
        var blobUri = new Uri(submission.BlobUrl);
        var containerClient = blobServiceClient.GetBlobContainerClient("audio-recordings");
        var blobClient = containerClient.GetBlobClient(Path.GetFileName(blobUri.LocalPath));
        await blobClient.DeleteIfExistsAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error deleting blob for submission {SubmissionId}", submissionId);
    }

    // Delete from database
    db.AudioSubmissions.Remove(submission);
    await db.SaveChangesAsync();

    Log.Information("Audio submission deleted: {SubmissionId}", submissionId);

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("DeleteAudioSubmission")
.WithTags("Audio");

app.Run();
