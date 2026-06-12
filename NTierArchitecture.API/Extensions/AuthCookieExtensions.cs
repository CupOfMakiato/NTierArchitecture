
using NTierArchitecture.Application.DTOs.Auth;

namespace NTierArchitecture.API.Extensions
{
    public static class AuthCookieExtensions
    {
        public static void AppendJwtTokenCookies(this HttpResponse response, JwtTokenResult tokens, bool secure)
        {
            response.Cookies.Append(
                tokens.AccessTokenCookieName,
                tokens.AccessToken,
                CreateCookieOptions(tokens.AccessTokenExpiresAt, secure));

            response.Cookies.Append(
                tokens.RefreshTokenCookieName,
                tokens.RefreshToken,
                CreateCookieOptions(tokens.RefreshTokenExpiresAt, secure));
        }

        public static void DeleteJwtTokenCookies(
            this HttpResponse response,
            string accessTokenCookieName,
            string refreshTokenCookieName)
        {
            response.Cookies.Delete(accessTokenCookieName);
            response.Cookies.Delete(refreshTokenCookieName);
        }

        private static CookieOptions CreateCookieOptions(DateTime expiresAt, bool secure)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            };
        }
    }
}
