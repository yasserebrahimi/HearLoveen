using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Devices.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// Get device telemetry
app.MapGet("/api/v1/iot/devices/{deviceId}/telemetry", async (string deviceId) =>
{
    // Placeholder - would integrate with Azure IoT Hub
    return Results.Ok(new
    {
        deviceId,
        battery = 85,
        signalStrength = -45,
        temperature = 22.5,
        timestamp = DateTime.UtcNow
    });
})
.RequireAuthorization()
.WithTags("IoT");

// Send command to device
app.MapPost("/api/v1/iot/devices/{deviceId}/command", async (string deviceId, DeviceCommand command) =>
{
    // Placeholder - would send command via Azure IoT Hub
    Log.Information("Sending command {CommandType} to device {DeviceId}", command.Type, deviceId);
    
    return Results.Ok(new { success = true, deviceId, command = command.Type });
})
.RequireAuthorization()
.WithTags("IoT");

app.Run();

public record DeviceCommand(string Type, Dictionary<string, object>? Parameters);
