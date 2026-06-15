using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTierArchitecture.Application.DTOs.User;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Settings.MailjetService;
using RestSharp;
using RestSharp.Authenticators;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NTierArchitecture.Application.Services
{
    public class EmailService : IEmailService
    {
        private static readonly TimeSpan OtpExpiration = TimeSpan.FromMinutes(5);

        private readonly IConfiguration _configuration;
        private readonly IRedisService _redisService;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            IRedisService redisService,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailDTO request)
        {
            try
            {
                var apiKey = _configuration[$"{SentEmailSettings.SectionName}:ApiKey"];
                var apiSecret = _configuration[$"{SentEmailSettings.SectionName}:ApiSecret"];
                var fromEmail = _configuration[$"{SentEmailSettings.SectionName}:FromEmail"];
                var fromName = _configuration[$"{SentEmailSettings.SectionName}:FromName"];

                if (string.IsNullOrWhiteSpace(apiKey)
                    || string.IsNullOrWhiteSpace(apiSecret)
                    || string.IsNullOrWhiteSpace(fromEmail))
                {
                    _logger.LogError("Mailjet settings are missing. Email was not sent to {Recipient}", request.To);
                    return false;
                }

                var options = new RestClientOptions("https://api.mailjet.com/v3.1/send")
                {
                    Authenticator = new HttpBasicAuthenticator(apiKey, apiSecret)
                };

                var client = new RestClient(options);

                var requestBody = new
                {
                    Messages = new[]
                    {
                        new
                        {
                            From = new { Email = fromEmail, Name = fromName },
                            To = new[] { new { Email = request.To, Name = request.To } },
                            Subject = request.Subject,
                            HTMLPart = request.Body
                        }
                    }
                };

                var jsonBody = JsonSerializer.Serialize(requestBody);

                var restRequest = new RestRequest()
                    .AddHeader("Content-Type", "application/json")
                    .AddStringBody(jsonBody, DataFormat.Json);

                var response = await client.ExecutePostAsync(restRequest);

                if (response.IsSuccessful)
                {
                    _logger.LogInformation("Email sent successfully to {Recipient}", request.To);
                    return true;
                }

                _logger.LogError(
                    "Failed to send email to {Recipient}. StatusCode: {StatusCode}. Response: {Content}. ErrorMessage: {ErrorMessage}.",
                    request.To,
                    response.StatusCode,
                    response.Content,
                    response.ErrorMessage);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending email to {Recipient}", request.To);
                return false;
            }
        }

        public async Task<bool> SendAuthenticationOtpAsync(string email, string purpose)
        {
            var normalizedEmail = NormalizeEmail(email);
            var normalizedPurpose = NormalizePurpose(purpose);
            var otp = GenerateOtp();
            var cacheKey = BuildOtpCacheKey(normalizedPurpose, normalizedEmail);

            try
            {
                await _redisService.SetAsync(cacheKey, otp, OtpExpiration);

                var isSent = await SendOtpEmailAsync(normalizedEmail, otp);
                if (!isSent)
                {
                    await _redisService.RemoveAsync(cacheKey);
                    return false;
                }

                _logger.LogInformation(
                    "OTP cached for {Purpose} email {Email} for {ExpirationMinutes} minutes.",
                    normalizedPurpose,
                    normalizedEmail,
                    OtpExpiration.TotalMinutes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cache or send OTP for {Purpose} email {Email}.", normalizedPurpose, normalizedEmail);
                return false;
            }
        }

        public async Task<bool> VerifyAuthenticationOtpAsync(string email, string purpose, string otp)
        {
            var normalizedEmail = NormalizeEmail(email);
            var normalizedPurpose = NormalizePurpose(purpose);
            var cacheKey = BuildOtpCacheKey(normalizedPurpose, normalizedEmail);
            var cachedOtp = await _redisService.GetAsync<string>(cacheKey);

            if (string.IsNullOrWhiteSpace(cachedOtp) || string.IsNullOrWhiteSpace(otp))
            {
                return false;
            }

            var isValid = FixedTimeEquals(cachedOtp, otp.Trim());
            if (!isValid)
            {
                return false;
            }

            await _redisService.RemoveAsync(cacheKey);
            return true;
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string token)
        {
            var verificationUrl = $"https://localhost:7238/api/auth/verify?token={token}";
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "Email Verification",
                Body = $"Please verify your email by clicking on the following link: <a href='{verificationUrl}'>Verify Email</a>"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendOtpEmailAsync(string email, string otp)
        {
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "Email Verification OTP",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Your authentication code</h2>
                            <p style='color: #555;'>Use this OTP to continue:</p>
                            <h1 style='letter-spacing: 4px; color: #111;'>{otp}</h1>
                            <p style='color: #555;'>This code expires in {(int)OtpExpiration.TotalMinutes} minutes.</p>
                        </div>
                    </body>
                    </html>"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> ReSendOtpEmail(string email, string otp)
        {
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "New Email Re-Verification OTP",
                Body = $"Your OTP for email verification is: {otp}"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendPendingEmailAsync(string email)
        {
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "Account Pending Approval",
                Body = @"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Thank you for registering as an staff!</h2>
                            <p style='color: #555;'>We have received your registration and it is currently being reviewed by our admin team.</p>
                            <p style='color: #555;'>You will receive a notification once your account is approved. Please be patient during this process.</p>
                            <p style='color: #555;'>Thank you for your understanding.</p>
                            <p style='color: #555;'>Best regards,<br />Placeholder Name</p>
                        </div>
                    </body>
                    </html>"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendActiveEmailAsync(string email)
        {
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "Account Reactivation",
                Body = @"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Welcome Back!</h2>
                            <p style='color: #555;'>We are excited to inform you that your account has been reactivated. You can now log in and continue using our system.</p>
                            <p style='color: #555;'>Thank you for being a valued member of our community.</p>
                            <p style='color: #555;'>Best regards,<br />Placeholder Name</p>
                        </div>
                    </body>
                    </html>"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendDeactiveEmailAsync(string email, string reason)
        {
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "Account Deactivation Notice",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Account Deactivation</h2>
                            <p style='color: #555;'>We regret to inform you that your account has been deactivated.</p>
                            <p style='color: #555;'>Reason: {reason}</p>
                            <p style='color: #555;'>If you have any questions or need further assistance, please contact our support team.</p>
                            <p style='color: #555;'>We appreciate your understanding.</p>
                            <p style='color: #555;'>Best regards,<br />Placeholder Name</p>
                        </div>
                    </body>
                    </html>"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendRejectionEmailAsync(string email, string reason)
        {
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "Instructor Rejection",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Registration Update</h2>
                            <p style='color: #555;'>We regret to inform you that your account registration has been rejected.</p>
                            <p style='color: #555;'>Reason: {reason}</p>
                            <p style='color: #555;'>If you have any questions or need further assistance, please contact our support team.</p>
                            <p style='color: #555;'>Best regards,<br />Placeholder Name</p>
                        </div>
                    </body>
                    </html>"
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<bool> SendApprovalEmailAsync(string email)
        {
            var emailDto = new EmailDTO
            {
                To = email,
                Subject = "Account Approval",
                Body = @"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #333;'>Congratulations!</h2>
                            <p style='color: #555;'>Your account has been approved. You can now log in and start using the system.</p>
                            <p style='color: #555;'>Thank you for joining our system.</p>
                            <p style='color: #555;'>Best regards,<br />Placeholder Name</p>
                        </div>
                    </body>
                    </html>"
            };

            return await SendEmailAsync(emailDto);
        }

        private static string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1_000_000).ToString();
        }

        private static string BuildOtpCacheKey(string purpose, string email)
        {
            return $"auth:otp:{purpose}:{email}";
        }

        private static bool FixedTimeEquals(string first, string second)
        {
            var firstBytes = Encoding.UTF8.GetBytes(first);
            var secondBytes = Encoding.UTF8.GetBytes(second);

            return firstBytes.Length == secondBytes.Length
                && CryptographicOperations.FixedTimeEquals(firstBytes, secondBytes);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string NormalizePurpose(string purpose)
        {
            return string.IsNullOrWhiteSpace(purpose)
                ? "auth"
                : purpose.Trim().ToLowerInvariant();
        }
    }
}
