using System.Threading.RateLimiting;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AgentForge Gateway API", Version = "v1" });
});

// Reverse proxy (single entry point to the internal network)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Liveness/readiness endpoint
builder.Services.AddHealthChecks();

// CORS for the browser SPA, which routes all of its traffic through the gateway
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
const string CorsPolicy = "WebClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Rate limiting (fixed window, partitioned per client IP)
const string RateLimitPolicy = "proxy";
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicy, httpContext =>
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue<int?>("RateLimiting:PermitLimit") ?? 100;
        var windowSeconds = config.GetValue<int?>("RateLimiting:WindowSeconds") ?? 10;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
            });
    });
});

var app = builder.Build();

app.UseCors(CorsPolicy);
app.UseRateLimiter();

app.MapReverseProxy()
   .RequireRateLimiting(RateLimitPolicy);

app.MapHealthChecks("/health");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgentForge Gateway API v1");
    c.RoutePrefix = "swagger";
});

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
