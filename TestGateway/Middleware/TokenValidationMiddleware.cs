using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestGateway.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenValidationMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public TokenValidationMiddleware(
            RequestDelegate next,
            IHttpClientFactory httpClientFactory,
            ILogger<TokenValidationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Eğer request zaten authorize edilmişse, token doğrulaması yapmaya gerek yok
            if (context.User.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }
            
            // Authorization header'ı kontrol et
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrEmpty(authHeader))
            {
                await _next(context);
                return;
            }

            try
            {
                // AuthSSO servisine gidip token'ı doğrula
                var httpClient = _httpClientFactory.CreateClient();
                var authEndpoint = _configuration["AuthValidationEndpoint"] ?? "http://auth-sso:8080/api/auth/validate-with-claims";
                
                var request = new HttpRequestMessage(HttpMethod.Post, authEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader.ToString().Replace("Bearer ", ""));
                
                var response = await httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<TokenValidationResult>>();
                    
                    if (result?.Data?.IsValid == true)
                    {
                        // Token geçerliyse, header'lara ekle
                        if (!string.IsNullOrEmpty(result.Data.UserId))
                            context.Request.Headers.TryAdd("X-User-Id", result.Data.UserId);
                            
                        if (!string.IsNullOrEmpty(result.Data.Username))
                            context.Request.Headers.TryAdd("X-Username", result.Data.Username);
                            
                        if (!string.IsNullOrEmpty(result.Data.Email))
                            context.Request.Headers.TryAdd("X-User-Email", result.Data.Email);
                            
                        _logger.LogInformation("Token validation successful for user: {UserId}", result.Data.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid token detected");
                    }
                }
                else
                {
                    _logger.LogWarning("Token validation failed: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token validation");
            }

            await _next(context);
        }
    }

    // Extension method for easy middleware registration
    public static class TokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenValidation(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenValidationMiddleware>();
        }
    }

    // Response models
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("data")]
        public T? Data { get; set; }
        
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }
    }

    public class TokenValidationResult
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }
        
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
        
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
