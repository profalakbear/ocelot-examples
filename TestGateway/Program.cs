using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System.Text;
using TestGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// JWT Settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey!)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("JwtBearer", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.WriteAsync(
            "Rate limitini astiniz lutfen bir sure sonra tekrar deneyin.",
            cancellationToken: _
        );

        return new ValueTask();
    };

    // API için rate limit
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(15);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Auth servisi için daha gevşek rate limit
    options.AddFixedWindowLimiter("auth-rate-limit", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(60);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// HttpClientFactory
builder.Services.AddHttpClient();

// Ekstra yapılandırma
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    { "AuthValidationEndpoint", "http://auth-sso:8080/api/auth/validate-with-claims" }
});

var app = builder.Build();

// Middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
app.UseTokenValidation(); // Token doğrulama middleware
app.UseRateLimiter();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Service = "TestGateway"
}));

// Auth durumu kontrol endpoint'i
app.MapGet("/auth-status", (HttpContext context) =>
{
    var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
    var userId = context.User.FindFirst("user_id")?.Value;
    var username = context.User.FindFirst("username")?.Value;
    
    return Results.Ok(new
    {
        IsAuthenticated = isAuthenticated,
        UserId = userId,
        Username = username,
        Timestamp = DateTime.UtcNow
    });
}).RequireAuthorization("JwtBearer");

app.MapReverseProxy();

app.Run();
