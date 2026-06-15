using NTierArchitecture.Application.Abstractions.Shared;
using NTierArchitecture.Application.DTOs.Auth;

namespace NTierArchitecture.Application.IServices
{
    public interface IAuthService
    {
        Task<Result<object>> RequestRegisterOtpAsync(RegisterRequest request);
        Task<Result<AuthResult>> RequestLoginAsync(LoginRequest request);
        Task<Result<AuthResult>> VerifyOtpAsync(VerifyOtpRequest request);
        Task<Result<object>> LogoutAsync(string sessionId);
    }
}