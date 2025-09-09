using Microsoft.EntityFrameworkCore;
using TestAuthSSO.Configuration;
using TestAuthSSO.Data;
using TestAuthSSO.DTOs;
using TestAuthSSO.Models;
using BCrypt.Net;

namespace TestAuthSSO.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken);
        Task<ApiResponse<bool>> RevokeAllTokensAsync(int userId);
        Task<ApiResponse<UserInfo>> GetUserInfoAsync(int userId);
        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);
        ApiResponse<bool> ValidateToken(string token);
        ApiResponse<TokenValidationResult> ValidateTokenWithClaims(string token);
    }

    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;

        public AuthService(AuthDbContext context, IJwtService jwtService, JwtSettings jwtSettings)
        {
            _context = context;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Username == request.Username || u.Email == request.Email);

                if (existingUser)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Kullanıcı adı veya email zaten mevcut",
                        Errors = new List<string> { "User already exists" }
                    };
                }

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    }
                };

                return new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Kayıt başarılı",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Kayıt sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user by username or email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);

                if (user == null || !user.IsActive)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Kullanıcı adı/email veya şifre hatalı",
                        Errors = new List<string> { "Invalid credentials" }
                    };
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Kullanıcı adı/email veya şifre hatalı",
                        Errors = new List<string> { "Invalid credentials" }
                    };
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;

                // Revoke old refresh tokens
                var oldTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in oldTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedReason = "New login";
                }

                // Generate new tokens
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save new refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    }
                };

                return new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Giriş başarılı",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Giriş sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

                if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Geçersiz refresh token",
                        Errors = new List<string> { "Invalid refresh token" }
                    };
                }

                var user = storedToken.User;
                if (!user.IsActive)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Kullanıcı aktif değil",
                        Errors = new List<string> { "User is not active" }
                    };
                }

                // Generate new tokens (Token Rotation)
                var newAccessToken = _jwtService.GenerateAccessToken(user);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Revoke old refresh token
                storedToken.IsRevoked = true;
                storedToken.RevokedReason = "Token rotation";
                storedToken.ReplacedByToken = newRefreshToken;

                // Create new refresh token
                var newRefreshTokenEntity = new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                var response = new AuthResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    }
                };

                return new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Token yenilendi",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Token yenileme sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (token == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Token bulunamadı",
                        Errors = new List<string> { "Token not found" }
                    };
                }

                token.IsRevoked = true;
                token.RevokedReason = "Manual revocation";
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Token iptal edildi",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Token iptal etme sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> RevokeAllTokensAsync(int userId)
        {
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedReason = "Revoke all tokens";
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Tüm tokenlar iptal edildi",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Tokenları iptal etme sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<UserInfo>> GetUserInfoAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return new ApiResponse<UserInfo>
                    {
                        Success = false,
                        Message = "Kullanıcı bulunamadı",
                        Errors = new List<string> { "User not found" }
                    };
                }

                var userInfo = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return new ApiResponse<UserInfo>
                {
                    Success = true,
                    Message = "Kullanıcı bilgileri getirildi",
                    Data = userInfo
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "Kullanıcı bilgileri getirme sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Kullanıcı bulunamadı",
                        Errors = new List<string> { "User not found" }
                    };
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Mevcut şifre hatalı",
                        Errors = new List<string> { "Current password is incorrect" }
                    };
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                // Revoke all refresh tokens for security
                await RevokeAllTokensAsync(userId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Şifre başarıyla değiştirildi",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Şifre değiştirme sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    // Don't reveal if user exists or not for security
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Eğer email mevcut ise şifre sıfırlama linki gönderildi",
                        Data = true
                    };
                }

                // In a real application, you would:
                // 1. Generate a password reset token
                // 2. Send email with reset link
                // 3. Implement reset password endpoint that accepts the token
                
                // For demo purposes, we'll just generate a temporary password
                var tempPassword = GenerateTemporaryPassword();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
                
                // Revoke all tokens
                await RevokeAllTokensAsync(user.Id);
                await _context.SaveChangesAsync();

                // In real app, send this via email
                // For demo, we'll return it (NOT recommended in production)
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Geçici şifre: {tempPassword} (Email ile gönderilecek)",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Şifre sıfırlama sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public ApiResponse<bool> ValidateToken(string token)
        {
            try
            {
                var isValid = _jwtService.ValidateToken(token);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = isValid ? "Token geçerli" : "Token geçersiz",
                    Data = isValid
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Token doğrulama sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public ApiResponse<TokenValidationResult> ValidateTokenWithClaims(string token)
        {
            try
            {
                var principal = _jwtService.ValidateTokenAndGetPrincipal(token);
                
                if (principal == null)
                {
                    return new ApiResponse<TokenValidationResult>
                    {
                        Success = true,
                        Message = "Token geçersiz",
                        Data = new TokenValidationResult { IsValid = false }
                    };
                }

                // Token geçerliyse, claim'leri de döndür
                var result = new TokenValidationResult
                {
                    IsValid = true,
                    UserId = principal.FindFirst("user_id")?.Value,
                    Username = principal.FindFirst("username")?.Value,
                    Email = principal.FindFirst("email")?.Value
                };

                return new ApiResponse<TokenValidationResult>
                {
                    Success = true,
                    Message = "Token geçerli",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokenValidationResult>
                {
                    Success = false,
                    Message = "Token doğrulama sırasında bir hata oluştu",
                    Errors = new List<string> { ex.Message },
                    Data = new TokenValidationResult { IsValid = false }
                };
            }
        }

        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
