using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Dapper;
using RabbitMQ.Client;
using Serilog;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// Configure RabbitMQ Connection
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq",
        Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
        VirtualHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/"
    };
    return factory.CreateConnection();
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

string connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Username=postgres;Password=postgres;Database=hearloveen";

// Initialize database tables if not exists
using (var conn = new NpgsqlConnection(connStr))
{
    await conn.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS dsr_requests (
            id SERIAL PRIMARY KEY,
            user_id VARCHAR(255) NOT NULL,
            action VARCHAR(50) NOT NULL,
            status VARCHAR(50) NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            completed_at TIMESTAMP NULL
        );

        CREATE INDEX IF NOT EXISTS idx_dsr_user_id ON dsr_requests(user_id);
        CREATE INDEX IF NOT EXISTS idx_dsr_status ON dsr_requests(status);
    ");
}

// DSR Export Endpoint
app.MapPost("/dsr/export", [Authorize] async (HttpContext ctx, IConnection rabbitConn) =>
{
    try
    {
        using var sr = new StreamReader(ctx.Request.Body);
        var body = await sr.ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

        if (payload == null || !payload.ContainsKey("userId"))
        {
            return Results.BadRequest(new { error = "userId is required" });
        }

        var userId = payload["userId"];

        // Insert request into database
        await using var con = new NpgsqlConnection(connStr);
        var requestId = await con.QuerySingleAsync<int>(
            "INSERT INTO dsr_requests(user_id, action, status) VALUES (@u,'export','queued') RETURNING id",
            new { u = userId }
        );

        // Publish to RabbitMQ queue for async processing
        using var channel = rabbitConn.CreateModel();
        channel.QueueDeclare(queue: "dsr_export_queue", durable: true, exclusive: false, autoDelete: false);

        var message = JsonSerializer.Serialize(new { requestId, userId, action = "export" });
        var bodyBytes = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: "", routingKey: "dsr_export_queue", basicProperties: null, body: bodyBytes);

        Log.Information("DSR Export request {RequestId} queued for user {UserId}", requestId, userId);

        return Results.Accepted($"/dsr/requests/{requestId}", new { requestId, status = "queued" });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error processing DSR export request");
        return Results.Problem("Failed to process export request", statusCode: 500);
    }
})
.WithTags("DSR");

// DSR Delete Endpoint
app.MapPost("/dsr/delete", [Authorize] async (HttpContext ctx, IConnection rabbitConn) =>
{
    try
    {
        using var sr = new StreamReader(ctx.Request.Body);
        var body = await sr.ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

        if (payload == null || !payload.ContainsKey("userId"))
        {
            return Results.BadRequest(new { error = "userId is required" });
        }

        var userId = payload["userId"];

        // Insert request into database
        await using var con = new NpgsqlConnection(connStr);
        var requestId = await con.QuerySingleAsync<int>(
            "INSERT INTO dsr_requests(user_id, action, status) VALUES (@u,'delete','queued') RETURNING id",
            new { u = userId }
        );

        // Publish to RabbitMQ queue for async processing
        using var channel = rabbitConn.CreateModel();
        channel.QueueDeclare(queue: "dsr_delete_queue", durable: true, exclusive: false, autoDelete: false);

        var message = JsonSerializer.Serialize(new { requestId, userId, action = "delete" });
        var bodyBytes = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: "", routingKey: "dsr_delete_queue", basicProperties: null, body: bodyBytes);

        Log.Information("DSR Delete request {RequestId} queued for user {UserId}", requestId, userId);

        return Results.Accepted($"/dsr/requests/{requestId}", new { requestId, status = "queued" });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error processing DSR delete request");
        return Results.Problem("Failed to process delete request", statusCode: 500);
    }
})
.WithTags("DSR");

// Get DSR Request Status
app.MapGet("/dsr/requests/{requestId:int}", [Authorize] async (int requestId) =>
{
    try
    {
        await using var con = new NpgsqlConnection(connStr);
        var request = await con.QueryFirstOrDefaultAsync(
            "SELECT id, user_id, action, status, created_at, updated_at, completed_at FROM dsr_requests WHERE id = @id",
            new { id = requestId }
        );

        if (request == null)
        {
            return Results.NotFound(new { error = "Request not found" });
        }

        return Results.Ok(request);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error retrieving DSR request {RequestId}", requestId);
        return Results.Problem("Failed to retrieve request status", statusCode: 500);
    }
})
.WithTags("DSR");

Log.Information("Privacy.API starting...");
app.Run();
