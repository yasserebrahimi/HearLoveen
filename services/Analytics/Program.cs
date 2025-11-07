using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Dapper;
using Serilog;

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

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

string connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Username=postgres;Password=postgres;Database=hearloveen";

// LTV (Lifetime Value) Calculation
app.MapGet("/api/analytics/ltv", [Authorize] async () =>
{
    try
    {
        await using var con = new NpgsqlConnection(connStr);

        // Calculate average revenue per user and average customer lifespan
        var query = @"
            WITH user_revenue AS (
                SELECT
                    user_id,
                    SUM(amount) as total_revenue,
                    EXTRACT(EPOCH FROM (MAX(payment_date) - MIN(payment_date))) / 86400 as lifespan_days
                FROM payments
                WHERE status = 'completed'
                GROUP BY user_id
            ),
            metrics AS (
                SELECT
                    AVG(total_revenue) as avg_revenue_per_user,
                    AVG(lifespan_days) as avg_lifespan_days,
                    COUNT(*) as total_paying_users
                FROM user_revenue
            )
            SELECT
                avg_revenue_per_user,
                avg_lifespan_days,
                total_paying_users,
                avg_revenue_per_user * (avg_lifespan_days / 30.0) as ltv_monthly
            FROM metrics;
        ";

        var result = await con.QueryFirstOrDefaultAsync(query);

        if (result == null)
        {
            return Results.Ok(new
            {
                ltv = 0.0,
                avgRevenuePerUser = 0.0,
                avgLifespanDays = 0.0,
                totalPayingUsers = 0
            });
        }

        return Results.Ok(new
        {
            ltv = result.ltv_monthly ?? 0.0,
            avgRevenuePerUser = result.avg_revenue_per_user ?? 0.0,
            avgLifespanDays = result.avg_lifespan_days ?? 0.0,
            totalPayingUsers = result.total_paying_users ?? 0
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error calculating LTV");
        return Results.Problem("Failed to calculate LTV", statusCode: 500);
    }
})
.WithTags("Analytics");

// CAC (Customer Acquisition Cost) Calculation
app.MapGet("/api/analytics/cac", [Authorize] async () =>
{
    try
    {
        await using var con = new NpgsqlConnection(connStr);

        // Calculate total marketing spend and new customers acquired
        var query = @"
            WITH monthly_metrics AS (
                SELECT
                    DATE_TRUNC('month', created_at) as month,
                    COUNT(*) as new_customers
                FROM users
                WHERE created_at >= NOW() - INTERVAL '12 months'
                GROUP BY month
            ),
            marketing_spend AS (
                SELECT
                    DATE_TRUNC('month', spend_date) as month,
                    SUM(amount) as total_spend
                FROM marketing_expenses
                WHERE spend_date >= NOW() - INTERVAL '12 months'
                GROUP BY month
            )
            SELECT
                COALESCE(SUM(m.total_spend), 0) as total_marketing_spend,
                COALESCE(SUM(c.new_customers), 0) as total_new_customers,
                CASE
                    WHEN SUM(c.new_customers) > 0
                    THEN SUM(m.total_spend) / SUM(c.new_customers)
                    ELSE 0
                END as cac
            FROM marketing_spend m
            FULL OUTER JOIN monthly_metrics c ON m.month = c.month;
        ";

        var result = await con.QueryFirstOrDefaultAsync(query);

        if (result == null)
        {
            return Results.Ok(new
            {
                cac = 0.0,
                totalMarketingSpend = 0.0,
                totalNewCustomers = 0
            });
        }

        return Results.Ok(new
        {
            cac = result.cac ?? 0.0,
            totalMarketingSpend = result.total_marketing_spend ?? 0.0,
            totalNewCustomers = result.total_new_customers ?? 0
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error calculating CAC");
        return Results.Problem("Failed to calculate CAC", statusCode: 500);
    }
})
.WithTags("Analytics");

// LTV/CAC Ratio (Key SaaS metric)
app.MapGet("/api/analytics/ltv-cac-ratio", [Authorize] async () =>
{
    try
    {
        await using var con = new NpgsqlConnection(connStr);

        // Get LTV
        var ltvQuery = @"
            WITH user_revenue AS (
                SELECT
                    user_id,
                    SUM(amount) as total_revenue,
                    EXTRACT(EPOCH FROM (MAX(payment_date) - MIN(payment_date))) / 86400 as lifespan_days
                FROM payments
                WHERE status = 'completed'
                GROUP BY user_id
            )
            SELECT AVG(total_revenue) * (AVG(lifespan_days) / 30.0) as ltv
            FROM user_revenue;
        ";

        var ltvResult = await con.QueryFirstOrDefaultAsync<decimal?>(ltvQuery) ?? 0;

        // Get CAC
        var cacQuery = @"
            SELECT
                CASE
                    WHEN COUNT(DISTINCT u.id) > 0
                    THEN SUM(m.amount) / COUNT(DISTINCT u.id)
                    ELSE 0
                END as cac
            FROM marketing_expenses m
            CROSS JOIN users u
            WHERE m.spend_date >= NOW() - INTERVAL '12 months'
              AND u.created_at >= NOW() - INTERVAL '12 months';
        ";

        var cacResult = await con.QueryFirstOrDefaultAsync<decimal?>(cacQuery) ?? 0;

        var ratio = cacResult > 0 ? (double)(ltvResult / cacResult) : 0;

        // Healthy SaaS metrics:
        // LTV/CAC ratio should be > 3
        // CAC payback period should be < 12 months
        var health = ratio > 3 ? "healthy" : ratio > 2 ? "acceptable" : "concerning";

        return Results.Ok(new
        {
            ltv = (double)ltvResult,
            cac = (double)cacResult,
            ratio,
            health,
            recommendation = ratio > 3
                ? "Excellent metrics. Consider investing more in customer acquisition."
                : ratio > 2
                ? "Acceptable metrics. Monitor closely and optimize acquisition channels."
                : "LTV/CAC ratio is concerning. Focus on reducing acquisition costs or increasing customer value."
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error calculating LTV/CAC ratio");
        return Results.Problem("Failed to calculate LTV/CAC ratio", statusCode: 500);
    }
})
.WithTags("Analytics");

// Cohort Analysis
app.MapGet("/api/analytics/cohorts", [Authorize] async () =>
{
    try
    {
        await using var con = new NpgsqlConnection(connStr);

        var query = @"
            WITH cohorts AS (
                SELECT
                    DATE_TRUNC('month', created_at) as cohort_month,
                    user_id
                FROM users
                WHERE created_at >= NOW() - INTERVAL '12 months'
            ),
            retention AS (
                SELECT
                    c.cohort_month,
                    DATE_TRUNC('month', s.created_at) as activity_month,
                    COUNT(DISTINCT s.user_id) as active_users
                FROM cohorts c
                JOIN therapy_sessions s ON c.user_id = s.user_id
                GROUP BY c.cohort_month, activity_month
            )
            SELECT
                cohort_month,
                activity_month,
                active_users,
                EXTRACT(MONTH FROM AGE(activity_month, cohort_month)) as months_since_signup
            FROM retention
            ORDER BY cohort_month, activity_month;
        ";

        var cohorts = await con.QueryAsync(query);

        return Results.Ok(cohorts);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error calculating cohort analysis");
        return Results.Problem("Failed to calculate cohort analysis", statusCode: 500);
    }
})
.WithTags("Analytics");

// Revenue Metrics
app.MapGet("/api/analytics/revenue", [Authorize] async () =>
{
    try
    {
        await using var con = new NpgsqlConnection(connStr);

        var query = @"
            SELECT
                SUM(CASE WHEN payment_date >= DATE_TRUNC('month', NOW()) THEN amount ELSE 0 END) as mrr,
                SUM(CASE WHEN payment_date >= DATE_TRUNC('year', NOW()) THEN amount ELSE 0 END) as arr,
                AVG(amount) as avg_transaction_value,
                COUNT(*) as total_transactions
            FROM payments
            WHERE status = 'completed';
        ";

        var result = await con.QueryFirstOrDefaultAsync(query);

        return Results.Ok(new
        {
            mrr = result?.mrr ?? 0.0,
            arr = result?.arr ?? 0.0,
            avgTransactionValue = result?.avg_transaction_value ?? 0.0,
            totalTransactions = result?.total_transactions ?? 0
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error calculating revenue metrics");
        return Results.Problem("Failed to calculate revenue metrics", statusCode: 500);
    }
})
.WithTags("Analytics");

Log.Information("Analytics Service starting...");
app.Run();
