using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading.Tasks;
using Npgsql;
using Dapper;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/dsr/delete", async (HttpContext ctx) =>
{
    using var sr = new StreamReader(ctx.Request.Body);
    var body = await sr.ReadToEndAsync();
    var payload = JsonSerializer.Deserialize<Dictionary<string,string>>(body);
    var userId = payload?["userId"] ?? "unknown";
    // Connect to Worker for async anonymization task
    using (var client = new HttpClient())
    {
        var workerResponse = await client.PostAsync("http://worker:5001/anonymize", new StringContent(body));
        if (workerResponse.IsSuccessStatusCode)
        {
            return Results.Accepted($"/dsr/requests/{userId}");
        }
        return Results.BadRequest("Failed to anonymize user data.");
    }
});

app.MapPost("/dsr/delete", async (HttpContext ctx) =>
{
    using var sr = new StreamReader(ctx.Request.Body);
    var body = await sr.ReadToEndAsync();
    var payload = JsonSerializer.Deserialize<Dictionary<string,string>>(body);
    var userId = payload?["userId"] ?? "unknown";
    // Connect to Worker for async anonymization task
    using (var client = new HttpClient())
    {
        var workerResponse = await client.PostAsync("http://worker:5001/anonymize", new StringContent(body));
        if (workerResponse.IsSuccessStatusCode)
        {
            return Results.Accepted($"/dsr/requests/{userId}");
        }
        return Results.BadRequest("Failed to anonymize user data.");
    }
});

string connStr = Environment.GetEnvironmentVariable("PG_CONN")
    ?? "Host=postgres;Username=hlv;Password=hlvpass;Database=hearloveen";

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapPost("/dsr/export", async (HttpContext ctx) =>
{
    using var sr = new StreamReader(ctx.Request.Body);
    var body = await sr.ReadToEndAsync();
    var payload = JsonSerializer.Deserialize<Dictionary<string,string>>(body);
    var userId = payload?["userId"] ?? "unknown";
    await using var con = new NpgsqlConnection(connStr);
    await con.ExecuteAsync("insert into dsr_requests(user_id, action, status) values (@u,'export','queued')", new { u = userId });
    return Results.Accepted($"/dsr/requests/{userId}");
});

app.MapPost("/dsr/delete", async (HttpContext ctx) =>
{
    using var sr = new StreamReader(ctx.Request.Body);
    var body = await sr.ReadToEndAsync();
    var payload = JsonSerializer.Deserialize<Dictionary<string,string>>(body);
    var userId = payload?["userId"] ?? "unknown";
    await using var con = new NpgsqlConnection(connStr);
    await con.ExecuteAsync("insert into dsr_requests(user_id, action, status) values (@u,'delete','queued')", new { u = userId });
    // NOTE: a real worker would process anonymization/deletion asynchronously
    return Results.Accepted($"/dsr/requests/{userId}");
});

app.Run();