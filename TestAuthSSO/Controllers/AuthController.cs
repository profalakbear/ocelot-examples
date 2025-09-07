using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TestAuthSSO.DTOs;
using TestAuthSSO.Services;

namespace TestAuthSSO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Kullanıcı kaydı
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Geçersiz veri",
                    Errors = errors
                });
            }

            var result = await _authService.RegisterAsync(request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Kullanıcı girişi
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Geçersiz veri",
                    Errors = errors
                });
            }

            var result = await _authService.LoginAsync(request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Token yenileme
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Geçersiz veri",
                    Errors = errors
                });
            }

            var result = await _authService.RefreshTokenAsync(request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Token iptal etme
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RevokeTokenAsync(request.RefreshToken);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Tüm tokenları iptal etme
        /// </summary>
        [HttpPost("revoke-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeAllTokens()
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı",
                    Errors = new List<string> { "Invalid user" }
                });
            }

            var result = await _authService.RevokeAllTokensAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Kullanıcı bilgilerini getirme
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserInfo>>> GetMe()
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı",
                    Errors = new List<string> { "Invalid user" }
                });
            }

            var result = await _authService.GetUserInfoAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Şifre değiştirme
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Geçersiz veri",
                    Errors = errors
                });
            }

            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Geçersiz kullanıcı",
                    Errors = new List<string> { "Invalid user" }
                });
            }

            var result = await _authService.ChangePasswordAsync(userId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Şifre sıfırlama
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Geçersiz veri",
                    Errors = errors
                });
            }

            var result = await _authService.ResetPasswordAsync(request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Token doğrulama
        /// </summary>
        [HttpPost("validate")]
        public ActionResult<ApiResponse<bool>> ValidateToken([FromHeader(Name = "Authorization")] string authorization)
        {
            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Geçersiz token formatı",
                    Errors = new List<string> { "Invalid token format" }
                });
            }

            var token = authorization["Bearer ".Length..];
            var result = _authService.ValidateToken(token);
            
            return Ok(result);
        }

        /// <summary>
        /// Gateway için kullanıcı bilgilerini header'a ekleme endpoint'i
        /// </summary>
        [HttpGet("user-info")]
        [Authorize]
        public ActionResult<ApiResponse<object>> GetUserInfoForGateway()
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            var usernameClaim = User.FindFirst("username")?.Value;
            var emailClaim = User.FindFirst("email")?.Value;

            if (userIdClaim == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Geçersiz token",
                    Errors = new List<string> { "Invalid token" }
                });
            }

            var userInfo = new
            {
                UserId = userIdClaim,
                Username = usernameClaim,
                Email = emailClaim
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Kullanıcı bilgileri",
                Data = userInfo
            });
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public ActionResult<object> Health()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "TestAuthSSO"
            });
        }
    }
}
