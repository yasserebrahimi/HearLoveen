using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Npgsql;
using Dapper;
using Serilog;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("DSR Worker starting...");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var rabbitHost = configuration["RabbitMQ:Host"] ?? "rabbitmq";
var rabbitPort = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";

var connStr = configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Username=postgres;Password=postgres;Database=hearloveen";

var factory = new ConnectionFactory
{
    HostName = rabbitHost,
    Port = rabbitPort,
    UserName = rabbitUser,
    Password = rabbitPass
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declare queues
channel.QueueDeclare(queue: "dsr_export_queue", durable: true, exclusive: false, autoDelete: false);
channel.QueueDeclare(queue: "dsr_delete_queue", durable: true, exclusive: false, autoDelete: false);

// Set up consumers
var exportConsumer = new EventingBasicConsumer(channel);
exportConsumer.Received += async (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var payload = JsonSerializer.Deserialize<DsrRequest>(message);

        if (payload == null)
        {
            Log.Warning("Received invalid export request");
            channel.BasicNack(ea.DeliveryTag, false, false);
            return;
        }

        Log.Information("Processing export request {RequestId} for user {UserId}", payload.requestId, payload.userId);

        // Update status to processing
        await using var con = new NpgsqlConnection(connStr);
        await con.ExecuteAsync(
            "UPDATE dsr_requests SET status = 'processing', updated_at = CURRENT_TIMESTAMP WHERE id = @id",
            new { id = payload.requestId }
        );

        // Perform export operation
        // 1. Gather all user data
        var userData = await GatherUserData(con, payload.userId);

        // 2. Create export file (JSON format for GDPR compliance)
        var exportData = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });

        // 3. Store export file (in production, upload to secure storage like Azure Blob)
        var exportPath = $"/exports/{payload.userId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        // TODO: Upload to Azure Blob Storage

        // Update status to completed
        await con.ExecuteAsync(
            "UPDATE dsr_requests SET status = 'completed', completed_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id",
            new { id = payload.requestId }
        );

        Log.Information("Export request {RequestId} completed successfully", payload.requestId);
        channel.BasicAck(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error processing export request");
        channel.BasicNack(ea.DeliveryTag, false, true); // Requeue for retry
    }
};

var deleteConsumer = new EventingBasicConsumer(channel);
deleteConsumer.Received += async (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var payload = JsonSerializer.Deserialize<DsrRequest>(message);

        if (payload == null)
        {
            Log.Warning("Received invalid delete request");
            channel.BasicNack(ea.DeliveryTag, false, false);
            return;
        }

        Log.Information("Processing delete request {RequestId} for user {UserId}", payload.requestId, payload.userId);

        // Update status to processing
        await using var con = new NpgsqlConnection(connStr);
        await con.ExecuteAsync(
            "UPDATE dsr_requests SET status = 'processing', updated_at = CURRENT_TIMESTAMP WHERE id = @id",
            new { id = payload.requestId }
        );

        // Perform anonymization/deletion (GDPR compliant)
        // 1. Anonymize user recordings
        await con.ExecuteAsync(
            "UPDATE audio_recordings SET user_id = 'ANONYMIZED', anonymized_at = CURRENT_TIMESTAMP WHERE user_id = @userId",
            new { userId = payload.userId }
        );

        // 2. Anonymize analysis results
        await con.ExecuteAsync(
            "UPDATE analysis_results SET user_id = 'ANONYMIZED', anonymized_at = CURRENT_TIMESTAMP WHERE user_id = @userId",
            new { userId = payload.userId }
        );

        // 3. Delete personal information
        await con.ExecuteAsync(
            "DELETE FROM user_profiles WHERE user_id = @userId",
            new { userId = payload.userId }
        );

        // 4. Keep audit trail (required for MDR compliance)
        await con.ExecuteAsync(
            @"INSERT INTO audit_log (event_type, user_id, description, created_at)
              VALUES ('user_data_deleted', @userId, 'User data deleted per GDPR request', CURRENT_TIMESTAMP)",
            new { userId = payload.userId }
        );

        // Update status to completed
        await con.ExecuteAsync(
            "UPDATE dsr_requests SET status = 'completed', completed_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id",
            new { id = payload.requestId }
        );

        Log.Information("Delete request {RequestId} completed successfully", payload.requestId);
        channel.BasicAck(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error processing delete request");
        channel.BasicNack(ea.DeliveryTag, false, true); // Requeue for retry
    }
};

channel.BasicConsume(queue: "dsr_export_queue", autoAck: false, consumer: exportConsumer);
channel.BasicConsume(queue: "dsr_delete_queue", autoAck: false, consumer: deleteConsumer);

Log.Information("DSR Worker is running. Press Ctrl+C to exit.");

// Keep the worker running
var exitEvent = new ManualResetEvent(false);
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    exitEvent.Set();
};
exitEvent.WaitOne();

Log.Information("DSR Worker shutting down...");

async Task<Dictionary<string, object>> GatherUserData(NpgsqlConnection con, string userId)
{
    var data = new Dictionary<string, object>
    {
        ["userId"] = userId,
        ["exportDate"] = DateTime.UtcNow,
        ["profile"] = await con.QueryFirstOrDefaultAsync("SELECT * FROM user_profiles WHERE user_id = @userId", new { userId }) ?? new { },
        ["recordings"] = await con.QueryAsync("SELECT id, created_at, duration FROM audio_recordings WHERE user_id = @userId", new { userId }),
        ["analyses"] = await con.QueryAsync("SELECT id, created_at, score FROM analysis_results WHERE user_id = @userId", new { userId }),
        ["sessions"] = await con.QueryAsync("SELECT id, created_at, completed_at FROM therapy_sessions WHERE user_id = @userId", new { userId })
    };

    return data;
}

record DsrRequest(int requestId, string userId, string action);
