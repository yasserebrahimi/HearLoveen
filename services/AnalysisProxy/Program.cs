using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Serilog;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OIDC/JWKS Authentication with Azure AD B2C
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    },
    options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    });

builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 120;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

// Configure HTTP Client for XAI Backend
builder.Services.AddHttpClient("xai", client =>
{
    var baseUrl = builder.Configuration["XAI:BaseUrl"] ?? "http://xai-backend:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTherapistDashboard", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("AllowTherapistDashboard");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

// Proxy endpoint for pronunciation analysis with XAI
app.MapPost("/api/analysis/pronunciation", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    try
    {
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
        var client = httpFactory.CreateClient("xai");
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/explain/pronunciation");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // Forward Authorization header if present
        if (ctx.Request.Headers.TryGetValue("Authorization", out var auth))
        {
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(auth.ToString());
        }

        var res = await client.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();

        return Results.Content(txt, "application/json", statusCode: (int)res.StatusCode);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error proxying pronunciation analysis request");
        return Results.Problem("Failed to process pronunciation analysis request", statusCode: 500);
    }
})
.RequireAuthorization()
.RequireRateLimiting("fixed")
.WithTags("Analysis");

// Proxy endpoint for emotion analysis with XAI
app.MapPost("/api/analysis/emotion", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    try
    {
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
        var client = httpFactory.CreateClient("xai");
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/explain/emotion");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // Forward Authorization header if present
        if (ctx.Request.Headers.TryGetValue("Authorization", out var auth))
        {
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(auth.ToString());
        }

        var res = await client.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();

        return Results.Content(txt, "application/json", statusCode: (int)res.StatusCode);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error proxying emotion analysis request");
        return Results.Problem("Failed to process emotion analysis request", statusCode: 500);
    }
})
.RequireAuthorization()
.RequireRateLimiting("fixed")
.WithTags("Analysis");

// Session analysis endpoint
app.MapPost("/api/analysis/session", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    try
    {
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
        var client = httpFactory.CreateClient("xai");
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/explain/session");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        if (ctx.Request.Headers.TryGetValue("Authorization", out var auth))
        {
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(auth.ToString());
        }

        var res = await client.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();

        return Results.Content(txt, "application/json", statusCode: (int)res.StatusCode);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error proxying session analysis request");
        return Results.Problem("Failed to process session analysis request", statusCode: 500);
    }
})
.RequireAuthorization()
.RequireRateLimiting("fixed")
.WithTags("Analysis");

Log.Information("AnalysisProxy starting on {Urls}", string.Join(", ", builder.Configuration.GetSection("urls").Get<string[]>() ?? new[] { "http://localhost:5100" }));

app.Run();
