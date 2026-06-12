using NTierArchitecture.Application.DTOs.Auth;
using NTierArchitecture.Domain.Entities;

namespace NTierArchitecture.Application.IServices
{
    public interface IJwtTokenService
    {
        Task<JwtTokenResult> GenerateAndStoreTokensAsync(User user, string roleName);
        Task<bool> ValidateAccessTokenCacheAsync(string accessTokenId, string sessionId);
        Task<JwtTokenCacheItem?> GetSessionAsync(string sessionId);
        Task RevokeSessionAsync(string sessionId);
    }
}
