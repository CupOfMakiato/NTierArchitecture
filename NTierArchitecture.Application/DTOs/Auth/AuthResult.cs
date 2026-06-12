namespace NTierArchitecture.Application.DTOs.Auth
{
    public class AuthResult
    {
        public AuthResponse Response { get; set; } = new();
        public JwtTokenResult Tokens { get; set; } = new();
    }
}
