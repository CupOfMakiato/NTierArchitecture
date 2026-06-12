namespace NTierArchitecture.Application.Utils
{
    public static class JwtTokenRedisKeys
    {
        public static string Session(string sessionId) => $"auth:session:{sessionId}";
        public static string AccessToken(string tokenId) => $"auth:access:{tokenId}";
        public static string RefreshToken(string tokenId) => $"auth:refresh:{tokenId}";
    }
}
