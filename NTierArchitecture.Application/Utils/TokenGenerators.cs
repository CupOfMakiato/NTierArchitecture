using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NTierArchitecture.Application.DTOs.Auth;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Settings.Jwt;
using NTierArchitecture.Domain.Entities;

namespace NTierArchitecture.Application.Utils
{
    public class TokenGenerators : IJwtTokenService
    {
        private const string TokenTypeClaim = "token_type";
        private const string AccessTokenType = "access";
        private const string RefreshTokenType = "refresh";
        private const string SessionIdClaim = "sid";

        private readonly IConfiguration _configuration;
        private readonly IRedisService _redisService;
        private readonly ICurrentTime _currentTime;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public TokenGenerators(
            IConfiguration configuration,
            IRedisService redisService,
            ICurrentTime currentTime)
        {
            _configuration = configuration;
            _redisService = redisService;
            _currentTime = currentTime;
        }

        public async Task<JwtTokenResult> GenerateAndStoreTokensAsync(User user, string roleName)
        {
            if (user.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Cannot create tokens for a user without an id.");
            }

            var settings = GetJwtSettings();
            var now = _currentTime.GetCurrentTime();
            var sessionId = Guid.NewGuid().ToString("N");
            var accessTokenId = Guid.NewGuid().ToString("N");
            var refreshTokenId = Guid.NewGuid().ToString("N");
            var accessTokenExpiresAt = now.AddMinutes(settings.AccessTokenExpirationMinutes);
            var refreshTokenExpiresAt = now.AddDays(settings.RefreshTokenExpirationDays);

            var accessToken = CreateToken(
                settings,
                BuildClaims(user, roleName, sessionId, accessTokenId, AccessTokenType),
                now,
                accessTokenExpiresAt);

            var refreshToken = CreateToken(
                settings,
                BuildClaims(user, roleName, sessionId, refreshTokenId, RefreshTokenType),
                now,
                refreshTokenExpiresAt);

            var cacheItem = new JwtTokenCacheItem
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                RoleName = roleName,
                SessionId = sessionId,
                AccessTokenId = accessTokenId,
                RefreshTokenId = refreshTokenId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IssuedAt = now,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };

            await _redisService.SetAsync(
                JwtTokenRedisKeys.Session(sessionId),
                cacheItem,
                refreshTokenExpiresAt - now);

            await _redisService.SetAsync(
                JwtTokenRedisKeys.AccessToken(accessTokenId),
                sessionId,
                accessTokenExpiresAt - now);

            await _redisService.SetAsync(
                JwtTokenRedisKeys.RefreshToken(refreshTokenId),
                sessionId,
                refreshTokenExpiresAt - now);

            return new JwtTokenResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                SessionId = sessionId,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                AccessTokenCookieName = settings.AccessTokenCookieName,
                RefreshTokenCookieName = settings.RefreshTokenCookieName
            };
        }

        public async Task<bool> ValidateAccessTokenCacheAsync(string accessTokenId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(accessTokenId) || string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            var cachedSessionId = await _redisService.GetAsync<string>(
                JwtTokenRedisKeys.AccessToken(accessTokenId));

            if (!string.Equals(cachedSessionId, sessionId, StringComparison.Ordinal))
            {
                return false;
            }

            var session = await GetSessionAsync(sessionId);
            if (session == null)
            {
                return false;
            }

            return string.Equals(session.AccessTokenId, accessTokenId, StringComparison.Ordinal)
                   && session.AccessTokenExpiresAt > _currentTime.GetCurrentTime();
        }

        public Task<JwtTokenCacheItem?> GetSessionAsync(string sessionId)
        {
            return _redisService.GetAsync<JwtTokenCacheItem>(JwtTokenRedisKeys.Session(sessionId));
        }

        public async Task RevokeSessionAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            var session = await GetSessionAsync(sessionId);
            if (session != null)
            {
                await _redisService.RemoveAsync(JwtTokenRedisKeys.AccessToken(session.AccessTokenId));
                await _redisService.RemoveAsync(JwtTokenRedisKeys.RefreshToken(session.RefreshTokenId));
            }

            await _redisService.RemoveAsync(JwtTokenRedisKeys.Session(sessionId));
        }

        private IEnumerable<Claim> BuildClaims(
            User user,
            string roleName,
            string sessionId,
            string tokenId,
            string tokenType)
        {
            return new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, tokenId),
                new Claim(SessionIdClaim, sessionId),
                new Claim(TokenTypeClaim, tokenType),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName)
            };
        }

        private string CreateToken(
            JwtSettings settings,
            IEnumerable<Claim> claims,
            DateTime issuedAt,
            DateTime expiresAt)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                notBefore: issuedAt,
                expires: expiresAt,
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }

        private JwtSettings GetJwtSettings()
        {
            var settings = new JwtSettings
            {
                SecretKey = GetRequiredSetting("SecretKey"),
                Issuer = GetRequiredSetting("Issuer"),
                Audience = GetRequiredSetting("Audience"),
                AccessTokenExpirationMinutes = GetPositiveIntSetting(
                    "AccessTokenExpirationMinutes",
                    new JwtSettings().AccessTokenExpirationMinutes),
                RefreshTokenExpirationDays = GetPositiveIntSetting(
                    "RefreshTokenExpirationDays",
                    new JwtSettings().RefreshTokenExpirationDays),
                AccessTokenCookieName = GetStringSetting(
                    "AccessTokenCookieName",
                    new JwtSettings().AccessTokenCookieName),
                RefreshTokenCookieName = GetStringSetting(
                    "RefreshTokenCookieName",
                    new JwtSettings().RefreshTokenCookieName)
            };

            if (Encoding.UTF8.GetByteCount(settings.SecretKey) < 32)
            {
                throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 bytes for HMAC SHA256.");
            }

            return settings;
        }

        private string GetRequiredSetting(string key)
        {
            var value = _configuration[$"{JwtSettings.SectionName}:{key}"];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{JwtSettings.SectionName}:{key} is missing.");
            }

            return value;
        }

        private string GetStringSetting(string key, string fallback)
        {
            var value = _configuration[$"{JwtSettings.SectionName}:{key}"];
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private int GetPositiveIntSetting(string key, int fallback)
        {
            var value = _configuration[$"{JwtSettings.SectionName}:{key}"];
            return int.TryParse(value, out var parsedValue) && parsedValue > 0
                ? parsedValue
                : fallback;
        }
    }
}
