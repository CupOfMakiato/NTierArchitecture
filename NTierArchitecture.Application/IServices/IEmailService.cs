using NTierArchitecture.Application.DTOs.User;

namespace NTierArchitecture.Application.IServices
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailDTO request);
        Task<bool> SendVerificationEmailAsync(string email, string token);
        Task<bool> SendOtpEmailAsync(string email, string otp);
        Task<bool> SendAuthenticationOtpAsync(string email, string purpose);
        Task<bool> VerifyAuthenticationOtpAsync(string email, string purpose, string otp);
        Task<bool> ReSendOtpEmail(string email, string otp);
        Task<bool> SendPendingEmailAsync(string email);
        Task<bool> SendActiveEmailAsync(string email);
        Task<bool> SendDeactiveEmailAsync(string email, string reason);
        Task<bool> SendRejectionEmailAsync(string email, string reason);
        Task<bool> SendApprovalEmailAsync(string email);
    }
}
