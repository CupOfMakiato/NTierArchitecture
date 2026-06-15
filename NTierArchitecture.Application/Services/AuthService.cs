using NTierArchitecture.Application.Abstractions.Shared;
using NTierArchitecture.Application.DTOs.Auth;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Domain.Entities;
using NTierArchitecture.Domain.Enums;

namespace NTierArchitecture.Application.Services
{
    public class AuthService : IAuthService
    {
        private const string DefaultUserRoleName = "User";
        private const string RegisterOtpPurpose = "register";
        private const string LoginOtpPurpose = "login";
        private static readonly TimeSpan LoginOtpVerificationSessionExpiration = TimeSpan.FromDays(30); // 30 days expiration for "remember me" functionality

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEncryptionService _encryptionService;
        private readonly IEmailService _emailService;
        private readonly IRedisService _redisService;
        private readonly ICurrentTime _currentTime;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IEncryptionService encryptionService,
            IEmailService emailService,
            IRedisService redisService,
            ICurrentTime currentTime)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _redisService = redisService;
            _currentTime = currentTime;
        }

        public async Task<Result<object>> RequestRegisterOtpAsync(RegisterRequest request)
        {
            var validationError = ValidateRegisterRequest(request);
            if (validationError != null)
            {
                return Failed<object>(validationError);
            }

            var email = NormalizeEmail(request.Email);
            var userName = request.UserName.Trim();
            var existingUser = await _unitOfWork.UserRepository.GetByEmailAsync(email);
            if (existingUser is { Status: UserStatus.Active })
            {
                return Failed<object>("Email or username already exists.");
            }

            var userRole = await _unitOfWork.UserRepository.GetRoleByNameAsync(DefaultUserRoleName);
            if (userRole == null)
            {
                return Failed<object>("Default user role is not configured.");
            }

            var user = existingUser;
            var isNewUser = user == null;
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = _encryptionService.Encrypt(email),
                    CreationDate = _currentTime.GetCurrentTime(),
                    CreatedBy = null
                };
            }

            user.UserName = userName;
            user.Password = _passwordHasher.HashPassword(request.Password);
            user.RoleId = userRole.Id;
            user.Status = UserStatus.Pending;
            user.CreatedBy = null;

            if (isNewUser)
            {
                await _unitOfWork.UserRepository.Add(user);
            }

            await _unitOfWork.SaveChangeAsync();

            var otpSent = await _emailService.SendAuthenticationOtpAsync(email, RegisterOtpPurpose);
            if (!otpSent)
            {
                if (isNewUser)
                {
                    _unitOfWork.UserRepository.HardRemove(user);
                    await _unitOfWork.SaveChangeAsync();
                }

                return Failed<object>("Could not send OTP email. Please try again.");
            }

            return Succeeded<object>("OTP email verification sent.");
        }

        public async Task<Result<AuthResult>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var validationError = ValidateOtpRequest(request);
            if (validationError != null)
            {
                return Failed<AuthResult>(validationError);
            }

            var email = NormalizeEmail(request.Email);
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(email);
            if (user == null || user.Status != UserStatus.Pending)
            {
                return await CompleteLoginOtpAsync(request, email);
            }

            var isOtpValid = await _emailService.VerifyAuthenticationOtpAsync(email, RegisterOtpPurpose, request.Otp);
            if (!isOtpValid)
            {
                return Failed<AuthResult>("Invalid or expired OTP.");
            }

            user.Status = UserStatus.Active;
            if (user.CreatedBy == Guid.Empty)
            {
                user.CreatedBy = null;
            }

            await _unitOfWork.SaveChangeAsync();
            await StoreLoginOtpVerificationSessionAsync(email);

            return Succeeded<AuthResult>("Register successfully.");
        }

        public async Task<Result<AuthResult>> RequestLoginAsync(LoginRequest request)
        {
            var validationError = ValidateLoginRequest(request);
            if (validationError != null)
            {
                return Failed<AuthResult>(validationError);
            }

            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                return Failed<AuthResult>("Email/username or password is incorrect.");
            }

            if (user.Status != UserStatus.Active)
            {
                return Failed<AuthResult>("User account is not active.");
            }

            var role = user.Role ?? await _unitOfWork.UserRepository.GetRoleByIdAsync(user.RoleId);
            if (role == null)
            {
                return Failed<AuthResult>("User role is not configured.");
            }

            var email = NormalizeEmail(request.Email);
            var isOtpSessionActive = await IsLoginOtpVerificationSessionActiveAsync(email);
            if (isOtpSessionActive)
            {
                return await CompleteLoginAsync(user, role);
            }

            var otpSent = await _emailService.SendAuthenticationOtpAsync(email, LoginOtpPurpose);
            if (!otpSent)
            {
                return Failed<AuthResult>("Could not send OTP email. Please try again.");
            }

            return Succeeded<AuthResult>("OTP email verification sent.");
        }

        private async Task<Result<AuthResult>> CompleteLoginOtpAsync(VerifyOtpRequest request, string email)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return Failed<AuthResult>("Invalid or expired OTP.");
            }

            if (user.Status != UserStatus.Active)
            {
                return Failed<AuthResult>("User account is not active.");
            }

            var role = user.Role ?? await _unitOfWork.UserRepository.GetRoleByIdAsync(user.RoleId);
            if (role == null)
            {
                return Failed<AuthResult>("User role is not configured.");
            }

            var isOtpValid = await _emailService.VerifyAuthenticationOtpAsync(email, LoginOtpPurpose, request.Otp);
            if (!isOtpValid)
            {
                return Failed<AuthResult>("Invalid or expired OTP.");
            }

            await StoreLoginOtpVerificationSessionAsync(email);
            return await CompleteLoginAsync(user, role);
        }

        public async Task<Result<object>> LogoutAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return new Result<object>
                {
                    Error = 1,
                    Message = "JWT session is missing."
                };
            }

            await _jwtTokenService.RevokeSessionAsync(sessionId);
            return new Result<object>
            {
                Error = 0,
                Message = "Logout successfully."
            };
        }

        private AuthResult BuildAuthResult(User user, string roleName, JwtTokenResult tokens)
        {
            return new AuthResult
            {
                Tokens = tokens,
                Response = new AuthResponse
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = _encryptionService.Decrypt(user.Email),
                    RoleName = roleName,
                    SessionId = tokens.SessionId,
                    AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                    RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt
                }
            };
        }

        private async Task<Result<AuthResult>> CompleteLoginAsync(User user, Role role)
        {
            var tokens = await _jwtTokenService.GenerateAndStoreTokensAsync(user, role.RoleName);
            return Succeeded("Login successfully.", BuildAuthResult(user, role.RoleName, tokens));
        }

        private async Task<bool> IsLoginOtpVerificationSessionActiveAsync(string email)
        {
            var cacheItem = await _redisService.GetAsync<bool>(BuildLoginOtpVerificationSessionCacheKey(email));
            return cacheItem;
        }

        private Task StoreLoginOtpVerificationSessionAsync(string email)
        {
            return _redisService.SetAsync(
                BuildLoginOtpVerificationSessionCacheKey(email),
                true,
                LoginOtpVerificationSessionExpiration);
        }

        private static string BuildLoginOtpVerificationSessionCacheKey(string email)
        {
            return $"auth:otp-session:login:{email}";
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string? ValidateRegisterRequest(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return "Username is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return "Email is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return "Password is required.";
            }

            if (request.Password.Length < 5)
            {
                return "Password must be at least 5 characters.";
            }

            if (!request.Password.Any(char.IsUpper))
            {
                return "Password must contain at least 1 uppercase character.";
            }

            if (!request.Password.Any(char.IsDigit))
            {
                return "Password must contain at least 1 number.";
            }

            if (!request.Password.Any(character => !char.IsLetterOrDigit(character)))
            {
                return "Password must contain at least 1 special character.";
            }

            return null;
        }

        private static string? ValidateLoginRequest(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return "Email or username is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return "Password is required.";
            }

            return null;
        }

        private static string? ValidateOtpRequest(VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return "Email is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Otp))
            {
                return "OTP is required.";
            }

            return null;
        }

        private static Result<T> Failed<T>(string message)
        {
            return new Result<T>
            {
                Error = 1,
                Message = message
            };
        }

        private static Result<T> Succeeded<T>(string message, T? data = default)
        {
            return new Result<T>
            {
                Error = 0,
                Message = message,
                Data = data
            };
        }
    }
}
