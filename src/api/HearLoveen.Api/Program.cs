using System.Reflection;
using HearLoveen.Application.Audio;
using HearLoveen.Application.Reports;
using HearLoveen.Infrastructure.Messaging;
using HearLoveen.Infrastructure.Persistence;
using HearLoveen.Infrastructure.Storage;
using HearLoveen.Infrastructure;
using HearLoveen.Api.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Authorization;
using HearLoveen.Api.Auth;

var builder = WebApplication.CreateBuilder(args);
using HearLoveen.Api.Features;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;

// Azure AD B2C (JWT Bearer)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options => {
        builder.Configuration.Bind("AzureAdB2C", options);
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.ValidateIssuer = true;
    }, options => {
        builder.Configuration.Bind("AzureAdB2C", options);
    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("Parent", p => p.RequireRole("Parent"));
    options.AddPolicy("Therapist", p => p.RequireRole("Therapist"));
    options.AddPolicy("Admin", p => p.RequireRole("Admin"));
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});


// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Config
var conn = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=hear;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UploadAudio.Command).Assembly));

// Storage & Queue (read from config)
builder.Services.AddSingleton<IStorageService>(_ =>
    new AzureBlobStorageService(
        accountUrl: builder.Configuration["Blob:AccountUrl"] ?? "https://example.blob.core.windows.net",
        container: builder.Configuration["Blob:Container"] ?? "audio"));
builder.Services.AddSingleton<IQueuePublisher>(_ =>
    new ServiceBusPublisher(
        builder.Configuration.GetConnectionString("ServiceBus") ?? "Endpoint=sb://example/;SharedAccessKeyName=listen;SharedAccessKey=key",
        builder.Configuration["ServiceBus:Queue"] ?? "audio-submitted"));

// OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var path = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(path)) c.IncludeXmlComments(path);
});

// Health + OTel
builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "db")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name:"redis");


builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuthorizationHandler, TherapistAssignedHandler>();
builder.Services.AddSingleton<IClaimsTransformation, ScopeClaimsTransformer>();

builder.Services.AddAuthorization(options => {
    options.AddPolicy("TherapistAssigned", policy => policy.RequireRole("Therapist").AddRequirements(new TherapistAssignedRequirement()));
});

builder.Services.AddOpenTelemetry()
    ;
builder.Services.AddSingleton<IFeatureFlags, ConfigFeatureFlags>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

    .ConfigureResource(r => r.AddService("HearLoveen.Api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/api/v1/audio/upload",
    async (UploadAudio.Command cmd, IMediator mediator, CancellationToken ct) =>
{
    var res = await mediator.Send(cmd, ct);
    return Results.Ok(res);
})
.WithName("InitAudioUpload").RequireAuthorization("Parent")
.WithSummary("Init upload and get SAS URL")
.Produces<UploadAudio.Result>(StatusCodes.Status200OK);

app.MapGet("/api/v1/report/{submissionId:guid}",
    async (Guid submissionId, IMediator mediator, CancellationToken ct) =>
{
    var res = await mediator.Send(new GetReport.Query(submissionId), ct);
    return res is null ? Results.NotFound() : Results.Ok(res);
})
.WithName("GetReport").RequireAuthorization("Parent");


app.MapGet("/api/v1/therapist/report/{submissionId:guid}",
    async (Guid submissionId, IMediator mediator, IAuthorizationService auth, HttpContext http, CancellationToken ct) =>
{
    var authResult = await auth.AuthorizeAsync(http.User, submissionId, new TherapistAssignedRequirement());
    if (!authResult.Succeeded) return Results.Forbid();

    var res = await mediator.Send(new GetReport.Query(submissionId), ct);
    return res is null ? Results.NotFound() : Results.Ok(res);
})
.WithName("GetTherapistReport")
.RequireAuthorization("TherapistAssigned");


// Curriculum endpoints
app.MapGet("/api/v1/curriculum/next/{childId:guid}",
    async (Guid childId, IMediator mediator, CancellationToken ct) =>
{
    var res = await mediator.Send(new HearLoveen.Application.Curriculum.GetNextPromptElo.Query(childId), ct);
    return Results.Ok(res);
})
.WithName("GetNextPrompt")
.RequireAuthorization();

app.MapPost("/api/v1/curriculum/feedback",
    async (HearLoveen.Application.Curriculum.ApplyFeedback.Command cmd, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(cmd, ct);
    return Results.Accepted();
})
.WithName("ApplyFeedback")
.RequireAuthorization();

app.MapHealthChecks("/health");
if (app.Environment.IsDevelopment()) { using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<HearLoveen.Infrastructure.Persistence.AppDbContext>(); await HearLoveen.Infrastructure.Seeding.DbSeeder.SeedAsync(db); }
app.UseSerilogRequestLogging();

app.Run();
