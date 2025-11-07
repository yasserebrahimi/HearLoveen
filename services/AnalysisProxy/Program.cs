using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        options.ClientId = builder.Configuration["AzureAd:ClientId"];
        options.TenantId = builder.Configuration["AzureAd:TenantId"];
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}";
    },
    options => { });

// Setup YARP for Gateway Routing
builder.Services.AddReverseProxy()
    .AddTransforms()
    .AddRateLimiter();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        options.ClientId = builder.Configuration["AzureAd:ClientId"];
        options.TenantId = builder.Configuration["AzureAd:TenantId"];
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}";
    },
    options => { });
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        options.ClientId = builder.Configuration["AzureAd:ClientId"];
        options.TenantId = builder.Configuration["AzureAd:TenantId"];
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}";
    },
    options => { });

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 120;
        opt.QueueLimit = 0;
    });
});

builder.Services.AddHttpClient("xai", client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("XAI_BASE_URL") ?? "http://xai-backend:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Set up reverse proxy (YARP)
app.MapReverseProxy()
    .RequireAuthorization()
    .UseRateLimiter()
    .UseAuthentication()
    .UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.UseRateLimiter();

app.MapPost("/api/analysis/pronunciation", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var client = httpFactory.CreateClient("xai");
    var req = new HttpRequestMessage(HttpMethod.Post, "/api/explain/pronunciation");
    req.Content = new StringContent(body, Encoding.UTF8, "application/json");
    // Forward bearer if present
    if (ctx.Request.Headers.TryGetValue("Authorization", out var auth))
        req.Headers.Authorization = AuthenticationHeaderValue.Parse(auth.ToString());

    var res = await client.SendAsync(req);
    var txt = await res.Content.ReadAsStringAsync();
    return Results.Content(txt, "application/json", res.StatusCode);
});

app.MapPost("/api/analysis/emotion", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var client = httpFactory.CreateClient("xai");
    var req = new HttpRequestMessage(HttpMethod.Post, "/api/explain/emotion");
    req.Content = new StringContent(body, Encoding.UTF8, "application/json");
    if (ctx.Request.Headers.TryGetValue("Authorization", out var auth))
        req.Headers.Authorization = AuthenticationHeaderValue.Parse(auth.ToString());

    var res = await client.SendAsync(req);
    var txt = await res.Content.ReadAsStringAsync();
    return Results.Content(txt, "application/json", res.StatusCode);
});

app.Run();