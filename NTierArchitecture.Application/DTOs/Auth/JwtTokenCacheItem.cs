namespace NTierArchitecture.Application.DTOs.Auth
{
    public class JwtTokenCacheItem
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string AccessTokenId { get; set; } = string.Empty;
        public string RefreshTokenId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
