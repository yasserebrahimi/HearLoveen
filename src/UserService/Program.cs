using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using HearLoveen.Infrastructure.Persistence;

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
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "database");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("UserService"))
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
        Title = "HearLoveen User Service", 
        Version = "v1" 
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

// Health check
app.MapHealthChecks("/health");

// Get all users (Admin only)
app.MapGet("/api/v1/users", async (AppDbContext db) =>
{
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);
})
.RequireAuthorization("Admin")
.WithName("GetAllUsers")
.WithTags("Users");

// Get user by ID
app.MapGet("/api/v1/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    return user is null ? Results.NotFound() : Results.Ok(user);
})
.RequireAuthorization()
.WithName("GetUserById")
.WithTags("Users");

// Get current user profile
app.MapGet("/api/v1/users/me", async (HttpContext context, AppDbContext db) =>
{
    var emailClaim = context.User.Claims.FirstOrDefault(c => c.Type == "email");
    if (emailClaim is null)
        return Results.Unauthorized();

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
    return user is null ? Results.NotFound() : Results.Ok(user);
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithTags("Users");

// Create user
app.MapPost("/api/v1/users", async (CreateUserRequest request, AppDbContext db) =>
{
    var user = new HearLoveen.Domain.Entities.User
    {
        Id = Guid.NewGuid(),
        Email = request.Email,
        Name = request.Name,
        Role = request.Role
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/users/{user.Id}", user);
})
.RequireAuthorization("Admin")
.WithName("CreateUser")
.WithTags("Users");

// Update user
app.MapPut("/api/v1/users/{id:guid}", async (Guid id, UpdateUserRequest request, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null)
        return Results.NotFound();

    user.Name = request.Name ?? user.Name;
    user.Email = request.Email ?? user.Email;
    user.Role = request.Role ?? user.Role;

    await db.SaveChangesAsync();
    return Results.Ok(user);
})
.RequireAuthorization("Admin")
.WithName("UpdateUser")
.WithTags("Users");

// Delete user
app.MapDelete("/api/v1/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null)
        return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization("Admin")
.WithName("DeleteUser")
.WithTags("Users");

app.Run();

// DTOs
public record CreateUserRequest(string Email, string Name, string Role);
public record UpdateUserRequest(string? Email, string? Name, string? Role);
