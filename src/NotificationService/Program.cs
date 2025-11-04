using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Identity.Web;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        options.TokenValidationParameters.NameClaimType = "name";
    },
    options => builder.Configuration.Bind("AzureAdB2C", options));

builder.Services.AddAuthorization();

// SignalR with Redis backplane for scaling
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = "hearloveen";
    });

// Health checks
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection, name: "redis");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("NotificationService"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapHealthChecks("/health");

// SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

// REST API for sending notifications
app.MapPost("/api/v1/notifications/send", async (
    SendNotificationRequest request,
    IHubContext<NotificationHub> hubContext) =>
{
    await hubContext.Clients.User(request.UserId).SendAsync(
        "ReceiveNotification",
        new
        {
            message = request.Message,
            type = request.Type,
            timestamp = DateTime.UtcNow
        });

    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("SendNotification")
.WithTags("Notifications");

// Broadcast to all users
app.MapPost("/api/v1/notifications/broadcast", async (
    BroadcastRequest request,
    IHubContext<NotificationHub> hubContext) =>
{
    await hubContext.Clients.All.SendAsync(
        "ReceiveNotification",
        new
        {
            message = request.Message,
            type = request.Type,
            timestamp = DateTime.UtcNow
        });

    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("BroadcastNotification")
.WithTags("Notifications");

app.Run();

// SignalR Hub
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name ?? "anonymous";
        _logger.LogInformation("User {UserId} connected to SignalR hub. ConnectionId: {ConnectionId}", 
            userId, Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.Identity?.Name ?? "anonymous";
        _logger.LogInformation("User {UserId} disconnected from SignalR hub. ConnectionId: {ConnectionId}", 
            userId, Context.ConnectionId);
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendToUser(string userId, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            message,
            type = "direct",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined group {GroupName}", 
            Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left group {GroupName}", 
            Context.ConnectionId, groupName);
    }
}

// DTOs
public record SendNotificationRequest(string UserId, string Message, string Type);
public record BroadcastRequest(string Message, string Type);
