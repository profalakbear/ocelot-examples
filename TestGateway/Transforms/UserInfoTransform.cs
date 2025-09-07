using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TestGateway.Transforms
{
    public static class UserInfoTransformExtensions
    {
        public static void AddUserInfoTransform(this TransformBuilderContext context)
        {
            context.AddRequestTransform(transformContext =>
            {
                var httpContext = transformContext.HttpContext;
                
                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    // Kullanıcı bilgilerini claim'lerden al ve header'a ekle
                    var userId = httpContext.User.FindFirst("user_id")?.Value;
                    var username = httpContext.User.FindFirst("username")?.Value;
                    var email = httpContext.User.FindFirst("email")?.Value;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", userId);
                    }

                    if (!string.IsNullOrEmpty(username))
                    {
                        transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-Username", username);
                    }

                    if (!string.IsNullOrEmpty(email))
                    {
                        transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Email", email);
                    }
                }

                return ValueTask.CompletedTask;
            });
        }
    }
}
