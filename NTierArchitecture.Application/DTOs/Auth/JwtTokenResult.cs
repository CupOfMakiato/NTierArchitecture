namespace NTierArchitecture.Application.DTOs.Auth
{
    public class JwtTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string AccessTokenCookieName { get; set; } = string.Empty;
        public string RefreshTokenCookieName { get; set; } = string.Empty;
    }
}
