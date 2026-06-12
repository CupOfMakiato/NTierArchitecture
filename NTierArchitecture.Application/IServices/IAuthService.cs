using NTierArchitecture.Application.Abstractions.Shared;
using NTierArchitecture.Application.DTOs.Auth;

namespace NTierArchitecture.Application.IServices
{
    public interface IAuthService
    {
        Task<Result<AuthResult>> RegisterAsync(RegisterRequest request);
        Task<Result<AuthResult>> LoginAsync(LoginRequest request);
        Task<Result<object>> LogoutAsync(string sessionId);
    }
}
